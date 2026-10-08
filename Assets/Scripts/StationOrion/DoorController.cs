using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// Sliding door opened by the MissionManager.
    ///  - Simple mode (no panels assigned): the object itself slides sideways.
    ///  - Two-panel mode: leftPanel / rightPanel slide apart into the wall (sci-fi door).
    /// Status lights (Glow) on the frame show red = locked, green = open.
    /// Colliders stay on, so a closed door blocks the player and the teleport ray.
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [Tooltip("How far the door (or each panel) slides, in metres.")]
        public float slideDistance = 1.6f;
        [Tooltip("Metres per second.")]
        public float speed = 1.2f;
        [Tooltip("Simple mode only: slide to the door's right (true) or left (false).")]
        public bool slideRight = true;

        [Header("Two-panel mode (optional)")]
        public Transform leftPanel;
        public Transform rightPanel;

        [Header("Status lights (optional)")]
        public Glow[] statusLights = new Glow[0];

        Vector3 closedPos, openPos;
        Vector3 lClosed, lOpen, rClosed, rOpen;
        bool isOpen;

        public bool IsOpen => isOpen;

        void Awake()
        {
            closedPos = transform.position;
            Vector3 dir = slideRight ? transform.right : -transform.right;
            openPos = closedPos + dir * slideDistance;
            if (leftPanel != null) { lClosed = leftPanel.localPosition; lOpen = lClosed + Vector3.left * slideDistance; }
            if (rightPanel != null) { rClosed = rightPanel.localPosition; rOpen = rClosed + Vector3.right * slideDistance; }
        }

        void Start() { RefreshStatus(); }

        [ContextMenu("Open door")]
        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            SOAudio.PlayAt(SOAudio.Door, transform.position + Vector3.up, 0.9f);
            RefreshStatus();
        }

        [ContextMenu("Close door")]
        public void Close() { isOpen = false; RefreshStatus(); }

        void RefreshStatus()
        {
            if (statusLights == null) return;
            Color c = isOpen ? new Color(0.2f, 1f, 0.35f) : new Color(1f, 0.15f, 0.1f);
            foreach (var g in statusLights) if (g != null) g.Set(c, !isOpen);
        }

        bool TwoPanel => leftPanel != null || rightPanel != null;

        void Update()
        {
            float step = speed * Time.deltaTime;
            if (TwoPanel)
            {
                if (leftPanel != null) leftPanel.localPosition = Vector3.MoveTowards(leftPanel.localPosition, isOpen ? lOpen : lClosed, step);
                if (rightPanel != null) rightPanel.localPosition = Vector3.MoveTowards(rightPanel.localPosition, isOpen ? rOpen : rClosed, step);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, isOpen ? openPos : closedPos, step);
            }
        }
    }
}
