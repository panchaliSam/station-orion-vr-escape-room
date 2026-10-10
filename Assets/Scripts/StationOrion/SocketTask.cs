using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StationOrion
{
    /// <summary>
    /// Sits on an XR Socket Interactor (a "drop zone") and turns it into a mission task.
    ///  - Tells the MissionManager when the right item is placed or removed.
    ///  - Drives the glowing pad under the socket: dim = idle, pulsing = do this now,
    ///    green = done, red flash = wrong item.
    ///  - Plays a clunk + beep when an item snaps in, and a buzz for a wrong item.
    /// The socket's Interaction Layer Mask decides which item is accepted.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class SocketTask : MonoBehaviour
    {
        public string displayName = "Power Cell";
        [Tooltip("Colour of this task (pad glow).")]
        public Color color = Color.yellow;
        [Tooltip("The visible pad/panel under the socket that should glow.")]
        public Renderer pad;
        [Tooltip("The item that belongs in this socket.")]
        public XRGrabInteractable expectedItem;

        public System.Action<SocketTask> Filled;
        public System.Action<SocketTask> Emptied;
        public System.Action<SocketTask> WrongItem;

        XRSocketInteractor socket;
        Glow padGlow;
        bool targeted;
        float errorUntil;
        float nextErrorAllowed;

        public bool IsFilled => socket != null && socket.hasSelection;

        void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();
            if (pad != null)
            {
                padGlow = pad.GetComponent<Glow>();
                if (padGlow == null) padGlow = pad.gameObject.AddComponent<Glow>();
            }
        }

        void OnEnable()
        {
            if (socket == null) socket = GetComponent<XRSocketInteractor>();
            socket.selectEntered.AddListener(OnEntered);
            socket.selectExited.AddListener(OnExited);
        }

        void OnDisable()
        {
            if (socket == null) return;
            socket.selectEntered.RemoveListener(OnEntered);
            socket.selectExited.RemoveListener(OnExited);
        }

        void Start() { Refresh(); }

        void OnEntered(SelectEnterEventArgs args)
        {
            SOAudio.PlayAt(SOAudio.Clunk, transform.position, 1f);
            SOAudio.PlayAt(SOAudio.Beep, transform.position, 0.6f);
            Refresh();
            Filled?.Invoke(this);
        }

        void OnExited(SelectExitEventArgs args)
        {
            Refresh();
            Emptied?.Invoke(this);
        }

        /// <summary>MissionManager calls this to make the pad pulse ("place the item here").</summary>
        public void SetTargeted(bool value)
        {
            if (targeted == value) return;
            targeted = value;
            Refresh();
        }

        // A wrong item came close to this socket -> red flash + buzz (feedback, Week 2 HCI loop).
        void OnTriggerEnter(Collider other)
        {
            if (IsFilled || socket == null) return;
            var item = other.GetComponentInParent<XRGrabInteractable>();
            if (item == null) return;
            bool accepted = (item.interactionLayers.value & socket.interactionLayers.value) != 0;
            if (accepted) return;
            if (Time.time < nextErrorAllowed) return;
            nextErrorAllowed = Time.time + 1.2f;
            errorUntil = Time.time + 1f;
            SOAudio.PlayAt(SOAudio.Error, transform.position, 0.7f);
            Refresh();
            Invoke(nameof(Refresh), 1.05f);
            WrongItem?.Invoke(this);
        }

        void Refresh()
        {
            if (padGlow == null) return;
            if (IsFilled) padGlow.Set(new Color(0.2f, 1f, 0.3f), false);          // done = green
            else if (Time.time < errorUntil) padGlow.Set(Color.red, true);        // wrong item
            else if (targeted) padGlow.Set(color, true);                         // do this now
            else padGlow.Set(color * 0.35f, false);                              // idle
        }
    }
}
