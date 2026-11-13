using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Player;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// Static quality gate for the lab scene ("is everything where it should be?"). Reports:
    /// missing scripts, pink/missing materials, visible grey-box placeholders, dynamic rigidbodies with nothing
    /// underneath (would fall forever), renderers outside the room, NPC setup, required systems.
    /// </summary>
    public static class LabSceneAudit
    {
        private static readonly string[] PLACEHOLDER_ALLOWED =
        {
            "wall", "floor", "ceiling", "hallway", "door", "skyline", "skyscraper", "smokevolume", "plane", "panorama", "glass_"
        };

        [MenuItem("EscapeFire/Audit Lab Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Collect(out var errors, out var warnings);
            var sb = new StringBuilder($"[EscapeFire] Audit '{scene.name}': {errors.Count} error(s), {warnings.Count} warning(s)\n");
            foreach (var e in errors) sb.AppendLine("  ERROR " + e);
            foreach (var w in warnings) sb.AppendLine("  WARN  " + w);
            if (errors.Count > 0) Debug.LogError(sb.ToString());
            else if (warnings.Count > 0) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString() + "  All checks passed.");
        }

        /// <summary>Opens the lab scene and audits it (batchmode: -executeMethod EscapeFire.EditorTools.LabSceneAudit.RunOnLab).</summary>
        public static void RunOnLab()
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{EscapeFire.Core.SceneNames.LAB}.unity", OpenSceneMode.Single);
            Run();
        }

        /// <summary>Runs every check on the active scene (used by the menu and by the EditMode scene test).</summary>
        public static void Collect(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            var floor = GameObject.Find("Floor_HighRise");
            var ceiling = GameObject.Find("Ceiling_HighRise");
            Bounds room = floor != null && floor.TryGetComponent<Renderer>(out var fr) ? fr.bounds : new Bounds(Vector3.zero, new Vector3(18f, 0.2f, 14f));
            float ceilingY = ceiling != null && ceiling.TryGetComponent<Renderer>(out var cr) ? cr.bounds.max.y : 5f;
            Physics.SyncTransforms();

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string path = PathOf(go.transform);
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                    errors.Add($"Missing script on '{path}'.");

                if (!go.activeInHierarchy) continue;
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled && !(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer))
                {
                    foreach (var m in renderer.sharedMaterials)
                    {
                        if (m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                        {
                            errors.Add($"Missing/pink material on '{path}'.");
                            break;
                        }
                    }

                    if (IsPrimitive(go) && !IsAllowedPlaceholder(path))
                        warnings.Add($"Grey-box primitive still visible: '{path}' (replace with a real asset).");

                    if (!IsAllowedPlaceholder(path))
                    {
                        Bounds b = renderer.bounds;
                        bool inHallway = path.ToLowerInvariant().Contains("hallway");
                        bool horizontallyInside = b.center.x > room.min.x - 0.5f && b.center.x < room.max.x + 0.5f &&
                                                  b.center.z > room.min.z - 0.5f && b.center.z < room.max.z + 0.5f;
                        // the storeys under the lab (facade windows of the V6 dressing) are outside the building, not in the room
                        bool exterior = path.StartsWith("[DRESSING]/Facade") || path.StartsWith("[DRESSING]/StairWindows");
                        if (horizontallyInside && !exterior && (b.max.y < room.min.y - 0.05f || b.min.y > ceilingY + 0.05f))
                            errors.Add($"'{path}' is below the floor or above the ceiling (y {b.min.y:F2}..{b.max.y:F2}).");
                        if (!horizontallyInside && !inHallway && b.size.magnitude < 5f && b.center.y > -1f)
                            warnings.Add($"'{path}' is outside the lab footprint at ({b.center.x:F1}, {b.center.y:F1}, {b.center.z:F1}).");
                    }
                }

                var rb = go.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic && rb.useGravity)
                {
                    Vector3 origin = go.transform.position + Vector3.up * 0.05f;
                    bool supported = false;
                    foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (!hit.transform.IsChildOf(go.transform)) { supported = true; break; }
                    }
                    if (!supported) errors.Add($"Dynamic Rigidbody '{path}' has no collider below it — it will fall forever.");
                    else if (go.GetComponentInChildren<Collider>() == null) errors.Add($"Dynamic Rigidbody '{path}' has no collider — it will fall through the floor.");
                }
            }

            // NPCs
            var npcs = Object.FindObjectsByType<NPCSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var roles = new HashSet<NPCRole>();
            foreach (var npc in npcs)
            {
                if (!roles.Add(npc.Role)) errors.Add($"Two NPCs share role {npc.Role} ('{npc.name}').");
                var so = new SerializedObject(npc);
                if (so.FindProperty("exitDoorTarget").objectReferenceValue == null) errors.Add($"NPC '{npc.name}' has no exitDoorTarget.");
                if (npc.transform.Find("Person_Visual") == null) warnings.Add($"NPC '{npc.name}' has no character model (still a capsule).");
                if (Mathf.Abs(npc.transform.position.y - room.max.y) > 0.15f)
                    warnings.Add($"NPC '{npc.name}' feet at y={npc.transform.position.y:F2}, floor at {room.max.y:F2}.");
                if (npc.Role == NPCRole.HoaFaint && npc.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>() == null)
                    errors.Add($"NPC '{npc.name}' cannot be rescued (no XR interactable).");

                // Body must not clip through furniture (people standing inside chairs/desks look broken in VR).
                Vector3 feet = npc.transform.position;
                foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * 0.35f, feet + Vector3.up * 1.45f, 0.22f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (c.transform.IsChildOf(npc.transform) || c.name.Contains("Floor") || c.name == "Plane") continue;
                    errors.Add($"NPC '{npc.name}' intersects '{PathOf(c.transform)}'.");
                }
            }

            // Required systems
            Require<LabProgressiveScenarioController>(errors);
            Require<ScoringManager>(errors);
            Require<SessionOutcomeReporter>(errors);
            Require<OxygenSystem>(errors);
            Require<HealthSystem>(errors);
            Require<VRHeightDetector>(errors);
            Require<SmokeDensityController>(errors);
            Require<VRWristHUD>(warnings);
            Require<EvacuationExitZone>(errors);
            Require<FireAlarmButton>(errors);
            Require<CircuitBreakerSwitch>(errors);
            Require<PerformanceBudgetGovernor>(warnings);
            Require<CognitiveFrictionTracker>(warnings);
            Require<MicroAIGovernor>(warnings);
            Require<SessionFactsCollector>(warnings);
            Require<MentorVoice>(warnings);

            bool hasCo2 = false, hasConductive = false;
            foreach (var ext in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ext.Type == ExtinguisherType.CO2) hasCo2 = true;
                if (ext.Type == ExtinguisherType.WaterFoam) hasConductive = true;
                if (ext.GetComponent<ExtinguisherXRBinding>() == null) errors.Add($"Extinguisher '{ext.name}' cannot be grabbed/sprayed (no ExtinguisherXRBinding).");
            }
            if (!hasCo2) errors.Add("No CO2 extinguisher: the correct choice for an electrical fire is missing.");
            if (!hasConductive) errors.Add("No water/foam extinguisher: the PCCC 'wrong choice' training case is missing.");
        }

        private static void Require<T>(List<string> list) where T : Object
        {
            if (Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) == null) list.Add($"Missing {typeof(T).Name} in scene.");
        }

        private static bool IsPrimitive(GameObject go)
        {
            if (!go.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null) return false;
            string n = mf.sharedMesh.name;
            return n == "Cube" || n == "Capsule" || n == "Sphere" || n == "Cylinder";
        }

        private static bool IsAllowedPlaceholder(string path)
        {
            string p = path.ToLowerInvariant();
            foreach (var a in PLACEHOLDER_ALLOWED) if (p.Contains(a)) return true;
            return false;
        }

        private static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }
    }
}
