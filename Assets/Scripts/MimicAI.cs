using UnityEngine;
using UnityEngine.AI;
using MimicSpace;

public class MimicAI : MonoBehaviour
{
    public enum MimicState { Patrol, Investigate, Chase, Stunned }

    [Header("AI State")]
    public MimicState currentState = MimicState.Patrol;
    public Transform player;
    public ItemHandler playerItemHandler;

    [Header("Patrol Tether")]
    public float tetherRadius = 20f;
    public float patrolWaitTime = 2f;
    private float waitTimer = 0f; [Header("Investigate Search")]
    public float searchWaitTime = 4f;
    public float fuzzyOffsetMax = 4f;
    private float searchTimer = 0f; [Header("Vision & Chase")]
    public float visionRange = 25f;
    public float visionAngle = 45f;
    public float chaseSpeedMultiplier = 2f;
    public float killDistance = 2.5f;
    public LayerMask obstacleMask = 1;
    private Vector3 lastKnownPos; [Header("Photophobia (Light Stun)")]
    public float stunDuration = 1.5f;
    private float stunTimer = 0f;

    [Header("Components")]
    public NavMeshAgent agent;
    public Mimic myMimic;
    public Renderer mimicRenderer;

    [Header("Dimension Materials")]
    public Material realMaterial;
    public Material riftMaterial;

    [Header("Speeds")]
    public float realSpeed = 2f;
    public float riftSpeed = 4f;

    private bool isRift = false;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (myMimic == null) myMimic = GetComponent<Mimic>();

        DimensionSwapper.OnDimensionSwap += HandleDimensionSwap;
        NoiseSystem.OnNoise += HearNoise;

        HandleDimensionSwap(DimensionSwapper.isRiftGlobal);
        SetNewPatrolPoint();
    }

    void OnDestroy()
    {
        DimensionSwapper.OnDimensionSwap -= HandleDimensionSwap;
        NoiseSystem.OnNoise -= HearNoise;
    }

    void Update()
    {
        // Mimic legs follow NavMesh agent speed
        myMimic.velocity = agent.velocity;

        // Light & Vision Checks (Rift Only)
        if (isRift)
        {
            bool lightHitting = IsTorchHittingMe();

            if (currentState != MimicState.Stunned)
            {
                if (lightHitting) StartStun();
                else if (currentState != MimicState.Chase && CanSeePlayer()) StartChasing(player.position);
            }
        }

        switch (currentState)
        {
            case MimicState.Patrol: PatrolBehavior(); break;
            case MimicState.Investigate: InvestigateBehavior(); break;
            case MimicState.Chase: ChaseBehavior(); break;
            case MimicState.Stunned: StunBehavior(); break;
        }
    }

    bool IsTorchHittingMe()
    {
        if (playerItemHandler == null) return false;

        TorchItem activeTorch = playerItemHandler.GetHeldTorch();
        if (activeTorch == null || !activeTorch.isOn || activeTorch.torchLight == null) return false;

        Vector3 dirToMimic = transform.position - activeTorch.transform.position;
        float dist = dirToMimic.magnitude;

        // Inside light range?
        if (dist <= activeTorch.torchLight.range)
        {
            // Inside spotlight cone?
            float angle = Vector3.Angle(activeTorch.torchLight.transform.forward, dirToMimic);
            if (angle <= activeTorch.torchLight.spotAngle / 2f)
            {
                // Wall blocking light?
                if (!Physics.Raycast(activeTorch.transform.position, dirToMimic, dist, obstacleMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void StartStun()
    {
        currentState = MimicState.Stunned;
        stunTimer = stunDuration;
        agent.isStopped = true; // Stop moving
        agent.velocity = Vector3.zero;
        Debug.Log("MIMIC STUNNED BY LIGHT! Hissing...");
    }

    void StunBehavior()
    {
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0)
        {
            agent.isStopped = false; // Allow movement again

            if (IsTorchHittingMe())
            {
                // Player didn't turn off torch -> ENRAGE
                Debug.Log("MIMIC ENRAGED!");
                StartChasing(player.position);
            }
            else
            {
                // Player turned off torch/hid -> Investigate last spot
                StartInvestigating(player.position);
            }
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) < visionAngle)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist < visionRange)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dist, obstacleMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void PatrolBehavior()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime)
            {
                SetNewPatrolPoint();
                waitTimer = 0f;
            }
        }
    }

    void InvestigateBehavior()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            searchTimer += Time.deltaTime;
            transform.Rotate(Vector3.up * 45f * Time.deltaTime);

            if (searchTimer >= searchWaitTime)
            {
                currentState = MimicState.Patrol;
                SetNewPatrolPoint();
                UpdateSpeed();
            }
        }
    }

    void ChaseBehavior()
    {
        if (Vector3.Distance(transform.position, player.position) <= killDistance)
        {
            Debug.Log("PLAYER DEAD - GAME OVER");
            currentState = MimicState.Patrol;
            SetNewPatrolPoint();
            return;
        }

        if (isRift)
        {
            if (CanSeePlayer())
            {
                lastKnownPos = player.position;
                agent.SetDestination(player.position);
            }
            else StartInvestigating(lastKnownPos);
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                StartInvestigating(transform.position);
            }
        }
    }

    void HearNoise(Vector3 noisePos, float radius)
    {
        if (currentState == MimicState.Stunned) return; // Ignore noise while blinded

        float distanceToNoise = Vector3.Distance(transform.position, noisePos);
        if (distanceToNoise > radius) return;

        if (currentState == MimicState.Chase)
        {
            if (!isRift)
            {
                lastKnownPos = noisePos;
                agent.SetDestination(noisePos);
            }
        }
        else if (currentState == MimicState.Investigate) StartChasing(noisePos);
        else StartInvestigating(noisePos);
    }

    void StartInvestigating(Vector3 noisePos)
    {
        currentState = MimicState.Investigate;
        searchTimer = 0f;
        UpdateSpeed();

        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(3f, fuzzyOffsetMax);
        Vector3 fuzzyPos = noisePos + new Vector3(randomOffset.x, 0, randomOffset.y);

        if (NavMesh.SamplePosition(fuzzyPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(noisePos);
    }

    void StartChasing(Vector3 targetPos)
    {
        currentState = MimicState.Chase;
        lastKnownPos = targetPos;
        agent.SetDestination(targetPos);
        UpdateSpeed();
    }

    void SetNewPatrolPoint()
    {
        if (player == null) return;
        Vector2 randomCircle = Random.insideUnitCircle * tetherRadius;
        Vector3 randomPos = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void HandleDimensionSwap(bool inRift)
    {
        isRift = inRift;
        mimicRenderer.material = isRift ? riftMaterial : realMaterial;

        if (currentState == MimicState.Stunned)
        {
            agent.isStopped = false;
            StartInvestigating(player.position); // Break stun if dimension swapped
        }
        else if (!isRift && currentState == MimicState.Chase)
        {
            StartInvestigating(lastKnownPos);
        }

        UpdateSpeed();
    }

    void UpdateSpeed()
    {
        float baseS = isRift ? riftSpeed : realSpeed;
        agent.speed = (currentState == MimicState.Chase) ? baseS * chaseSpeedMultiplier : baseS;
    }
}