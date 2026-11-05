using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// M6 scene quality pass for the lab (BasicScene): replaces grey-box primitives with the project's real assets
    /// (PolygonOffice desks, computers, chairs, characters, fire alarm), grounds every object on the floor,
    /// mounts extinguishers on the nearest wall, fixes NPC setup, adds the smoke layer and wrist HUD.
    /// Idempotent: re-running skips objects already dressed. Run "Audit Lab Scene" afterwards.
    /// </summary>
    public static class LabSceneDresser
    {
        private const string LAB = "Assets/Scenes/" + SceneNames.LAB + ".unity";
        private const string PO = "Assets/PolygonOffice/Prefabs/";
        private const string DESK = PO + "Props/Furniture/SM_Prop_Desk_01.prefab";
        private const string CHAIR = PO + "Props/Furniture/SM_Prop_Chair_01.prefab";
        private const string COMPUTER = PO + "Props/Desk Props/SM_Prop_Computer_Setup_01.prefab";
        private const string ALARM = PO + "Props/Wall Props/SM_Prop_FireAlarm_Switch_01.prefab";
        private const string NPC_MINH = PO + "Characters/SM_Chr_Developer_Male_01.prefab";
        private const string NPC_HOA = PO + "Characters/SM_Chr_Developer_Female_01.prefab";
        private const string SMOKE_MATERIAL = "Assets/Settings/EF_SmokeLayer.mat";
        private const string VISUAL = "_Visual";
        private static float FLOOR_Y
        {
            get
            {
                var floor = GameObject.Find("Floor_HighRise");
                return floor != null && floor.TryGetComponent<Renderer>(out var r) ? r.bounds.max.y : 0f;
            }
        }
        private const float HUMAN_HEIGHT = 1.7f;

        [MenuItem("EscapeFire/M6/Dress Lab With Real Assets")]
        public static void DressLab()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(LAB, OpenSceneMode.Single);
            var log = new List<string>();

            FixPolygonOfficeMaterials(log);
            EnsureLighting(log);
            DressDesks(log);
            DressFireAlarm(log);
            MountExtinguishers(log);
            FixNpcs(log);
            PlaceSecondaryFireSlotsOnDesks(log);
            EnsureSmokeLayer(log);
            EnsureWristHud(log);
            EnsureDirectorExtras(log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] Dress Lab:\n- " + string.Join("\n- ", log));
            LabSceneAudit.Run();
        }

        [MenuItem("EscapeFire/M6/Rotate Lab Computers 180°")]
        public static void FlipComputers()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Computer" + VISUAL) t.Rotate(0f, 180f, 0f, Space.World);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------------ desks, computers, chairs

        private static void DressDesks(List<string> log)
        {
            int desks = 0;
            foreach (var desk in AllNamed(IsDeskName))
            {
                if (desk.Find("Desk" + VISUAL) != null) continue;
                var deskRenderer = desk.GetComponent<Renderer>();
                if (deskRenderer == null) continue;
                Bounds box = deskRenderer.bounds;

                var model = InstantiateFitted(DESK, "Desk" + VISUAL, desk, box, alignLongAxis: true);
                if (model == null) continue;
                deskRenderer.enabled = false; // keep BoxCollider for physics / placement
                float deskTop = WorldBounds(model).max.y;

                Vector3 towardChair = Vector3.back;
                var monitor = desk.Find("PC_Monitor");
                if (monitor != null && monitor.TryGetComponent<Renderer>(out var monitorRenderer))
                {
                    Vector3 offset = monitorRenderer.bounds.center - box.center;
                    offset.y = 0f;
                    towardChair = offset.sqrMagnitude > 1e-4f ? -offset.normalized : Vector3.back;

                    var computer = InstantiateNatural(COMPUTER, "Computer" + VISUAL, desk);
                    if (computer != null)
                    {
                        computer.transform.rotation = Quaternion.LookRotation(towardChair, Vector3.up);
                        PlaceOnSurface(computer, new Vector3(monitorRenderer.bounds.center.x, deskTop, monitorRenderer.bounds.center.z));
                        monitorRenderer.enabled = false;
                    }
                }

                var chair = InstantiateNatural(CHAIR, "Chair" + VISUAL, desk);
                if (chair != null)
                {
                    Vector3 chairPos = box.center + towardChair * (Mathf.Max(box.extents.x, box.extents.z) * 0.5f + 0.45f);
                    chair.transform.rotation = Quaternion.LookRotation(-towardChair, Vector3.up);
                    PlaceOnSurface(chair, new Vector3(chairPos.x, FLOOR_Y, chairPos.z));
                    AddBoundsCollider(chair);
                }
                desks++;
            }
            log.Add($"Dressed {desks} desk(s) with PolygonOffice desk + computer + chair.");
        }

        private static void DressFireAlarm(List<string> log)
        {
            var door = GameObject.Find("Main_Exit_Door_Double");
            foreach (var alarm in Object.FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Spec: the alarm sits beside the exit door (opposite the CO2 station), never on the door leaf.
                if (door != null && door.TryGetComponent<Renderer>(out var doorRenderer))
                {
                    Bounds d = doorRenderer.bounds;
                    Vector3 p = alarm.transform.position;
                    if (p.x > d.min.x - 0.15f && p.x < d.max.x + 0.15f)
                    {
                        var co2 = Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                            .FirstOrDefault(e => e.Type == ExtinguisherType.CO2);
                        bool co2OnLeft = co2 == null || co2.transform.position.x < d.center.x;
                        float x = co2OnLeft ? d.max.x + 0.35f : d.min.x - 0.35f;
                        alarm.transform.position = new Vector3(x, FLOOR_Y + 1.4f, p.z);
                        log.Add($"Moved fire alarm off the exit door to x={x:F2} (beside the door, opposite the CO2 station).");
                    }
                }
                if (alarm.transform.Find("Alarm" + VISUAL) != null) continue;
                if (!alarm.TryGetComponent<Renderer>(out var r)) continue;
                var model = InstantiateFitted(ALARM, "Alarm" + VISUAL, alarm.transform, r.bounds, alignLongAxis: false);
                if (model == null) continue;
                model.transform.rotation = alarm.transform.rotation;
                r.enabled = false;
                log.Add($"Fire alarm '{alarm.name}' now uses SM_Prop_FireAlarm_Switch_01.");
            }
        }

        // ------------------------------------------------------------------ extinguishers on walls

        private static void MountExtinguishers(List<string> log)
        {
            Physics.SyncTransforms();
            Vector3[] dirs = { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            foreach (var ext in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var t = ext.transform;
                string lower = ext.name.ToLowerInvariant();
                if (lower.Contains("water") || lower.Contains("foam"))
                {
                    var typeSo = new SerializedObject(ext);
                    var typeProp = typeSo.FindProperty("type");
                    if (typeProp.enumValueIndex != (int)ExtinguisherType.WaterFoam)
                    {
                        typeProp.enumValueIndex = (int)ExtinguisherType.WaterFoam;
                        typeSo.ApplyModifiedPropertiesWithoutUndo();
                        log.Add($"'{ext.name}' type set to WaterFoam (was CO2: the wrong-choice extinguisher was missing).");
                    }
                }
                Vector3 origin = t.position;
                bool decoy = ext.Type == ExtinguisherType.WaterFoam ||
                             new SerializedObject(ext).FindProperty("type").enumValueIndex == (int)ExtinguisherType.WaterFoam;
                if (decoy)
                {
                    // Water/foam decoy hangs on the solid east wall, 4 m from the live electrical panel:
                    // plausible in a real lab and it exercises Socratic trigger #1 (water near live panel).
                    origin = new Vector3(0f, 1.2f, WATER_DECOY_Z);
                    dirs = new[] { Vector3.right };
                }
                else
                {
                    dirs = new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
                }
                RaycastHit best = default;
                float bestDist = float.MaxValue;
                foreach (var d in dirs)
                {
                    foreach (var hit in Physics.RaycastAll(origin, d, decoy ? 20f : 4f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.IsChildOf(t) || hit.collider.attachedRigidbody != null) continue;
                        if (!IsWall(hit.collider)) continue;
                        if (hit.distance < bestDist) { bestDist = hit.distance; best = hit; }
                    }
                }

                if (bestDist == float.MaxValue)
                {
                    log.Add($"WARNING: no wall within 4 m of '{ext.name}' — left in place (kinematic, will not fall).");
                    continue;
                }

                Bounds b = WorldBounds(t.gameObject);
                float halfDepth = Mathf.Min(b.extents.x, b.extents.z);
                float bottomOffset = t.position.y - b.min.y;
                Vector3 target = best.point + best.normal * (halfDepth + 0.03f);
                target.y = FLOOR_Y + 0.55f + bottomOffset; // bracket height: bottom 55 cm above floor
                t.position = target;
                t.rotation = Quaternion.LookRotation(best.normal, Vector3.up);
                if (t.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
                log.Add($"Mounted '{ext.name}' on '{best.collider.name}' ({bestDist:F2} m away).");
            }
        }

        /// <summary>Solid walls only: never hang equipment on the glass panorama.</summary>
        private static bool IsWall(Collider c)
        {
            string n = c.name.ToLowerInvariant();
            return n.Contains("wall") && !n.Contains("glass") && !n.Contains("panorama");
        }

        private const float WATER_DECOY_Z = -2f;

        // ------------------------------------------------------------------ materials, lighting, NPC pose

        /// <summary>PolygonOffice materials lost their atlas textures (all white). Re-link Material_XX_Y → Texture_XX_Y.</summary>
        private static void FixPolygonOfficeMaterials(List<string> log)
        {
            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/PolygonOffice/Materials" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || !mat.HasProperty("_BaseMap") || mat.GetTexture("_BaseMap") != null) continue;

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                string texName = name.Contains("_Material_Screen_")
                    ? name.Replace("_Material_Screen_", "_Texture_Sceen_")
                    : name.Replace("_Material_", "_Texture_");
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/PolygonOffice/Textures/{texName}.png");
                if (tex == null) continue;

                mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
            AssetDatabase.SaveAssets();
            if (fixedCount > 0) log.Add($"Re-linked atlas textures on {fixedCount} PolygonOffice material(s) (were rendering white).");
        }

        /// <summary>The south wall (exit door, alarm, CO2) rendered black: give the room readable ambient light (cheap on Quest).</summary>
        private static void EnsureLighting(List<string> log)
        {
            if (RenderSettings.ambientMode == AmbientMode.Trilight && RenderSettings.ambientSkyColor.grayscale > 0.4f) return;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.64f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.50f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.29f, 0.28f);
            RenderSettings.ambientIntensity = 1f;
            log.Add("Lighting: trilight ambient so every wall (exit door, alarm, extinguishers) is readable.");
        }

        /// <summary>Synty characters import in T-pose and the pack has no animations: set a relaxed standing pose via humanoid muscles.</summary>
        private static void ApplyStandingPose(GameObject model, List<string> log)
        {
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                log.Add($"WARNING: '{model.name}' has no humanoid avatar; pose unchanged.");
                return;
            }

            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            SetMuscle(ref pose, "Left Arm Down-Up", -0.75f);
            SetMuscle(ref pose, "Right Arm Down-Up", -0.75f);
            SetMuscle(ref pose, "Left Arm Front-Back", 0.15f);
            SetMuscle(ref pose, "Right Arm Front-Back", 0.15f);
            SetMuscle(ref pose, "Left Forearm Stretch", 0.85f);
            SetMuscle(ref pose, "Right Forearm Stretch", 0.85f);
            SetMuscle(ref pose, "Left Shoulder Down-Up", -0.2f);
            SetMuscle(ref pose, "Right Shoulder Down-Up", -0.2f);
            Vector3 rootPos = animator.transform.position;
            Quaternion rootRot = animator.transform.rotation;
            handler.SetHumanPose(ref pose);
            animator.transform.SetPositionAndRotation(rootPos, rootRot); // muscles only, keep placement
            handler.Dispose();

            animator.enabled = false; // no controller in this pack; keep the authored pose at runtime
            EditorUtility.SetDirty(model);
        }

        private static void SetMuscle(ref HumanPose pose, string muscle, float value)
        {
            int index = System.Array.IndexOf(HumanTrait.MuscleName, muscle);
            if (index >= 0 && index < pose.muscles.Length) pose.muscles[index] = value;
        }

        // ------------------------------------------------------------------ NPCs

        private static void FixNpcs(List<string> log)
        {
            var door = GameObject.Find("Main_Exit_Door_Double");
            var npcs = Object.FindObjectsByType<NPCSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                bool isHoa = npc.name.ToLowerInvariant().Contains("hoa");
                var so = new SerializedObject(npc);
                so.FindProperty("role").enumValueIndex = (int)(isHoa ? NPCRole.HoaFaint : NPCRole.MinhPanic);
                so.FindProperty("reactionDelaySeconds").floatValue = isHoa ? 20f : 2f;
                if (door != null) so.FindProperty("exitDoorTarget").objectReferenceValue = door.transform;

                // Root pivot at the feet, upright, on the floor; collider sized for a person.
                var t = npc.transform;
                Vector3 p = t.position;
                if (isHoa) p = FindFreeSpotNear(p, 1.0f);
                t.position = new Vector3(p.x, FLOOR_Y, p.z);
                t.rotation = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
                t.localScale = Vector3.one;
                if (npc.TryGetComponent<CapsuleCollider>(out var capsule))
                {
                    capsule.center = new Vector3(0f, 0.9f, 0f);
                    capsule.height = 1.8f;
                    capsule.radius = 0.3f;
                    capsule.direction = 1;
                }
                if (npc.TryGetComponent<MeshRenderer>(out var capsuleRenderer)) capsuleRenderer.enabled = false;

                var visual = t.Find("Person" + VISUAL);
                if (visual == null)
                {
                    var model = InstantiateNatural(isHoa ? NPC_HOA : NPC_MINH, "Person" + VISUAL, t);
                    if (model != null)
                    {
                        Bounds mb = WorldBounds(model);
                        float s = mb.size.y > 0.1f ? HUMAN_HEIGHT / mb.size.y : 1f;
                        model.transform.localScale *= s;
                        model.transform.localRotation = Quaternion.identity;
                        PlaceOnSurface(model, new Vector3(t.position.x, FLOOR_Y, t.position.z));
                        visual = model.transform;
                    }
                }
                if (visual != null)
                {
                    so.FindProperty("visualRoot").objectReferenceValue = visual;
                    ApplyStandingPose(visual.gameObject, log);
                }

                if (!isHoa)
                {
                    var chair = NearestNamed(t.position, "Chair" + VISUAL);
                    if (chair != null) so.FindProperty("chairToKnockOver").objectReferenceValue = chair.gameObject;
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                if (isHoa && npc.GetComponent<XRSimpleInteractable>() == null)
                {
                    var interactable = npc.gameObject.AddComponent<XRSimpleInteractable>();
                    UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, npc.RescueNPC);
                }
                log.Add($"NPC '{npc.name}': role={(isHoa ? "HoaFaint" : "MinhPanic")}, grounded, PolygonOffice character, exit={(door != null ? door.name : "MISSING")}{(isHoa ? ", rescue on select" : "")}.");
            }
        }

        private static Vector3 FindFreeSpotNear(Vector3 p, float step)
        {
            Physics.SyncTransforms();
            Vector3[] offsets = { Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (var o in offsets)
            {
                Vector3 c = new Vector3(p.x, 0.9f, p.z) + o * step;
                var hits = Physics.OverlapCapsule(c + Vector3.up * 0.5f, c - Vector3.up * 0.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                if (hits.All(h => h.attachedRigidbody != null || h.GetComponent<NPCSimulator>() != null || h.name.Contains("Floor")))
                    return c;
            }
            return p;
        }

        // ------------------------------------------------------------------ secondary fire slots

        private static void PlaceSecondaryFireSlotsOnDesks(List<string> log)
        {
            var slots = Object.FindObjectsByType<SecondaryFireSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (slots.Length == 0) return;
            var door = GameObject.Find("Main_Exit_Door_Double");
            Vector3 exit = door != null ? door.transform.position : new Vector3(0f, 0f, -7f);

            var desks = AllNamed(IsDeskName)
                .OrderByDescending(d => Vector3.Distance(d.position, exit))
                .ToList();
            for (int i = 0; i < slots.Length && i < desks.Count; i++)
            {
                var deskModel = desks[i].Find("Desk" + VISUAL);
                Bounds b = deskModel != null ? WorldBounds(deskModel.gameObject) : desks[i].GetComponent<Renderer>().bounds;
                slots[i].transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
                log.Add($"SecondaryFireSlot '{slots[i].name}' placed on top of '{desks[i].name}' ({Vector3.Distance(desks[i].position, exit):F1} m from exit).");
            }
        }

        // ------------------------------------------------------------------ smoke layer

        private static void EnsureSmokeLayer(List<string> log)
        {
            if (Object.FindFirstObjectByType<SmokeDensityController>(FindObjectsInactive.Include) != null) return;
            var controller = Object.FindFirstObjectByType<LabProgressiveScenarioController>(FindObjectsInactive.Include);
            var floor = GameObject.Find("Floor_HighRise");
            var ceiling = GameObject.Find("Ceiling_HighRise");
            Bounds room = floor != null && floor.TryGetComponent<Renderer>(out var fr) ? fr.bounds : new Bounds(Vector3.zero, new Vector3(18f, 0.2f, 14f));
            float ceilingY = ceiling != null && ceiling.TryGetComponent<Renderer>(out var cr) ? cr.bounds.min.y : 4.5f;

            var layer = new GameObject("[SMOKE_LAYER]");
            layer.transform.position = new Vector3(room.center.x, ceilingY - 0.1f, room.center.z);
            var smoke = layer.AddComponent<SmokeDensityController>();

            var volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volume.name = "SmokeVolume";
            Object.DestroyImmediate(volume.GetComponent<Collider>());
            volume.transform.SetParent(layer.transform, false);
            float height = ceilingY + 1f;
            volume.transform.localScale = new Vector3(room.size.x - 0.1f, height, room.size.z - 0.1f);
            volume.transform.localPosition = new Vector3(0f, height * 0.5f, 0f); // bottom face = layer height
            var renderer = volume.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SmokeMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var so = new SerializedObject(smoke);
            so.FindProperty("initialSmokeHeight").floatValue = ceilingY - 0.1f;
            so.FindProperty("minSmokeHeight").floatValue = 1.0f;
            so.FindProperty("smokeLoweringSpeed").floatValue = 0.03f;
            so.FindProperty("targetFire").objectReferenceValue = controller != null ? controller.ServerFire : null;
            so.FindProperty("smokeVolumeTransform").objectReferenceValue = layer.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"Added smoke layer over the room (ceiling {ceilingY:F2} m → min 1.0 m, lowers 3 cm/s while the server fire burns).");
        }

        private static Material SmokeMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SMOKE_MATERIAL);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface", 1f);     // transparent
            mat.SetFloat("_Blend", 0f);       // alpha
            mat.SetFloat("_Cull", 0f);        // both faces: visible from inside the smoke
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.12f, 0.55f));
            AssetDatabase.CreateAsset(mat, SMOKE_MATERIAL);
            return mat;
        }

        // ------------------------------------------------------------------ wrist HUD

        private static void EnsureWristHud(List<string> log)
        {
            if (Object.FindFirstObjectByType<VRWristHUD>(FindObjectsInactive.Include) != null) return;
            var wrist = GameObject.Find("Left Controller");
            if (wrist == null) wrist = GameObject.Find("LeftHand Controller");
            if (wrist == null)
            {
                log.Add("WARNING: 'Left Controller' not found — wrist HUD not created.");
                return;
            }

            var hud = new GameObject("WristHUD");
            hud.transform.SetParent(wrist.transform, false);
            hud.transform.localPosition = new Vector3(0f, 0.05f, -0.05f);
            hud.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            var component = hud.AddComponent<VRWristHUD>();

            var so = new SerializedObject(component);
            so.FindProperty("oxygenText").objectReferenceValue = WristText(hud.transform, "Oxygen", 0.045f, 0.25f);
            so.FindProperty("healthText").objectReferenceValue = WristText(hud.transform, "Health", 0.025f, 0.25f);
            so.FindProperty("crouchStatusText").objectReferenceValue = WristText(hud.transform, "Crouch", 0.005f, 0.18f);
            so.FindProperty("aiMentorText").objectReferenceValue = WristText(hud.transform, "Mentor", -0.035f, 0.14f, 0.05f);
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"Added wrist HUD on '{wrist.name}' (oxygen, health, posture, AI mentor).");
        }

        private static TextMeshPro WristText(Transform parent, string name, float y, float size, float height = 0.02f)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(0.12f, height);
            return tmp;
        }

        private static void EnsureDirectorExtras(List<string> log)
        {
            var director = FindOrCreate("[AI_DIRECTOR_SYSTEM]");
            if (Object.FindFirstObjectByType<FrameTimeMonitor>(FindObjectsInactive.Include) == null)
            {
                director.AddComponent<FrameTimeMonitor>();
                log.Add("Added FrameTimeMonitor (frame-time stats in the report).");
            }
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go : new GameObject(name);
        }

        /// <summary>Grey-box desks are named Desk_1..Desk_12; excludes generated children like Desk_Visual.</summary>
        private static bool IsDeskName(string n) => n.StartsWith("Desk_") && n.Length > 5 && char.IsDigit(n[5]);

        private static IEnumerable<Transform> AllNamed(System.Func<string, bool> predicate) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => predicate(t.name));

        private static Transform NearestNamed(Vector3 p, string name) =>
            AllNamed(n => n == name).OrderBy(t => Vector3.Distance(t.position, p)).FirstOrDefault();

        private static GameObject InstantiateNatural(string path, string name, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[EscapeFire] Asset not found: {path}");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            go.name = name;
            go.transform.SetParent(parent, true);
            // Compensate the parent's (often non-uniform) grey-box scale so the model keeps its real size.
            Vector3 ls = parent.lossyScale;
            go.transform.localScale = new Vector3(1f / Safe(ls.x), 1f / Safe(ls.y), 1f / Safe(ls.z));
            return go;
        }

        /// <summary>Instantiates a prefab and uniformly scales it so its footprint matches the target bounds; bottom on the target's bottom.</summary>
        private static GameObject InstantiateFitted(string path, string name, Transform parent, Bounds target, bool alignLongAxis)
        {
            var go = InstantiateNatural(path, name, parent);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;

            Bounds b = WorldBounds(go);
            if (alignLongAxis && (b.size.x > b.size.z) != (target.size.x > target.size.z))
            {
                go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                b = WorldBounds(go);
            }

            float sx = target.size.x / Safe(b.size.x);
            float sz = target.size.z / Safe(b.size.z);
            float sy = target.size.y / Safe(b.size.y);
            float s = alignLongAxis ? Mathf.Clamp(Mathf.Min(sx, sz), 0.6f, 1.6f) : Mathf.Min(sx, sy, sz);
            go.transform.localScale *= s;
            PlaceOnSurface(go, new Vector3(target.center.x, target.min.y, target.center.z));
            return go;
        }

        /// <summary>Moves the object so its world-bounds bottom-centre sits exactly at <paramref name="bottomCentre"/>.</summary>
        private static void PlaceOnSurface(GameObject go, Vector3 bottomCentre)
        {
            Bounds b = WorldBounds(go);
            Vector3 delta = bottomCentre - new Vector3(b.center.x, b.min.y, b.center.z);
            go.transform.position += delta;
        }

        private static void AddBoundsCollider(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>() != null) return;
            Bounds b = WorldBounds(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            Vector3 ls = go.transform.lossyScale;
            box.size = new Vector3(b.size.x / Safe(ls.x), b.size.y / Safe(ls.y), b.size.z / Safe(ls.z));
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static float Safe(float v) => Mathf.Abs(v) < 1e-4f ? 1f : v;
    }
}
