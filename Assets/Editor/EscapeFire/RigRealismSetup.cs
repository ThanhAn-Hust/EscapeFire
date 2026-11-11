using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Player;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V0.1 "real-life" rig (plan Phần B.0 / B.2), idempotent:
    /// human hands with finger curl instead of controller models, posture-limited walking speed, tunneling
    /// vignette on all locomotion, and near-only (hand-at-object) interaction on every PCCC interactable.
    /// </summary>
    public static class RigRealismSetup
    {
        private const string LAB = "Assets/Scenes/BasicScene.unity";
        private const string HANDS = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/";
        private const string ACTIONS = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
        private const string VIGNETTE = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";

        [MenuItem("EscapeFire/V0/Setup Realistic Rig (Lab)")]
        public static void RunOnLab()
        {
            var scene = EditorSceneManager.OpenScene(LAB, OpenSceneMode.Single);
            var log = new List<string>();
            Apply(log);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] Realistic rig: " + (log.Count > 0 ? string.Join("; ", log) : "already set up"));
        }

        public static void Apply(List<string> log)
        {
            var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (origin == null) { log.Add("no XR Origin"); return; }

            if (origin.GetComponent<PostureLocomotion>() == null)
            {
                origin.gameObject.AddComponent<PostureLocomotion>();
                log.Add("PostureLocomotion on XR Origin");
            }

            SetupHand(origin, "Left Controller", true, log);
            SetupHand(origin, "Right Controller", false, log);
            SetupVignette(origin, log);
            AddNearOnlyFilters(log);
            SetupCallPoints(log);
            SetupExtinguishers(log);
            SetupBreakerPanel(log);
            SetupExitDoor(log);
        }

        // ------------------------------------------------------------------ V0.7 exit door

        /// <summary>
        /// B.1-8 / B.1-9: the heat check is done with the back of the hand (DoorHeatChecker), no ray select;
        /// each leaf has a lever handle that must be turned down before the leaf can be pushed open.
        /// </summary>
        private static void SetupExitDoor(List<string> log)
        {
            var door = Object.FindFirstObjectByType<ExitDoor>(FindObjectsInactive.Include);
            if (door == null) return;
            foreach (var si in door.GetComponents<XRSimpleInteractable>()) si.enabled = false;
            var root = door.transform.parent;
            var black = Mat("EF_ExitDoorHandle", new Color(0.8f, 0.8f, 0.78f), 0.8f, 1f);
            int n = 0;
            foreach (var hingeName in new[] { "ExitDoor_HingeLeft", "ExitDoor_HingeRight" })
            {
                var hinge = root != null ? root.Find(hingeName) : null;
                if (hinge == null) continue;
                bool left = hingeName.EndsWith("Left");
                var oldIn = hinge.Find("Handle_In");
                Vector3 handlePos = oldIn != null ? oldIn.position : hinge.position + Vector3.up;
                if (oldIn != null) oldIn.gameObject.SetActive(false);
                var pivot = Child(hinge, "EF_HandlePivotIn");
                pivot.position = handlePos;
                pivot.localRotation = Quaternion.identity;
                ClearChildren(pivot);
                float toHinge = left ? -1f : 1f; // lever points back towards the hinge, like a real lever handle
                Prim(PrimitiveType.Cube, "EF_HandleRose", pivot, Vector3.zero, Quaternion.identity, new Vector3(0.05f, 0.05f, 0.015f), black);
                Prim(PrimitiveType.Cube, "EF_HandleLever", pivot, new Vector3(toHinge * 0.06f, 0f, 0.025f), Quaternion.identity, new Vector3(0.12f, 0.022f, 0.022f), black);
                var grip = Child(pivot, "GrabPoint");
                grip.localPosition = new Vector3(toHinge * 0.08f, 0f, 0.03f);
                var handle = pivot.GetComponent<HingeGrab>();
                if (handle == null) handle = pivot.gameObject.AddComponent<HingeGrab>();
                // Pushing the lever down rotates it about the door normal; sign so that "down" is positive travel.
                handle.Configure(Vector3.forward, 0f, 45f * (left ? 1f : -1f), grip, true, 0f);
                EditorUtility.SetDirty(handle);

                var leaf = hinge.GetComponent<HingeGrab>();
                if (leaf == null) leaf = hinge.gameObject.AddComponent<HingeGrab>();
                leaf.Configure(Vector3.up, 0f, 100f * (left ? 1f : -1f), grip, false, 0f, handle, 25f);
                EditorUtility.SetDirty(leaf);
                n++;
            }
            log.Add($"exit door: back-of-hand heat check, {n} leaves with lever handles (turn, then push)");
        }

        // ------------------------------------------------------------------ V0.6 breaker panel

        /// <summary>
        /// B.1-3: a hinged metal panel door in front of the breakers, and a real lever behind it: ON tilted up,
        /// OFF tilted down, 70 deg travel; pulled past 85 % it latches OFF. Released earlier it springs back.
        /// The old "select = toggle" shortcut is removed.
        /// </summary>
        private static void SetupBreakerPanel(List<string> log)
        {
            var breaker = Object.FindFirstObjectByType<CircuitBreakerSwitch>(FindObjectsInactive.Include);
            if (breaker == null) return;
            Transform zone = breaker.transform;
            var oldHandle = zone.Find("Aptomat_Switch_Handle");
            bool any = false;
            Bounds b = new Bounds(zone.position, Vector3.one * 0.4f);
            foreach (var r in zone.GetComponentsInChildren<Renderer>(true))
            {
                if (oldHandle != null && r.transform.IsChildOf(oldHandle)) continue;
                if (r.transform.name.StartsWith("EF_")) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            Vector3 intoWall = IntoWall(zone.position);
            Vector3 outOfWall = -intoWall;
            Vector3 along = Vector3.Cross(Vector3.up, intoWall).normalized; // horizontal, parallel to the wall
            float frontOffset = Vector3.Dot(b.center - zone.position, outOfWall) + Mathf.Abs(Vector3.Dot(b.extents, outOfWall));
            Vector3 front = zone.position + outOfWall * frontOffset; // panel face, in front of the zone pivot
            front.y = b.center.y;
            float width = Mathf.Abs(Vector3.Dot(b.size, along)), height = b.size.y;

            if (oldHandle != null)
            {
                foreach (var r in oldHandle.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (var c in oldHandle.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            }
            foreach (var si in zone.GetComponents<XRSimpleInteractable>()) si.enabled = false;

            var metal = Mat("EF_PanelDoor", new Color(0.62f, 0.64f, 0.66f), 0.5f, 0.6f);
            var black = Mat("EF_LeverBlack", new Color(0.05f, 0.05f, 0.05f), 0.4f);
            var red = Mat("EF_LeverRed", new Color(0.75f, 0.08f, 0.06f), 0.4f);

            // Panel door: hinge on one vertical edge, swings out into the room.
            var doorHinge = Child(zone, "EF_PanelDoorHinge");
            doorHinge.position = front + outOfWall * 0.04f - along * (width * 0.5f);
            doorHinge.rotation = Quaternion.LookRotation(along, Vector3.up);
            ClearChildren(doorHinge);
            Prim(PrimitiveType.Cube, "EF_PanelDoorLeaf", doorHinge, new Vector3(0f, 0f, width * 0.5f), Quaternion.identity, new Vector3(0.012f, height * 1.02f, width * 1.02f), metal);
            Prim(PrimitiveType.Cube, "EF_PanelDoorKnob", doorHinge, new Vector3(-0.02f, 0f, width * 0.9f), Quaternion.identity, new Vector3(0.03f, 0.06f, 0.02f), black);
            var doorGrab = Child(doorHinge, "GrabPoint");
            doorGrab.localPosition = new Vector3(-0.03f, 0f, width * 0.9f);
            var door = doorHinge.GetComponent<HingeGrab>();
            if (door == null) door = doorHinge.gameObject.AddComponent<HingeGrab>();
            // Positive rotation about up turns the free edge (local +Z) out of the wall?
            float openSign = Mathf.Sign(Vector3.Dot(Quaternion.AngleAxis(90f, Vector3.up) * along, outOfWall));
            door.Configure(Vector3.up, 0f, 110f * openSign, doorGrab, false, 0f);
            EditorUtility.SetDirty(door);

            // Lever behind the door: arm sticks out of the panel, tilted up (ON); rotates about the along-wall axis.
            var leverPivot = Child(zone, "EF_BreakerLever");
            leverPivot.position = front + outOfWall * 0.015f;
            leverPivot.rotation = Quaternion.LookRotation(outOfWall, Vector3.up);
            leverPivot.localRotation = leverPivot.localRotation * Quaternion.identity;
            ClearChildren(leverPivot);
            var arm = Child(leverPivot, "Arm");
            arm.localPosition = Vector3.zero;
            arm.localRotation = Quaternion.Euler(-35f, 0f, 0f); // tilted up = ON
            Prim(PrimitiveType.Cube, "EF_LeverBody", arm, new Vector3(0f, 0f, 0.035f), Quaternion.identity, new Vector3(0.025f, 0.02f, 0.07f), black);
            Prim(PrimitiveType.Cube, "EF_LeverTip", arm, new Vector3(0f, 0f, 0.07f), Quaternion.identity, new Vector3(0.03f, 0.025f, 0.02f), red);
            var tip = Child(arm, "GrabPoint");
            tip.localPosition = new Vector3(0f, 0f, 0.07f);
            var lever = leverPivot.GetComponent<HingeGrab>();
            if (lever == null) lever = leverPivot.gameObject.AddComponent<HingeGrab>();
            // Pitching the pivot about its local X turns the arm from up (ON) to down (OFF): +70 deg.
            lever.Configure(Vector3.right, 0f, 70f, tip, true, 0.85f, door, 60f);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(lever.OnLatched, breaker.LeverPulledOff);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(lever.OnLatched, breaker.LeverPulledOff);
            EditorUtility.SetDirty(lever);

            var so = new SerializedObject(breaker);
            so.FindProperty("lever").objectReferenceValue = lever;
            so.FindProperty("switchHandle").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add("breaker panel: hinged door + 70 deg lever (latch OFF at 85 %), select shortcut removed");
        }

        // ------------------------------------------------------------------ V0.4 / V0.5 extinguishers

        /// <summary>
        /// B.1-4 / B.1-5: carry handle as grab point, wall bracket (lift to detach), one hand on the body only,
        /// a safety pin pulled by the other hand, a horn / hose nozzle held by the other hand to aim.
        /// </summary>
        private static void SetupExtinguishers(List<string> log)
        {
            foreach (var ext in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var grab = ext.GetComponent<XRGrabInteractable>();
                if (grab == null) continue;
                Transform t = ext.transform;
                Bounds b = ModelBounds(t);

                var handle = Child(t, "CarryHandle");
                handle.position = new Vector3(b.center.x, b.max.y - 0.07f, b.center.z);
                handle.rotation = t.rotation;
                // The carry handle must be grabbable: a collider around it (the body collider stops lower).
                var handleCol = handle.GetComponent<BoxCollider>();
                if (handleCol == null) handleCol = handle.gameObject.AddComponent<BoxCollider>();
                Vector3 hs = handle.lossyScale;
                handleCol.size = new Vector3(0.14f / Mathf.Max(1e-4f, hs.x), 0.1f / Mathf.Max(1e-4f, hs.y), 0.1f / Mathf.Max(1e-4f, hs.z));
                handleCol.center = Vector3.zero;
                if (!grab.colliders.Contains(handleCol) && grab.colliders.Count > 0) grab.colliders.Add(handleCol);
                grab.attachTransform = handle;
                grab.useDynamicAttach = false;
                grab.attachEaseInTime = 0.2f; // heavy: follows the hand with a little lag
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous; // no physics shove against the wall while hooked
                grab.throwOnDetach = false;
                EditorUtility.SetDirty(grab);

                // Order matters: the general transformer follows the hand, then the bracket constrains it.
                var general = ext.GetComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
                if (general == null) general = ext.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();
                var mount = ext.GetComponent<ExtinguisherMount>();
                if (mount == null) mount = ext.gameObject.AddComponent<ExtinguisherMount>();
                var gso = new SerializedObject(grab);
                var list = gso.FindProperty("m_StartingSingleGrabTransformers");
                list.ClearArray();
                list.InsertArrayElementAtIndex(0); list.GetArrayElementAtIndex(0).objectReferenceValue = general;
                list.InsertArrayElementAtIndex(1); list.GetArrayElementAtIndex(1).objectReferenceValue = mount;
                gso.ApplyModifiedPropertiesWithoutUndo();
                var near = ext.GetComponent<NearOnlyFilter>();
                if (near != null) { near.Exclusive = true; near.Reach = 0.15f; EditorUtility.SetDirty(near); }

                Vector3 side = t.right; // the pin slides out sideways through the valve
                var pin = Child(t, "SafetyPin");
                pin.position = handle.position + side * 0.03f + t.up * 0.02f;
                pin.rotation = t.rotation;
                ClearChildren(pin);
                Prim(PrimitiveType.Cylinder, "Ring", pin, new Vector3(0.035f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.035f, 0.004f, 0.035f), Mat("EF_PinRing", new Color(0.95f, 0.8f, 0.1f)));
                Prim(PrimitiveType.Cylinder, "Rod", pin, Vector3.zero, Quaternion.Euler(0f, 0f, 90f), new Vector3(0.006f, 0.03f, 0.006f), Mat("EF_PinRod", new Color(0.75f, 0.75f, 0.78f), 0.8f, 1f));
                var pinComp = pin.GetComponent<ExtinguisherPin>();
                if (pinComp == null) pinComp = pin.gameObject.AddComponent<ExtinguisherPin>();
                pinComp.Configure(ext, grab, Vector3.right);
                EditorUtility.SetDirty(pinComp);

                bool co2 = ext.Type == ExtinguisherType.CO2;
                var hoseRoot = Child(t, "HoseRoot");
                hoseRoot.position = handle.position + t.up * 0.03f - side * 0.02f;
                var horn = Child(t, "Horn");
                // Hangs in its clip on the side of the bottle, pointing down.
                horn.position = new Vector3(b.center.x, b.center.y + 0.05f, b.center.z) - side * (b.extents.x + 0.035f);
                horn.rotation = Quaternion.LookRotation(Vector3.down, t.forward);
                ClearChildren(horn);
                var dark = Mat("EF_HornBlack", new Color(0.06f, 0.06f, 0.06f), 0.4f);
                Prim(PrimitiveType.Cylinder, "Grip", horn, new Vector3(0f, 0f, -0.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.03f, 0.04f, 0.03f), dark);
                if (co2)
                {
                    // CO2 discharge horn: a wide black cone
                    Prim(PrimitiveType.Cylinder, "Horn_Mid", horn, new Vector3(0f, 0f, 0.04f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.05f, 0.05f, 0.05f), dark);
                    Prim(PrimitiveType.Cylinder, "Horn_Mouth", horn, new Vector3(0f, 0f, 0.11f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.085f, 0.03f, 0.085f), dark);
                }
                else
                {
                    Prim(PrimitiveType.Cylinder, "Nozzle", horn, new Vector3(0f, 0f, 0.03f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.025f, 0.04f, 0.025f), dark);
                }
                var tip = Child(horn, "Tip");
                tip.localPosition = new Vector3(0f, 0f, co2 ? 0.15f : 0.08f);
                tip.localRotation = Quaternion.identity;
                var lr = horn.GetComponent<LineRenderer>();
                if (lr == null) lr = horn.gameObject.AddComponent<LineRenderer>();
                lr.sharedMaterial = Mat("EF_HoseBlack", new Color(0.04f, 0.04f, 0.04f), 0.3f);
                lr.widthMultiplier = 0.018f;
                var hornComp = horn.GetComponent<ExtinguisherHorn>();
                if (hornComp == null) hornComp = horn.gameObject.AddComponent<ExtinguisherHorn>();
                hornComp.Configure(ext, grab, hoseRoot);
                EditorUtility.SetDirty(hornComp);

                var so = new SerializedObject(ext);
                so.FindProperty("nozzle").objectReferenceValue = tip;
                so.ApplyModifiedPropertiesWithoutUndo();
                log.Add("extinguisher '" + ext.name + "' (" + ext.Type + "): carry handle, wall bracket, pin, " + (co2 ? "CO2 horn" : "hose nozzle"));
            }
        }

        private static Bounds ModelBounds(Transform root)
        {
            bool any = false;
            Bounds b = new Bounds(root.position, Vector3.one * 0.3f);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer) continue;
                string n = r.name;
                if (n == "Ring" || n == "Rod" || n.StartsWith("Horn") || n == "Grip" || n == "Nozzle") continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static Transform Child(Transform parent, string name)
        {
            var c = parent.Find(name);
            if (c != null) return c;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                if (t.GetChild(i).name != "Tip") Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        private static void Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 worldSize, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPos, localRot);
            Vector3 ps = parent.lossyScale;
            go.transform.localScale = new Vector3(worldSize.x / Mathf.Max(1e-4f, ps.x), worldSize.y / Mathf.Max(1e-4f, ps.y), worldSize.z / Mathf.Max(1e-4f, ps.z));
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material Mat(string name, Color c, float smooth = 0.4f, float metal = 0f)
        {
            string path = "Assets/Settings/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// B.1-1: a manual call point is pressed with a fingertip, ≥ 8 mm into the face, towards the wall.
        /// Grabbing it with a fist or pointing from afar does nothing.
        /// </summary>
        private static void SetupCallPoints(List<string> log)
        {
            foreach (var alarm in Object.FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var interactable = alarm.GetComponent<XRBaseInteractable>();
                if (interactable == null) continue;
                var near = alarm.GetComponent<NearOnlyFilter>();
                if (near != null) { near.PokeOnly = true; near.Reach = 0.12f; EditorUtility.SetDirty(near); }

                Vector3 intoWall = IntoWall(alarm.transform.position);
                PokeAxis axis = LocalAxis(alarm.transform, intoWall);
                var box = alarm.GetComponent<BoxCollider>();
                MakeFacePlate(box, axis, CallPointTravel);
                // XRI selects when the fingertip reaches the interactable's pivot; the pivot sits behind the face,
                // so shift the trigger point forward to "face + travel".
                float faceToPivot = box != null ? Vector3.Dot(alarm.transform.position - box.bounds.center, intoWall) + box.bounds.extents[intoWall.x != 0f ? 0 : 2] : 0f;
                var poke = alarm.GetComponent<XRPokeFilter>();
                if (poke == null) poke = alarm.gameObject.AddComponent<XRPokeFilter>();
                poke.pokeConfiguration = new UnityEngine.XR.Interaction.Toolkit.Filtering.PokeThresholdDatumProperty(new PokeThresholdData
                {
                    pokeDirection = axis,
                    interactionDepthOffset = Mathf.Max(0f, faceToPivot - CallPointTravel),
                    enablePokeAngleThreshold = true,
                    pokeAngleThreshold = 60f
                });
                EditorUtility.SetDirty(poke);
                log.Add($"call point '{alarm.name}': fingertip poke {LocalAxis(alarm.transform, intoWall)}");
            }
        }

        /// <summary>
        /// XRI fires a poke when the fingertip has travelled through the whole collider along the poke axis, so the
        /// collider becomes a thin plate at the front face: its thickness is the real button travel (~9 mm).
        /// </summary>
        public const float CallPointTravel = 0.009f;

        public static void MakeFacePlate(BoxCollider box, PokeAxis axis, float travelMeters)
        {
            if (box == null) return;
            int i = axis == PokeAxis.X || axis == PokeAxis.NegativeX ? 0 : axis == PokeAxis.Y || axis == PokeAxis.NegativeY ? 1 : 2;
            float sign = axis == PokeAxis.X || axis == PokeAxis.Y || axis == PokeAxis.Z ? -1f : 1f; // front face is opposite the poke direction
            float scale = Mathf.Abs(box.transform.lossyScale[i]);
            if (scale < 1e-5f) return;
            Vector3 size = box.size, center = box.center;
            float front = center[i] + sign * size[i] * 0.5f;
            size[i] = travelMeters / scale;
            center[i] = front - sign * size[i] * 0.5f;
            box.size = size;
            box.center = center;
            EditorUtility.SetDirty(box);
        }

        /// <summary>Walls are axis-aligned: the direction from the room centre towards the wall the object hangs on.</summary>
        public static Vector3 IntoWall(Vector3 p)
        {
            Vector3 d = new Vector3(p.x, 0f, p.z);
            return Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(d.z));
        }

        public static PokeAxis LocalAxis(Transform t, Vector3 worldDir)
        {
            var axes = new (Vector3 dir, PokeAxis axis)[]
            {
                (t.right, PokeAxis.X), (-t.right, PokeAxis.NegativeX), (t.up, PokeAxis.Y), (-t.up, PokeAxis.NegativeY),
                (t.forward, PokeAxis.Z), (-t.forward, PokeAxis.NegativeZ)
            };
            return axes.OrderByDescending(a => Vector3.Dot(a.dir.normalized, worldDir)).First().axis;
        }

        private static void SetupHand(XROrigin origin, string controllerName, bool left, List<string> log)
        {
            var controller = origin.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == controllerName);
            if (controller == null) { log.Add($"{controllerName} missing"); return; }
            var hand = controller.GetComponent<ControllerHand>();
            bool added = hand == null;
            if (added) hand = controller.gameObject.AddComponent<ControllerHand>();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(HANDS + (left ? "LeftHand.fbx" : "RightHand.fbx"));
            string map = left ? "XRI Left Interaction" : "XRI Right Interaction";
            hand.Configure(model, left, new InputActionProperty(Action(map, "Select Value")), new InputActionProperty(Action(map, "Activate Value")));
            EditorUtility.SetDirty(hand);
            if (added) log.Add($"hand model on {controllerName}");
        }

        private static InputActionReference Action(string map, string action) =>
            AssetDatabase.LoadAllAssetsAtPath(ACTIONS).OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.actionMap != null && r.action.actionMap.name == map && r.action.name == action);

        private static void SetupVignette(XROrigin origin, List<string> log)
        {
            if (origin.Camera == null) return;
            var controller = origin.GetComponentInChildren<TunnelingVignetteController>(true);
            if (controller == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIGNETTE);
                if (prefab == null) { log.Add("vignette prefab missing"); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, origin.Camera.transform);
                go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                controller = go.GetComponent<TunnelingVignetteController>();
                log.Add("tunneling vignette under Main Camera");
            }
            var providers = new List<LocomotionVignetteProvider>();
            foreach (var p in origin.GetComponentsInChildren<LocomotionProvider>(true))
                providers.Add(new LocomotionVignetteProvider { locomotionProvider = p, enabled = true });
            controller.locomotionVignetteProviders = providers;
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Everything the learner presses, pulls or grabs in real life must be reached with the hand.</summary>
        private static void AddNearOnlyFilters(List<string> log)
        {
            int n = 0;
            var targets = new List<GameObject>();
            foreach (var c in Object.FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<CircuitBreakerSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<DoorHeatChecker>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<NPCSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var go in targets)
                foreach (var interactable in go.GetComponentsInChildren<XRBaseInteractable>(true))
                    if (EnsureNearOnly(interactable, go.GetComponent<NPCSimulator>() != null ? 0.35f : 0.15f)) n++;
            if (n > 0) log.Add($"near-only filter on {n} interactable(s)");
        }

        public static bool EnsureNearOnly(XRBaseInteractable interactable, float reach)
        {
            var filter = interactable.GetComponent<NearOnlyFilter>();
            bool added = filter == null;
            if (added) filter = interactable.gameObject.AddComponent<NearOnlyFilter>();
            filter.Reach = reach;
            EditorUtility.SetDirty(filter);
            var so = new SerializedObject(interactable);
            bool changed = AddToList(so.FindProperty("m_StartingHoverFilters"), filter) | AddToList(so.FindProperty("m_StartingSelectFilters"), filter);
            so.ApplyModifiedPropertiesWithoutUndo();
            return added || changed;
        }

        private static bool AddToList(SerializedProperty list, Object item)
        {
            if (list == null) return false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return false;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            return true;
        }
    }
}
