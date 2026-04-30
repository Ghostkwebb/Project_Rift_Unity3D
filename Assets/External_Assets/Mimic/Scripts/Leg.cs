using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MimicSpace
{
    public class Leg : MonoBehaviour
    {
        Mimic myMimic;
        public bool isDeployed = false;
        public Vector3 footPosition;
        public float maxLegDistance;
        public int legResolution;
        //public GameObject legObject;
        public LineRenderer legLine;
        public int handlesCount = 8; // 8 (7 legs + 1 finalfoot)

        public float legMinHeight;
        public float legMaxHeight;
        float legHeight;
        public Vector3[] handles;
        public float handleOffsetMinRadius;
        public float handleOffsetMaxRadius;
        public Vector3[] handleOffsets;
        public float finalFootDistance;

        public float growCoef;
        public float growTarget = 1;

        [Range(0, 1f)]
        public float progression;

        bool isRemoved = false;
        bool canDie = false;
        public float minDuration;

        [Header("Rotation")]
        public float rotationSpeed;
        public float minRotSpeed;
        public float maxRotSpeed;
        float rotationSign = 1;
        public float oscillationSpeed;
        public float minOscillationSpeed;
        public float maxOscillationSpeed;
        float oscillationProgress;

        public Color myColor;
        private float waitToDieTimer = 0f;
        private float waitAndDieTimer = 0f;
        private Vector3[] tempHandles;
        private Vector3[] linePoints;

        public void Initialize(Vector3 footPosition, int legResolution, float maxLegDistance, float growCoef, Mimic myMimic, float lifeTime)
        {
            myColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
            this.footPosition = footPosition;
            this.legResolution = legResolution;
            this.maxLegDistance = maxLegDistance;
            this.growCoef = growCoef;
            this.myMimic = myMimic;

            this.legLine = GetComponent<LineRenderer>();
            handles = new Vector3[handlesCount];

            // INIT ARRAYS ONCE
            tempHandles = new Vector3[handlesCount];
            linePoints = new Vector3[legResolution + 2];

            handleOffsets = new Vector3[6];
            handleOffsets[0] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);
            handleOffsets[1] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);
            handleOffsets[2] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);
            handleOffsets[3] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);
            handleOffsets[4] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);
            handleOffsets[5] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);

            Vector2 footOffset = Random.insideUnitCircle.normalized * finalFootDistance;
            RaycastHit hit;
            Vector3 offsetPos = footPosition + new Vector3(footOffset.x, 0, footOffset.y);

            if (Physics.Raycast(offsetPos + Vector3.up * 5f, Vector3.down, out hit))
            {
                handles[7] = hit.point;
            }
            else
            {
                handles[7] = offsetPos;
            }

            legHeight = Random.Range(legMinHeight, legMaxHeight);
            rotationSpeed = Random.Range(minRotSpeed, maxRotSpeed);
            rotationSign = 1;
            oscillationSpeed = Random.Range(minOscillationSpeed, maxOscillationSpeed);
            oscillationProgress = 0;

            myMimic.legCount++;
            growTarget = 1;

            isRemoved = false;
            canDie = false;
            isDeployed = false;
            waitToDieTimer = minDuration;
            waitAndDieTimer = lifeTime;
            Sethandles();
        }

        private void Update()
        {
            if (waitToDieTimer > 0) waitToDieTimer -= Time.deltaTime;
            else canDie = true;

            if (waitAndDieTimer > 0) waitAndDieTimer -= Time.deltaTime;
            else if (myMimic.deployedLegs >= myMimic.minimumAnchoredParts) growTarget = 0;


            if (growTarget == 1 && Vector3.Distance(new Vector3(myMimic.legPlacerOrigin.x, 0, myMimic.legPlacerOrigin.z), new Vector3(footPosition.x, 0, footPosition.z)) > maxLegDistance && canDie && myMimic.deployedLegs > myMimic.minimumAnchoredParts)
                growTarget = 0;
            else if (growTarget == 1)
            {
                if (Physics.Linecast(footPosition, transform.position, out RaycastHit hit)) growTarget = 0;
            }

            progression = Mathf.Lerp(progression, growTarget, growCoef * Time.deltaTime);

            if (!isDeployed && progression > 0.9f)
            {
                myMimic.deployedLegs++;
                isDeployed = true;
            }
            else if (isDeployed && progression < 0.9f)
            {
                myMimic.deployedLegs--;
                isDeployed = false;
            }
            if (progression < 0.5f && growTarget == 0)
            {
                if (!isRemoved)
                {
                    myMimic.legCount--;
                    isRemoved = true;
                }

                if (progression < 0.05f)
                {
                    legLine.positionCount = 0;
                    myMimic.RecycleLeg(this.gameObject);
                    return;
                }
            }

            Sethandles();

            // FAST ZERO-ALLOC ROUTINE
            int count = FillSamplePoints(legResolution, progression);
            legLine.positionCount = count;
            legLine.SetPositions(linePoints);
        }

        void Sethandles()
        {
            handles[0] = transform.position;
            handles[6] = footPosition + Vector3.up * 0.05f;

            handles[2] = Vector3.Lerp(handles[0], handles[6], 0.4f);
            handles[2].y = handles[0].y + legHeight;

            handles[1] = Vector3.Lerp(handles[0], handles[2], 0.5f);
            handles[3] = Vector3.Lerp(handles[2], handles[6], 0.25f);
            handles[4] = Vector3.Lerp(handles[2], handles[6], 0.5f);
            handles[5] = Vector3.Lerp(handles[2], handles[6], 0.75f);

            RotateHandleOffset();

            handles[1] += handleOffsets[0];
            handles[2] += handleOffsets[1];
            handles[3] += handleOffsets[2];
            handles[4] += handleOffsets[3] / 2f;
            handles[5] += handleOffsets[4] / 4f;
        }

        void RotateHandleOffset()
        {
            oscillationProgress += Time.deltaTime * oscillationSpeed;
            if (oscillationProgress >= 360f) oscillationProgress -= 360f;

            float newAngle = rotationSpeed * Time.deltaTime * Mathf.Cos(oscillationProgress * Mathf.Deg2Rad) + 1f;

            for (int i = 1; i < 6; i++)
            {
                Vector3 axisRotation = (handles[i + 1] - handles[i - 1]) / 2f;
                handleOffsets[i - 1] = Quaternion.AngleAxis(newAngle, rotationSign * axisRotation) * handleOffsets[i - 1];
            }
        }

        int FillSamplePoints(int resolution, float t)
        {
            int index = 0;
            float segmentLength = 1f / (float)resolution;

            for (float _t = 0; _t <= t; _t += segmentLength)
            {
                linePoints[index] = GetPointOnCurve(_t);
                index++;
            }
            linePoints[index] = GetPointOnCurve(t);
            index++;
            return index;
        }

        Vector3 GetPointOnCurve(float t)
        {
            for (int i = 0; i < handles.Length; i++) tempHandles[i] = handles[i];

            int currentPoints = tempHandles.Length;
            while (currentPoints > 1)
            {
                for (int i = 0; i < currentPoints - 1; i++)
                    tempHandles[i] = Vector3.Lerp(tempHandles[i], tempHandles[i + 1], t);
                currentPoints--;
            }
            return tempHandles[0];
        }
    }
}