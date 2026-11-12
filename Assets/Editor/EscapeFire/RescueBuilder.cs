using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using EscapeFire.Interactions;
using EscapeFire.Lesson;
using EscapeFire.NPC;
using EscapeFire.Player;
using EscapeFire.Scenario;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V3 — rescue and full evacuation props (plan Phần D V3), built on top of the V2 building:
    /// a sink with a quarter-turn tap and a towel in the lab (B.1-7), a radio on a wall dock with the address plate
    /// for the 114 call (B.1-2), an unconscious victim (Nam) in the floor-3 corridor for the Rautek drag (B.1-11),
    /// Minh panicking at the lift (B.1-14), the warden, report board and evacuees at the assembly point (B.1-15)
    /// and the wall-follow tracker (B.1-12). Idempotent: rebuilds "[RESCUE_V3]".
    /// </summary>
    public static class RescueBuilder
    {
        private const string LAB = "Assets/Scenes/BasicScene.unity";
        private const string ROOT = "[RESCUE_V3]";
        private const string PO = "Assets/PolygonOffice/Prefabs/Characters/";

        /// <summary>Stairwell A past the floor-3 fire door (landing + first flight): the protected space a victim is dragged into.</summary>
        public static readonly Bounds StairwellALanding = new Bounds(new Vector3(0f, -0.25f, -14.875f), new Vector3(2.6f, 3.5f, 3.25f));
        public static readonly Vector3 VictimHeels = new Vector3(2.55f, 0f, -12.35f);

        [MenuItem("EscapeFire/V3/Build Rescue Scenario (rebuilds V2 building too)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(LAB, OpenSceneMode.Single);
            BuildingBuilder.Build(); // calls Build(log) below at its end
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void Build(List<string> log)
        {
            var old = GameObject.Find(ROOT);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(ROOT).transform;

            Sink(root, new Vector3(-3.6f, 0f, -6.9f));
            var radio = RadioDock(root, new Vector3(2.35f, 0f, -6.9f));
            var call = new GameObject("Call114").AddComponent<Call114Dialogue>();
            call.transform.SetParent(root, false);
            call.Configure(radio);
            Victim(root);
            Minh(root);
            RollCall(root, new Vector3(-7f, BuildingBuilder.GroundY, -23f));
            root.gameObject.AddComponent<WallFollowTracker>();
            // Teleport targets answer only the teleport ray: a hand gripping near the floor must not grab the floor.
            int fixedAreas = 0;
            foreach (var area in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mask = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");
                if (area.interactionLayers == mask) continue;
                area.interactionLayers = mask;
                EditorUtility.SetDirty(area);
                fixedAreas++;
            }
            log.Add($"teleport areas set to the Teleport layer: {fixedAreas}");
            log.Add("V3: sink + tap + towel, radio + 114 dialogue, victim Nam (Rautek), Minh at the lift, roll call board, wall-follow tracker");
        }

        // ------------------------------------------------------------------ lab: sink, tap, towel

        private static void Sink(Transform root, Vector3 wallBase)
        {
            var white = BuildingBuilder.Mat("EF_Ceramic", new Color(0.92f, 0.93f, 0.95f), 0.8f);
            var steel = BuildingBuilder.Mat("EF_ElevatorSteel", new Color(0.7f, 0.72f, 0.75f), 0.8f, 0.9f);
            var cab = BuildingBuilder.Mat("EF_Cabinet", new Color(0.35f, 0.4f, 0.45f));
            var sink = new GameObject("Sink").transform;
            sink.SetParent(root, false);
            Vector3 c = wallBase + new Vector3(0f, 0f, 0.3f);
            BuildingBuilder.Box(sink, "Cabinet", c + Vector3.up * 0.42f, Quaternion.identity, new Vector3(0.7f, 0.84f, 0.5f), cab, true);
            BuildingBuilder.Box(sink, "Counter", c + Vector3.up * 0.86f, Quaternion.identity, new Vector3(0.74f, 0.04f, 0.54f), white, true);
            BuildingBuilder.Box(sink, "Basin", c + new Vector3(0.08f, 0.85f, 0.02f), Quaternion.identity, new Vector3(0.38f, 0.03f, 0.32f), steel, false);
            // tap: pipe out of the wall, spout over the basin, quarter-turn wing handle on top
            Vector3 body = new Vector3(c.x + 0.08f, 1.08f, wallBase.z + 0.08f);
            BuildingBuilder.Cylinder(sink, "TapPipe", new Vector3(body.x, body.y, wallBase.z), body, 0.035f, steel);
            Vector3 spoutEnd = body + new Vector3(0f, -0.05f, 0.14f);
            BuildingBuilder.Cylinder(sink, "TapSpout", body, spoutEnd, 0.025f, steel);
            var spout = new GameObject("SpoutPoint").transform;
            spout.SetParent(sink, false);
            spout.position = spoutEnd + Vector3.down * 0.02f;

            var pivot = new GameObject("TapHandlePivot").transform;
            pivot.SetParent(sink, false);
            pivot.position = body + Vector3.up * 0.04f;
            BuildingBuilder.Cylinder(pivot, "Stem", pivot.position - Vector3.up * 0.03f, pivot.position + Vector3.up * 0.01f, 0.02f, steel);
            BuildingBuilder.Box(pivot, "Wing", pivot.position + new Vector3(0.04f, 0.01f, 0f), Quaternion.identity, new Vector3(0.08f, 0.015f, 0.018f), BuildingBuilder.Mat("EF_TapRed", new Color(0.75f, 0.12f, 0.1f)), false);
            var grab = new GameObject("GrabPoint").transform;
            grab.SetParent(pivot, false);
            grab.position = pivot.position + new Vector3(0.07f, 0.01f, 0f);
            var hinge = pivot.gameObject.AddComponent<HingeGrab>();
            hinge.Configure(Vector3.up, -100f, 100f, grab, false, 0f); // a quarter turn either way opens it

            var water = BuildingBuilder.Cylinder(sink, "WaterStream", spout.position, spout.position + Vector3.down * 0.2f, 0.018f,
                BuildingBuilder.Mat("EF_Water", new Color(0.65f, 0.82f, 1f), 0.95f));
            water.GetComponent<Renderer>().enabled = false;
            var tap = sink.gameObject.AddComponent<WaterTap>();
            tap.Configure(hinge, spout, water.GetComponent<Renderer>());

            // towel folded on the counter
            var towel = BuildingBuilder.Box(sink, "Towel", c + new Vector3(-0.24f, 0.9f, 0.05f), Quaternion.identity, new Vector3(0.22f, 0.03f, 0.16f),
                BuildingBuilder.Mat("EF_Towel", new Color(0.55f, 0.78f, 0.95f), 0.1f), true);
            var rb = towel.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            var g = towel.AddComponent<XRGrabInteractable>();
            g.movementType = XRBaseInteractable.MovementType.Instantaneous;
            g.throwOnDetach = false;
            RigRealismSetup.EnsureNearOnly(g, 0.12f);
            rb.isKinematic = true;
            g.gameObject.AddComponent<DropsWhenReleased>();
            towel.AddComponent<WetTowel>();

            BuildingBuilder.Box(sink, "SignPlate", new Vector3(c.x, 1.55f, wallBase.z + 0.11f), Quaternion.identity, new Vector3(0.62f, 0.26f, 0.015f), BuildingBuilder.Mat("EF_SignBlue", new Color(0.1f, 0.25f, 0.6f)), false);
            BuildingBuilder.Label(sink, "SignText", new Vector3(c.x, 1.55f, wallBase.z + 0.125f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.6f, 0.24f), 0.45f,
                "<b>KHĂN ƯỚT</b>\nVặn vòi, làm ướt khăn, che mũi miệng khi đi qua khói");
        }

        // ------------------------------------------------------------------ lab: radio dock + address

        private static Radio RadioDock(Transform root, Vector3 wallBase)
        {
            var dock = new GameObject("RadioDock").transform;
            dock.SetParent(root, false);
            var dark = BuildingBuilder.Mat("EF_RadioBody", new Color(0.12f, 0.12f, 0.13f), 0.4f);
            float z = wallBase.z + 0.1f;
            BuildingBuilder.Box(dock, "Shelf", new Vector3(wallBase.x, 1.1f, z + 0.02f), Quaternion.identity, new Vector3(0.22f, 0.02f, 0.14f), BuildingBuilder.Mat("EF_Cabinet", new Color(0.35f, 0.4f, 0.45f)), true);

            var radio = new GameObject("Radio");
            radio.transform.SetParent(dock, false);
            radio.transform.position = new Vector3(wallBase.x, 1.2f, z + 0.03f);
            BuildingBuilder.Box(radio.transform, "Body", radio.transform.position, Quaternion.identity, new Vector3(0.06f, 0.16f, 0.035f), dark, true);
            BuildingBuilder.Box(radio.transform, "PTT", radio.transform.position + new Vector3(-0.032f, 0.02f, 0f), Quaternion.identity, new Vector3(0.008f, 0.04f, 0.02f), BuildingBuilder.Mat("EF_TapRed", new Color(0.75f, 0.12f, 0.1f)), false);
            BuildingBuilder.Cylinder(radio.transform, "Antenna", radio.transform.position + new Vector3(0.018f, 0.08f, 0f), radio.transform.position + new Vector3(0.018f, 0.17f, 0f), 0.008f, dark);
            var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "TxLight";
            Object.DestroyImmediate(led.GetComponent<Collider>());
            led.transform.SetParent(radio.transform, false);
            led.transform.position = radio.transform.position + new Vector3(-0.015f, 0.075f, 0.018f);
            led.transform.localScale = Vector3.one * 0.012f;
            led.GetComponent<Renderer>().sharedMaterial = BuildingBuilder.Mat("EF_LedRed", new Color(1f, 0.1f, 0.05f));
            led.GetComponent<Renderer>().enabled = false;
            var rb = radio.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            var g = radio.AddComponent<XRGrabInteractable>();
            g.movementType = XRBaseInteractable.MovementType.Instantaneous;
            g.throwOnDetach = false;
            RigRealismSetup.EnsureNearOnly(g, 0.12f);
            rb.isKinematic = true;
            g.gameObject.AddComponent<DropsWhenReleased>();
            var r = radio.AddComponent<Radio>();
            var so = new SerializedObject(r);
            so.FindProperty("txLight").objectReferenceValue = led.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildingBuilder.Box(dock, "AddressPlate", new Vector3(wallBase.x, 1.55f, wallBase.z + 0.11f), Quaternion.identity, new Vector3(0.7f, 0.36f, 0.015f), BuildingBuilder.Mat("EF_AddressRed", new Color(0.6f, 0.08f, 0.06f)), false);
            BuildingBuilder.Label(dock, "AddressText", new Vector3(wallBase.x, 1.55f, wallBase.z + 0.125f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.66f, 0.34f), 0.42f,
                "<b>CHÁY GỌI 114</b>\nPhòng Lab 302 — Tầng 3 — Tòa B1\nĐHBK Hà Nội, số 1 Đại Cồ Việt\n<size=80%>Cầm bộ đàm sát miệng, giữ nút để nói</size>");
            return r;
        }

        // ------------------------------------------------------------------ corridor: victim, Minh

        private static GameObject Person(string prefab, string name, Transform parent)
        {
            var model = LabSceneDresser.InstantiateNatural(PO + prefab, name, parent);
            if (model == null) return null;
            Bounds mb = LabSceneDresser.WorldBounds(model);
            float s = mb.size.y > 0.1f ? LabSceneDresser.HUMAN_HEIGHT / mb.size.y : 1f;
            model.transform.localScale *= s;
            model.transform.localRotation = Quaternion.identity;
            return model; // callers place it, then Pose() — the humanoid pose must be applied at the final placement
        }

        private static void Pose(GameObject model)
        {
            if (model != null) LabSceneDresser.ApplyStandingPose(model, new List<string>());
        }

        private static void Victim(Transform root)
        {
            var v = new GameObject("Victim_Nam").transform;
            v.SetParent(root, false);
            v.SetPositionAndRotation(VictimHeels, Quaternion.LookRotation(Vector3.left)); // heels east, head west towards the stairwell door
            var tilt = new GameObject("Tilt").transform;
            tilt.SetParent(v, false);
            var model = Person("SM_Chr_Developer_Male_02.prefab", "Person_Visual", tilt);
            if (model != null)
            {
                // pose while upright (humanoid pose is applied in world space), then lay him on his back:
                // the model's up (head) along +Z of the tilt pivot, its face up
                model.transform.localPosition = Vector3.zero;
                Pose(model);
                model.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                model.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            }
            var chest = new GameObject("Chest").transform;
            chest.SetParent(tilt, false);
            chest.localPosition = new Vector3(0f, 0.12f, 1.38f);
            v.gameObject.AddComponent<VictimBody>().Configure(tilt, chest, StairwellALanding, "Nam");
        }

        private static void Minh(Transform root)
        {
            var m = new GameObject("NPC_Minh_AtLift").transform;
            m.SetParent(root, false);
            m.SetPositionAndRotation(new Vector3(-2.35f, 0f, -11.45f), Quaternion.LookRotation(Vector3.left));
            var model = Person("SM_Chr_Developer_Male_01.prefab", "Person_Visual", m);
            if (model != null) LabSceneDresser.PlaceOnSurface(model, m.position);
            Pose(model);
            var lift = Object.FindFirstObjectByType<ElevatorTrap>();
            var building = GameObject.Find("[BUILDING_V2]");
            var stairs = building != null ? building.transform.Find("StairwellA/FireDoor_F3") : null;
            m.gameObject.AddComponent<PanicRunner>().Configure(lift, stairs, new Bounds(new Vector3(0f, 1.2f, -10.2f), new Vector3(6f, 3f, 6f)));
        }

        // ------------------------------------------------------------------ assembly point: warden, report board, evacuees

        private static void RollCall(Transform root, Vector3 p)
        {
            var ap = GameObject.Find("[BUILDING_V2]")?.transform.Find("AssemblyPoint");
            var point = (ap != null ? ap.gameObject : new GameObject("AssemblyPoint")).AddComponent<AssemblyPoint>();
            var group = new GameObject("RollCall").transform;
            group.SetParent(root, false);

            var warden = Person("SM_Chr_Security_Male_01.prefab", "Warden", group);
            if (warden != null)
            {
                warden.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                LabSceneDresser.PlaceOnSurface(warden, p + new Vector3(1.3f, 0f, -1.4f));
                Pose(warden);
            }
            Evacuee(group, "SM_Chr_Business_Female_01.prefab", "Evacuee_1", p + new Vector3(-1.6f, 0f, -0.6f), 30f);
            Evacuee(group, "SM_Chr_Cleaner_Male_01.prefab", "Evacuee_2", p + new Vector3(-1.9f, 0f, 0.4f), 60f);
            var hoa = Evacuee(group, "SM_Chr_Developer_Female_01.prefab", "Hoa_AtAssembly", p + new Vector3(-1.0f, 0f, -1.4f), 10f);
            var minh = Evacuee(group, "SM_Chr_Developer_Male_01.prefab", "Minh_AtAssembly", p + new Vector3(1.9f, 0f, 0.5f), -60f);

            // report board on a stand, facing the learner coming from the building (+z side)
            var board = new GameObject("ReportBoard").transform;
            board.SetParent(group, false);
            Vector3 b = p + new Vector3(0f, 0f, -1.2f);
            var face = Quaternion.Euler(0f, 180f, 0f); // labels readable from +z
            var post = BuildingBuilder.Mat("EF_Rail", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.9f);
            BuildingBuilder.Box(board, "Stand", b + Vector3.up * 0.6f, Quaternion.identity, new Vector3(0.06f, 1.2f, 0.06f), post, true);
            BuildingBuilder.Box(board, "Panel", b + new Vector3(0f, 1.35f, 0f), Quaternion.identity, new Vector3(0.9f, 0.75f, 0.03f), BuildingBuilder.Mat("EF_SignBlue", new Color(0.1f, 0.25f, 0.6f)), true);
            BuildingBuilder.Label(board, "Question", b + new Vector3(0f, 1.63f, 0.02f), face, new Vector2(0.86f, 0.16f), 0.5f,
                "<b>BÁO ĐỘI TRƯỞNG:</b> còn ai trong tòa nhà?");
            Button(board, "Report_AllOut", b + new Vector3(0f, 1.47f, 0.025f), "ĐỦ NGƯỜI — không còn ai", new Color(0.2f, 0.6f, 0.3f), point.ReportAllOut);
            Button(board, "Report_Stairwell", b + new Vector3(0f, 1.3f, 0.025f), "CÒN NGƯỜI — buồng thang A, tầng 3", new Color(0.8f, 0.5f, 0.1f), point.ReportStairwell);
            Button(board, "Report_CorridorLift", b + new Vector3(0f, 1.13f, 0.025f), "CÒN NGƯỜI — hành lang / thang máy tầng 3", new Color(0.8f, 0.3f, 0.1f), point.ReportCorridorOrLift);
            point.Configure(board.gameObject, hoa, minh);
        }

        private static GameObject Evacuee(Transform parent, string prefab, string name, Vector3 at, float yaw)
        {
            var e = Person(prefab, name, parent);
            if (e == null) return null;
            e.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            LabSceneDresser.PlaceOnSurface(e, at);
            Pose(e);
            return e;
        }

        private static void Button(Transform parent, string name, Vector3 pos, string text, Color color, UnityEngine.Events.UnityAction onPress)
        {
            var btn = BuildingBuilder.Box(parent, name, pos, Quaternion.identity, new Vector3(0.8f, 0.13f, 0.03f), BuildingBuilder.Mat("EF_Btn_" + name, color), true);
            BuildingBuilder.Label(btn.transform.parent, name + "_Text", pos + new Vector3(0f, 0f, 0.017f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.78f, 0.12f), 0.42f, "<b>" + text + "</b>");
            var si = btn.AddComponent<XRSimpleInteractable>();
            var poke = btn.AddComponent<PokeAction>();
            UnityEventTools.AddVoidPersistentListener(si.selectEntered, poke.Press);
            UnityEventTools.AddVoidPersistentListener(poke.OnPressed, onPress);
            RigRealismSetup.MakePokeable(si, Vector3.back, 0.009f);
        }
    }
}
