using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// Slides a door sideways when Open() is called (by the MissionManager).
    /// The door keeps its collider, so it blocks the player and the teleport ray while closed.
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [Tooltip("How far the door slides, in metres.")]
        public float slideDistance = 1.6f;
        [Tooltip("Metres per second.")]
        public float speed = 1.2f;
        [Tooltip("Slide to the door's right (true) or left (false).")]
        public bool slideRight = true;

        Vector3 closedPos, openPos;
        bool isOpen;

        public bool IsOpen => isOpen;

        void Awake()
        {
            closedPos = transform.position;
            Vector3 dir = slideRight ? transform.right : -transform.right;
            openPos = closedPos + dir * slideDistance;
        }

        [ContextMenu("Open door")]
        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            SOAudio.PlayAt(SOAudio.Door, transform.position, 0.9f);
        }

        [ContextMenu("Close door")]
        public void Close() { isOpen = false; }

        void Update()
        {
            Vector3 target = isOpen ? openPos : closedPos;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
    }
}
