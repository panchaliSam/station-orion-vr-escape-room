using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace StationOrion
{
    /// <summary>
    /// Puts the player back on the Command Deck whenever the scene starts or restarts, and catches the
    /// player if they ever fall out of the station.
    ///
    /// Why it's needed: a restart reloads the scene, but the XR Interaction Simulator (and a real headset)
    /// keep their tracked head position. If you had walked away from the start point, the camera would
    /// reappear that far from the reloaded XR Origin, often outside the hull. Gravity then pulls the rig
    /// down into space. This script moves the XR Origin so the head is back at the spawn point, facing the
    /// spawn direction, with gravity paused until it is placed.
    ///
    /// It also reconnects the XR Interaction Simulator to the new camera after a restart.
    ///
    /// It adds itself to the XR Origin automatically when a scene starts. Nothing to set up.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public class PlayerSpawn : MonoBehaviour
    {
        [Tooltip("How far below the lowest floor the head may drop before the player is rescued (m).")]
        public float fallMargin = 0.8f;

        XROrigin origin;
        Camera cam;
        CharacterController body;
        GravityProvider gravity;
        Vector3 spawnPos, spawnForward;
        Vector3 lastSafeFloor;
        float lowestFloorY = float.PositiveInfinity, fallingSince = -1f;
        readonly List<Bounds> rooms = new List<Bounds>();
        bool placing = true;

        // ---------- automatic setup ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Attach();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            if (rebuildSimulator)
            {
                rebuildSimulator = false;
                // creates a brand-new simulator, exactly as Unity does when Play starts
                if (XRInteractionSimulator.instance == null) XRInteractionSimulatorLoader.Initialize();
            }
            Attach();
        }

        static bool rebuildSimulator;

        /// <summary>
        /// Call just before reloading the scene. The XR Interaction Simulator normally survives a reload and
        /// keeps links to the old scene's controllers, so the mouse stops steering them in round 2.
        /// Removing it here means a fresh one is created for the new round, the same as on the first Play.
        /// </summary>
        public static void BeforeRestart()
        {
            var sim = XRInteractionSimulator.instance;
            if (sim == null) return;
            Destroy(sim.gameObject);
            rebuildSimulator = true;
        }

        static void Attach()
        {
            var o = Object.FindAnyObjectByType<XROrigin>();
            if (o != null && o.GetComponent<PlayerSpawn>() == null) o.gameObject.AddComponent<PlayerSpawn>();
        }

        // ---------- spawn ----------

        void Awake()
        {
            origin = GetComponent<XROrigin>();
            cam = origin.Camera != null ? origin.Camera : Camera.main;
            body = GetComponent<CharacterController>();
            gravity = GetComponentInChildren<GravityProvider>(true);

            // the spawn point is where the XR Origin was placed in the scene (the Command Deck)
            spawnPos = transform.position;
            spawnForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (spawnForward == Vector3.zero) spawnForward = Vector3.forward;
            lastSafeFloor = spawnPos;

            foreach (var c in Object.FindObjectsByType<Collider>())
                if (c.name == "TeleportFloor")
                {
                    rooms.Add(c.bounds);
                    lowestFloorY = Mathf.Min(lowestFloorY, c.bounds.max.y);
                }
            if (float.IsInfinity(lowestFloorY)) lowestFloorY = spawnPos.y;

            ReconnectSimulator();
        }

        /// <summary>
        /// The XR Interaction Simulator is created once and survives a scene reload. It remembers the camera,
        /// the controllers and the hand aim points it found the first time it started. After a restart those
        /// objects are destroyed, so mouse look and "controller follows the mouse" (point and click) stop
        /// working. This points the simulator at the new camera, controllers and aim points.
        /// </summary>
        void ReconnectSimulator()
        {
            if (cam == null) return;
            var sim = XRInteractionSimulator.instance;
            if (sim == null) return;

            sim.cameraTransform = cam.transform;

            Transform leftCtrl = null, rightCtrl = null, leftAim = null, rightAim = null;
            var modality = Object.FindAnyObjectByType<XRInputModalityManager>();
            if (modality != null)
            {
                leftCtrl = PoseTransform(modality.leftController);
                rightCtrl = PoseTransform(modality.rightController);
                leftAim = AimTransform(modality.leftHand);
                rightAim = AimTransform(modality.rightHand);
            }
            sim.leftControllerTransform = leftCtrl != null ? leftCtrl : cam.transform;
            sim.rightControllerTransform = rightCtrl != null ? rightCtrl : cam.transform;
            sim.leftHandAimTransform = leftAim != null ? leftAim : cam.transform;
            sim.rightHandAimTransform = rightAim != null ? rightAim : cam.transform;
        }

        static Transform PoseTransform(GameObject controller)
        {
            if (controller == null) return null;
            var driver = controller.GetComponentInChildren<TrackedPoseDriver>(true);
            return driver != null ? driver.transform : null;
        }

        static Transform AimTransform(GameObject hand)
        {
            if (hand == null) return null;
            foreach (var interactor in hand.GetComponentsInChildren<NearFarInteractor>(true))
            {
                var caster = interactor.farInteractionCaster;
                if (caster != null && caster.castOrigin != null) return caster.castOrigin;
            }
            return null;
        }

        IEnumerator Start()
        {
            bool hadGravity = gravity != null && gravity.useGravity;
            if (gravity != null) gravity.useGravity = false;

            // wait for the headset / simulator to report its pose, then place the player (twice, to be safe)
            yield return null;
            yield return null;
            ReconnectSimulator();
            PlaceAt(spawnPos, true);
            yield return new WaitForSeconds(0.3f);
            PlaceAt(spawnPos, true);

            if (gravity != null) { gravity.ResetFallForce(); gravity.useGravity = hadGravity; }
            placing = false;
        }

        /// <summary>Moves the XR Origin so the head is above <paramref name="floorPoint"/>.</summary>
        public void PlaceAt(Vector3 floorPoint, bool faceSpawnDirection)
        {
            if (origin == null || cam == null) return;
            bool ccOn = body != null && body.enabled;
            if (ccOn) body.enabled = false;   // a CharacterController ignores direct position changes

            if (faceSpawnDirection) origin.MatchOriginUpCameraForward(Vector3.up, spawnForward);

            // keep the player's real head height, but never below a crouch or above a tall adult
            float headHeight = Mathf.Clamp(cam.transform.position.y - transform.position.y, 1.1f, 1.9f);
            origin.MoveCameraToWorldLocation(new Vector3(floorPoint.x, floorPoint.y + headHeight, floorPoint.z));
            // stand the rig on the floor itself
            var p = transform.position; p.y = floorPoint.y; transform.position = p;

            if (ccOn) body.enabled = true;
            if (gravity != null) gravity.ResetFallForce();
            Physics.SyncTransforms();
        }

        // ---------- fall rescue ----------

        void LateUpdate()
        {
            if (placing || cam == null) return;
            Vector3 head = cam.transform.position;

            // remember the last place the player stood inside a room
            if (head.y > lowestFloorY + 0.5f && InsideRoom(head))
                lastSafeFloor = new Vector3(head.x, transform.position.y, head.z);

            bool fallen = head.y < lowestFloorY - fallMargin || (head.y < lowestFloorY + 0.3f && !InsideRoom(head));
            if (!fallen) { fallingSince = -1f; return; }
            if (fallingSince < 0f) { fallingSince = Time.time; return; }
            if (Time.time - fallingSince < 0.25f) return;

            fallingSince = -1f;
            PlaceAt(InsideRoom(lastSafeFloor) ? lastSafeFloor : spawnPos, false);
            SOAudio.PlayAt(SOAudio.Beep, cam.transform.position, 0.6f);
        }

        bool InsideRoom(Vector3 p)
        {
            if (rooms.Count == 0) return true;
            foreach (var b in rooms)
                if (p.x >= b.min.x - 0.2f && p.x <= b.max.x + 0.2f && p.z >= b.min.z - 0.2f && p.z <= b.max.z + 0.2f) return true;
            return false;
        }
    }
}
