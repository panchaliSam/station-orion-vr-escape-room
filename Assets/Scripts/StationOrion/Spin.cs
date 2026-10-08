using UnityEngine;

namespace StationOrion
{
    /// <summary>Rotates an object and optionally bobs it up and down (reactor ring, objective marker).</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 90f, 0f);
        public float bobHeight = 0f;
        public float bobSpeed = 2f;
        [Tooltip("Multiplier other scripts can change (e.g. reactor slows down when stable).")]
        public float speedMultiplier = 1f;

        Vector3 basePos;

        void Start() { basePos = transform.localPosition; }

        void Update()
        {
            transform.Rotate(degreesPerSecond * speedMultiplier * Time.deltaTime, Space.Self);
            if (bobHeight > 0f)
                transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }
    }
}
