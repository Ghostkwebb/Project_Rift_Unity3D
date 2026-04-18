using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class DimensionSwapper : MonoBehaviour
{
    public Volume realVolume;
    public Volume riftVolume;
    public GameObject realGeo;
    public GameObject riftGeo;
    public Transform testObstacle;

    public PlayerMovement playerMove;
    public AudioSource levelAudio;
    public Transform cameraTransform;

    private bool inRift = false;
    private bool isSwapping = false;
    private Vector3 obstacleStartPos;

    void Start()
    {
        // Subscribe to shared controls
        playerMove.controls.Player.Swap.performed += ctx => TrySwap();

        riftGeo.SetActive(false);
        riftVolume.weight = 0f;
        if (testObstacle != null) obstacleStartPos = testObstacle.position;
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
        Vector3 camOriginalPos = cameraTransform.localPosition;

        if (!inRift) riftGeo.SetActive(true);
        else realGeo.SetActive(true);

        Vector3 endObstaclePos = inRift ? obstacleStartPos : obstacleStartPos + Vector3.down * 5f;
        Vector3 currentObstaclePos = testObstacle != null ? testObstacle.position : Vector3.zero;

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
            }
            else
            {
                riftVolume.weight = 1f - t;
                realVolume.weight = t;
                if (levelAudio) levelAudio.pitch = Mathf.Lerp(0.5f, 1f, t);
            }

            if (testObstacle != null)
            {
                testObstacle.position = Vector3.Lerp(currentObstaclePos, endObstaclePos, t);
            }

            yield return null;
        }

        cameraTransform.localPosition = camOriginalPos;
        playerMove.swapSpeedMultiplier = 1f;

        if (!inRift) realGeo.SetActive(false);
        else riftGeo.SetActive(false);

        inRift = !inRift;
        isSwapping = false;
    }
}