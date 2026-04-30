using System.Collections.Generic;
using UnityEngine;

namespace MimicSpace
{
    public class Mimic : MonoBehaviour
    {
        [Header("Animation")]
        public GameObject legPrefab; [Range(2, 20)] public int numberOfLegs = 5; [Range(1, 10)] public int partsPerLeg = 4;
        int maxLegs;

        public int legCount;
        public int deployedLegs;
        [Range(0, 19)] public int minimumAnchoredLegs = 2;
        public int minimumAnchoredParts;

        public float minLegLifetime = 5;
        public float maxLegLifetime = 15;
        public Vector3 legPlacerOrigin = Vector3.zero;
        public float newLegRadius = 3;
        public float minLegDistance = 4.5f;
        public float maxLegDistance = 6.3f; [Range(2, 50)] public int legResolution = 40;
        public float minGrowCoef = 4.5f;
        public float maxGrowCoef = 6.5f;
        public float newLegCooldown = 0.3f;

        private float cooldownTimer = 0f;
        List<GameObject> availableLegPool = new List<GameObject>();
        public Vector3 velocity;

        void Start() { ResetMimic(); }

        void ResetMimic()
        {
            foreach (Leg g in Object.FindObjectsByType<Leg>(FindObjectsSortMode.None)) Destroy(g.gameObject);
            legCount = 0;
            deployedLegs = 0;
            maxLegs = numberOfLegs * partsPerLeg;
            velocity = new Vector3(Random.insideUnitCircle.x, 0, Random.insideUnitCircle.y);
            minimumAnchoredParts = minimumAnchoredLegs * partsPerLeg;
            maxLegDistance = newLegRadius * 2.1f;
        }

        void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return;
            }

            legPlacerOrigin = transform.position + velocity.normalized * newLegRadius;

            if (legCount <= maxLegs - partsPerLeg)
            {
                Vector2 offset = Random.insideUnitCircle * newLegRadius;
                Vector3 newLegPosition = legPlacerOrigin + new Vector3(offset.x, 0, offset.y);

                if (velocity.magnitude > 1f)
                {
                    if (Mathf.Abs(Vector3.Angle(velocity, newLegPosition - transform.position)) > 90)
                        newLegPosition = transform.position - (newLegPosition - transform.position);
                }

                Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flatLeg = new Vector3(legPlacerOrigin.x, 0, legPlacerOrigin.z);
                if (Vector3.Distance(flatPos, flatLeg) < minLegDistance)
                    newLegPosition = ((newLegPosition - transform.position).normalized * minLegDistance) + transform.position;

                if (Vector3.Angle(velocity, newLegPosition - transform.position) > 45)
                    newLegPosition = transform.position + ((newLegPosition - transform.position) + velocity.normalized * (newLegPosition - transform.position).magnitude) / 2f;

                RaycastHit hit;
                Vector3 myHit = newLegPosition;
                myHit.y = transform.position.y;

                Vector3 bodyOrigin = transform.position + Vector3.up * 1f;
                Vector3 legTarget = newLegPosition + Vector3.up * 1f;

                if (Physics.Linecast(bodyOrigin, legTarget, out hit)) myHit = hit.point;
                else if (Physics.Raycast(newLegPosition + Vector3.up * 5f, Vector3.down, out hit, 10f)) myHit = hit.point;

                float lifeTime = Random.Range(minLegLifetime, maxLegLifetime);
                cooldownTimer = newLegCooldown;

                for (int i = 0; i < partsPerLeg; i++)
                {
                    RequestLeg(myHit, legResolution, maxLegDistance, Random.Range(minGrowCoef, maxGrowCoef), this, lifeTime);
                    if (legCount >= maxLegs) return;
                }
            }
        }

        void RequestLeg(Vector3 footPosition, int legResolution, float maxLegDistance, float growCoef, Mimic myMimic, float lifeTime)
        {
            GameObject newLeg;
            if (availableLegPool.Count > 0)
            {
                newLeg = availableLegPool[availableLegPool.Count - 1];
                availableLegPool.RemoveAt(availableLegPool.Count - 1);
            }
            else newLeg = Instantiate(legPrefab, transform.position, Quaternion.identity);

            newLeg.SetActive(true);
            newLeg.GetComponent<Leg>().Initialize(footPosition, legResolution, maxLegDistance, growCoef, myMimic, lifeTime);
            newLeg.transform.SetParent(myMimic.transform);
        }

        public void RecycleLeg(GameObject leg)
        {
            availableLegPool.Add(leg);
            leg.SetActive(false);
        }
    }
}