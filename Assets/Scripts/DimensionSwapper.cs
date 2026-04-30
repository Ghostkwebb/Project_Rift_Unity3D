using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DimensionSwapper : MonoBehaviour
{
    public static System.Action<bool> OnDimensionSwap;
    public static bool isRiftGlobal = false;
    public Volume realVolume;
    public Volume riftVolume;
    public GameObject realGeo;
    public GameObject riftGeo;

    // 2. ADD VIGNETTE VARS
    private Vignette realVignette;
    private Vignette riftVignette;

    [Header("Obstacles")]
    public float sinkDistance = 5f;
    public Transform[] realObstacles;
    public Transform[] riftObstacles;

    [Header("References")]
    public PlayerMovement playerMove;
    public AudioSource levelAudio;
    public Transform cameraTransform;

    private bool inRift = false;
    private bool isSwapping = false;
    private Vector3[] realStartPos;
    private Vector3[] riftStartPos;

    void Start()
    {
        playerMove.controls.Player.Swap.performed += ctx => TrySwap();

        riftGeo.SetActive(false);
        riftVolume.weight = 0f;

        realStartPos = new Vector3[realObstacles.Length];
        for (int i = 0; i < realObstacles.Length; i++)
            realStartPos[i] = realObstacles[i].position;

        riftStartPos = new Vector3[riftObstacles.Length];
        for (int i = 0; i < riftObstacles.Length; i++)
        {
            riftStartPos[i] = riftObstacles[i].position;
            riftObstacles[i].position += Vector3.down * sinkDistance;
        }

        realVolume.profile.TryGet(out realVignette);
        riftVolume.profile.TryGet(out riftVignette);
    }

    void Update()
    {
        if (realVignette != null)
        {
            float targetReal = playerMove.isCrouching ? 0.56f : 0.46f;
            realVignette.intensity.value = Mathf.Lerp(realVignette.intensity.value, targetReal, Time.deltaTime * 8f);
        }

        if (riftVignette != null)
        {
            float targetRift = playerMove.isCrouching ? 0.6f : 0.5f;
            riftVignette.intensity.value = Mathf.Lerp(riftVignette.intensity.value, targetRift, Time.deltaTime * 8f);
        }
    }


    void TrySwap()
    {
        if (!isSwapping) StartCoroutine(SwapDimensionCoroutine());
    }

    IEnumerator SwapDimensionCoroutine()
    {
        isSwapping = true;
        playerMove.swapSpeedMultiplier = 0.5f;

        float timer = 0f;
        Vector3 camOriginalPos = Vector3.zero;

        if (!inRift) riftGeo.SetActive(true);
        else realGeo.SetActive(true);

        while (timer < 1f)
        {
            timer += Time.deltaTime;
            float t = timer / 1f;

            cameraTransform.localPosition = camOriginalPos + Random.insideUnitSphere * 0.1f;

            if (!inRift)
            {
                realVolume.weight = 1f - t;
                riftVolume.weight = t;
                if (levelAudio) levelAudio.pitch = Mathf.Lerp(1f, 0.5f, t);

                // Swap to Rift: Real sink, Rift rise
                MoveObstacles(realObstacles, realStartPos, true, t);
                MoveObstacles(riftObstacles, riftStartPos, false, t);
            }
            else
            {
                riftVolume.weight = 1f - t;
                realVolume.weight = t;
                if (levelAudio) levelAudio.pitch = Mathf.Lerp(0.5f, 1f, t);

                // Swap to Real: Real rise, Rift sink
                MoveObstacles(realObstacles, realStartPos, false, t);
                MoveObstacles(riftObstacles, riftStartPos, true, t);
            }

            yield return null;
        }

        cameraTransform.localPosition = camOriginalPos;
        playerMove.swapSpeedMultiplier = 1f;

        if (!inRift) realGeo.SetActive(false);
        else riftGeo.SetActive(false);

        inRift = !inRift;
        isSwapping = false;

        isRiftGlobal = inRift;
        OnDimensionSwap?.Invoke(inRift);
    }

    void MoveObstacles(Transform[] obstacles, Vector3[] startPositions, bool isSinking, float t)
    {
        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i] == null) continue;

            Vector3 upPos = startPositions[i];
            Vector3 downPos = startPositions[i] + Vector3.down * sinkDistance;

            Vector3 from = isSinking ? upPos : downPos;
            Vector3 to = isSinking ? downPos : upPos;

            obstacles[i].position = Vector3.Lerp(from, to, t);
        }
    }
}