using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StationOrion
{
    /// <summary>
    /// Keeps grabbable items (power cell, canisters, keycard) from getting lost or clipping.
    ///  1. Laser grab keeps the item at the tip of the laser, but the held item is moved by
    ///     physics (velocity tracking), so walls and table tops stop it.
    ///  2. Letting go does not throw it; items have friction and damping so they don't roll away.
    ///  3. Thin table tops, shelves and windows get thicker (invisible) colliders so items can't
    ///     slip through them, and the push-out speed after a collision is limited.
    ///  4. If an item still ends up outside the station (outside every room, below the floor or
    ///     far away), it returns to the last place it was resting inside the station, with a beep.
    /// It adds itself to every XRGrabInteractable when a scene starts - nothing to set up.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ItemSafety : MonoBehaviour
    {
        [Tooltip("Return the item if it drops below this height (the floor is at 0).")]
        public float minHeight = -1f;
        [Tooltip("Return the item if it gets further than this from where it started (metres).")]
        public float maxDistance = 40f;
        [Tooltip("Seconds an item may be outside every room before it is returned.")]
        public float outsideGrace = 0.75f;

        XRGrabInteractable grab;
        Rigidbody rb;
        Vector3 startPos, safePos;
        Quaternion startRot, safeRot;
        float outsideSince = -1f;

        static readonly List<Bounds> rooms = new List<Bounds>();
        static PhysicsMaterial itemMaterial;

        // ---------- automatic setup for every scene ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Setup();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode m) { Setup(); }

        static void Setup()
        {
            // the station's rooms = the floor areas you can teleport on (+ room height)
            rooms.Clear();
            foreach (var c in Object.FindObjectsByType<Collider>())
                if (c.name == "TeleportFloor")
                {
                    var b = c.bounds;
                    b.SetMinMax(new Vector3(b.min.x - 0.25f, -0.6f, b.min.z - 0.25f), new Vector3(b.max.x + 0.25f, 6f, b.max.z + 0.25f));
                    rooms.Add(b);
                }

            ThickenThinColliders();

            foreach (var g in Object.FindObjectsByType<XRGrabInteractable>())
                if (g.GetComponent<ItemSafety>() == null) g.gameObject.AddComponent<ItemSafety>();
        }

        /// <summary>Very thin solid boxes (shelves, table tops, glass) get an invisible 12 cm thick collider.</summary>
        static void ThickenThinColliders()
        {
            const float minThick = 0.12f;
            foreach (var box in Object.FindObjectsByType<BoxCollider>())
            {
                if (box.isTrigger || box.attachedRigidbody != null) continue;
                Vector3 ls = box.transform.lossyScale;
                Vector3 world = new Vector3(Mathf.Abs(box.size.x * ls.x), Mathf.Abs(box.size.y * ls.y), Mathf.Abs(box.size.z * ls.z));
                for (int axis = 0; axis < 3; axis++)
                {
                    float thick = world[axis];
                    float a = world[(axis + 1) % 3], b = world[(axis + 2) % 3];
                    if (thick >= minThick || thick < 0.0001f || a < 0.25f || b < 0.25f) continue;
                    float scale = Mathf.Abs(ls[axis]) < 0.0001f ? 1f : Mathf.Abs(ls[axis]);
                    float newLocal = minThick / scale;
                    Vector3 size = box.size, center = box.center;
                    bool horizontal = axis == 1 && Vector3.Dot(box.transform.up, Vector3.up) > 0.9f;
                    if (horizontal) center.y -= (newLocal - size.y) * 0.5f;   // keep the top surface where it is
                    size[axis] = newLocal;
                    box.size = size;
                    box.center = center;
                }
            }
        }

        // ---------- per item ----------

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            startPos = safePos = transform.position;
            startRot = safeRot = transform.rotation;

            grab.farAttachMode = InteractableFarAttachMode.Far;                        // stays at the laser tip
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;     // walls and tables block it while held
            grab.throwOnDetach = false;                                                // dropping doesn't fling it

            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.maxDepenetrationVelocity = 1.5f;   // no "shooting out" of a wall after overlapping it
                rb.linearDamping = 0.3f;
                rb.angularDamping = 2.5f;             // round items stop rolling quickly
            }
            if (itemMaterial == null)
            {
                itemMaterial = new PhysicsMaterial("SO_Item")
                {
                    dynamicFriction = 0.7f,
                    staticFriction = 0.8f,
                    bounciness = 0.05f,
                    frictionCombine = PhysicsMaterialCombine.Maximum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
            }
            foreach (var c in GetComponentsInChildren<Collider>())
                if (!c.isTrigger) c.sharedMaterial = itemMaterial;
        }

        void Update()
        {
            if (grab.isSelected) { outsideSince = -1f; return; }   // held, or sitting in a socket

            Vector3 p = transform.position;
            bool inside = InsideStation(p);

            if (p.y < minHeight || (p - startPos).sqrMagnitude > maxDistance * maxDistance) { ReturnToSafety(); return; }

            if (!inside)
            {
                if (outsideSince < 0f) outsideSince = Time.time;
                else if (Time.time - outsideSince > outsideGrace) ReturnToSafety();
                return;
            }
            outsideSince = -1f;

            // remember the last place it was resting safely inside the station
            if (rb == null || rb.linearVelocity.sqrMagnitude < 0.04f)
            {
                safePos = p;
                safeRot = transform.rotation;
            }
        }

        static bool InsideStation(Vector3 p)
        {
            if (rooms.Count == 0) return true;   // scene without TeleportFloor objects: skip this check
            foreach (var b in rooms) if (b.Contains(p)) return true;
            return false;
        }

        [ContextMenu("Return to safety")]
        public void ReturnToSafety()
        {
            Vector3 to = InsideStation(safePos) && safePos.y > minHeight ? safePos : startPos;
            Quaternion rot = to == safePos ? safeRot : startRot;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = to + Vector3.up * 0.02f;
                rb.rotation = rot;
            }
            transform.SetPositionAndRotation(to + Vector3.up * 0.02f, rot);
            outsideSince = -1f;
            SOAudio.PlayAt(SOAudio.Beep, to, 0.8f);
            Debug.Log("[ItemSafety] " + name + " left the station and was returned.");
        }
    }
}
