using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StationOrion
{
    /// <summary>
    /// Stops the player's head (the camera) from going through walls, doors, floors and furniture.
    /// This is what the brief means by "nothing clipping through the environment".
    ///
    /// Why colliders alone don't do it: in VR (and in the XR Device Simulator) the camera is moved
    /// by head tracking, not by physics, so it ignores every collider. This script remembers the last
    /// position where the head was in free space. Each frame it sweeps a small sphere from there to the
    /// new head position; if that path hits something solid, it shifts the whole XR Origin back so the
    /// head stays where it was. Teleporting, snap turning and thumbstick moves are not affected.
    ///
    /// It adds itself to the Main Camera automatically when a scene starts - nothing to set up.
    /// </summary>
    [DefaultExecutionOrder(10000)]   // after head tracking has moved the camera
    public class HeadCollision : MonoBehaviour
    {
        [Tooltip("Size of the player's head, in metres.")]
        public float radius = 0.15f;
        [Tooltip("Which layers count as solid.")]
        public LayerMask solidLayers = ~0;
        [Tooltip("Show a message in the Console when the head is blocked (for testing).")]
        public bool debugLog = false;

        readonly Collider[] overlaps = new Collider[16];
        readonly RaycastHit[] sweeps = new RaycastHit[16];
        Transform rig;
        Vector3 lastSafeHead, lastRigPos;
        Quaternion lastRigRot;
        bool started;

        // ---------- automatic setup ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Attach();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode m) { Attach(); }

        static void Attach()
        {
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<HeadCollision>() == null) cam.gameObject.AddComponent<HeadCollision>();
        }

        // ---------- per frame ----------

        void Awake()
        {
            var origin = GetComponentInParent<XROrigin>();
            rig = origin != null ? origin.transform : transform.root;
        }

        void OnEnable() { started = false; }

        void LateUpdate()
        {
            Vector3 head = transform.position;
            if (!started) { Remember(head); started = true; return; }

            // the rig itself moved (teleport, snap turn, thumbstick): accept the new position
            if ((rig.position - lastRigPos).sqrMagnitude > 0.000001f || Quaternion.Angle(rig.rotation, lastRigRot) > 0.01f)
            {
                Remember(head);
                return;
            }

            // already inside something (e.g. spawned against a table): never trap the player
            if (Inside(lastSafeHead)) { Remember(head); return; }

            if (Inside(head) || SweepHits(lastSafeHead, head))
            {
                rig.position += lastSafeHead - head;   // undo the head movement into the object
                lastRigPos = rig.position;
                lastRigRot = rig.rotation;
                if (debugLog) Debug.Log("[HeadCollision] head blocked by a wall/object");
            }
            else Remember(head);
        }

        void Remember(Vector3 head)
        {
            lastSafeHead = head;
            lastRigPos = rig.position;
            lastRigRot = rig.rotation;
        }

        bool Inside(Vector3 p)
        {
            int n = Physics.OverlapSphereNonAlloc(p, radius, overlaps, solidLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (Solid(overlaps[i])) return true;
            return false;
        }

        bool SweepHits(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.0001f) return false;
            int m = Physics.SphereCastNonAlloc(from, radius * 0.9f, d / len, sweeps, len, solidLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < m; i++) if (Solid(sweeps[i].collider)) return true;
            return false;
        }

        bool Solid(Collider c)
        {
            if (c == null) return false;
            if (c.transform.IsChildOf(rig)) return false;     // the player's own body and hands
            if (c.attachedRigidbody != null) return false;     // items you can pick up (even while held)
            return true;
        }
    }
}
