using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MimicSpace
{
    /// <summary>
    /// Updated for New Input System.
    /// </summary>
    public class Movement : MonoBehaviour
    {
        [Header("Controls")]
        [Tooltip("Body Height from ground")]
        [Range(0.5f, 5f)]
        public float height = 0.8f;
        public float speed = 5f;
        Vector3 velocity = Vector3.zero;
        public float velocityLerpCoef = 4f;
        Mimic myMimic;

        private void Start()
        {
            myMimic = GetComponent<Mimic>();
        }

        void Update()
        {
            // Read raw WASD from new input
            float h = 0f;
            float v = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            }

            Vector3 inputDir = new Vector3(h, 0, v).normalized;
            velocity = Vector3.Lerp(velocity, inputDir * speed, velocityLerpCoef * Time.deltaTime);

            // Assigning velocity to the mimic to assure great leg placement
            myMimic.velocity = velocity;

            transform.position = transform.position + velocity * Time.deltaTime;

            RaycastHit hit;
            Vector3 destHeight = transform.position;

            // Layer mask 2 is Ignore Raycast (Player layer). Good practice to skip it.
            int layerMask = ~(1 << 2);

            if (Physics.Raycast(transform.position + Vector3.up * 5f, -Vector3.up, out hit, 10f, layerMask))
                destHeight = new Vector3(transform.position.x, hit.point.y + height, transform.position.z);

            transform.position = Vector3.Lerp(transform.position, destHeight, velocityLerpCoef * Time.deltaTime);
        }
    }
}