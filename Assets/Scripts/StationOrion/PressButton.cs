using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StationOrion
{
    /// <summary>
    /// A physical 3D button. Point at it and press GRIP (select) to press it.
    /// It moves in, plays a click, and raises an event. Used for the keypad keys
    /// and the emergency shutdown button.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class PressButton : MonoBehaviour
    {
        [Tooltip("What this button means, e.g. \"7\", \"C\" or \"EMERGENCY\".")]
        public string id = "";
        [Tooltip("World direction the button moves when pressed (into the panel).")]
        public Vector3 pushDirection = Vector3.down;
        public float pushDepth = 0.015f;
        public bool playClick = true;

        public UnityEvent onPressed = new UnityEvent();
        public System.Action<PressButton> Pressed;

        XRSimpleInteractable simple;
        Vector3 restPos;
        float pressedUntil;

        void Awake()
        {
            simple = GetComponent<XRSimpleInteractable>();
            restPos = transform.position;
        }

        void OnEnable()
        {
            if (simple == null) simple = GetComponent<XRSimpleInteractable>();
            simple.selectEntered.AddListener(OnSelect);
        }

        void OnDisable()
        {
            if (simple != null) simple.selectEntered.RemoveListener(OnSelect);
        }

        void OnSelect(SelectEnterEventArgs args) { Press(); }

        [ContextMenu("Press")]
        public void Press()
        {
            pressedUntil = Time.time + 0.15f;
            if (playClick) SOAudio.PlayAt(SOAudio.Key, transform.position, 0.8f);
            onPressed.Invoke();
            Pressed?.Invoke(this);
        }

        void Update()
        {
            Vector3 target = Time.time < pressedUntil ? restPos + pushDirection.normalized * pushDepth : restPos;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 25f);
        }
    }
}
