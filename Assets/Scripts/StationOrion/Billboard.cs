using UnityEngine;

namespace StationOrion
{
    /// <summary>Keeps a floating label turned towards the player's head so it is always readable.</summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("Only turn left/right (keeps labels upright).")]
        public bool yawOnly = true;

        Transform cam;

        void LateUpdate()
        {
            if (cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                cam = c.transform;
            }
            Vector3 dir = transform.position - cam.position;
            if (yawOnly) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
