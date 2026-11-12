using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using EscapeFire.Generation;
using EscapeFire.Hazards;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V4 — prompt-generated rooms (plan V4.2/V4.4). Saves the lab's working extinguishers and fire as prefabs, maps the
    /// catalog categories to PolygonOffice models (ScenarioAssetLibrary in Resources) and creates the scene
    /// 03_Generated (XR rig + ScenarioBuilder + GeneratedDrill) that builds a room from a ScenarioSpec at runtime.
    /// </summary>
    public static class GeneratedSceneBuilder
    {
        private const string LAB = "Assets/Scenes/BasicScene.unity";
        private const string PATH = "Assets/Scenes/03_Generated.unity";
        private const string PREFABS = "Assets/Generated/Prefabs";
        private const string LIBRARY = "Assets/Resources/ScenarioAssetLibrary.asset";
        private const string XR_RIG = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string FONT = "Assets/TextMesh Pro/Resources/Fonts & Materials/EF_Vietnamese SDF.asset";
        private const string PO = "Assets/PolygonOffice/Prefabs";

        /// <summary>Catalog category -> PolygonOffice prefab names (first list) and optional toppers (second).</summary>
        private static readonly Dictionary<string, (string[] models, string[] toppers)> Map = new Dictionary<string, (string[], string[])>
        {
            { "desk", (new[] { "SM_Prop_Desk_01", "SM_Prop_Desk_02", "SM_Prop_Desk_03", "SM_Prop_Desk_06" }, new string[0]) },
            { "computer_desk", (new[] { "SM_Prop_Desk_04", "SM_Prop_Desk_05" }, new[] { "SM_Prop_Computer_Setup_01", "SM_Prop_Computer_Setup_02" }) },
            { "chair", (new[] { "SM_Prop_Chair_01", "SM_Prop_Chair_02", "SM_Prop_Chair_03", "SM_Prop_Chair_04", "SM_Prop_Chair_05" }, new string[0]) },
            { "table", (new[] { "SM_Prop_Table_01", "SM_Prop_Table_02", "SM_Prop_Table_03" }, new string[0]) },
            { "round_table", (new[] { "SM_Prop_Table_Round_01", "SM_Prop_Table_Round_02" }, new string[0]) },
            { "conference_table", (new[] { "SM_Prop_Table_Conference_01", "SM_Prop_Table_Conference_02", "SM_Prop_Table_Conference_03" }, new string[0]) },
            { "drafting_table", (new[] { "SM_Prop_Table_Drafting_01" }, new string[0]) },
            { "bookshelf", (new[] { "SM_Prop_Shelf_01", "SM_Prop_Shelf_02", "SM_Prop_Shelf_03", "SM_Prop_Shelf_04" }, new string[0]) },
            { "cabinet", (new[] { "SM_Prop_Cabinets_01", "SM_Prop_Cabinets_02", "SM_Prop_Cabinets_03" }, new string[0]) },
            { "server_rack", (new[] { "SM_Prop_Server_Cabinet_01_Full", "SM_Prop_Server_Cabinet_02_Full", "SM_Prop_Server_Cabinet_03_Full" }, new string[0]) },
            { "chemical_cabinet", (new[] { "SM_Prop_Shelf_05", "SM_Prop_Shelf_06", "SM_Prop_Kitchen_Cabinet_01" }, new string[0]) },
            { "kitchen_counter", (new[] { "SM_Prop_Kitchen_Counter_01", "SM_Prop_Kitchen_CounterSink_01", "SM_Prop_Kitchen_CounterSink_Dishes_01" }, new string[0]) },
            { "fridge", (new[] { "SM_Prop_Fridge_01", "SM_Prop_Fridge_02" }, new string[0]) },
            { "vending_machine", (new[] { "SM_Prop_VendingMachine_01" }, new string[0]) },
            { "couch", (new[] { "SM_Prop_Couch_01", "SM_Prop_Couch_02", "SM_Prop_Couch_03", "SM_Prop_Couch_04" }, new string[0]) },
            { "printer", (new[] { "SM_Prop_Photocopier_01", "SM_Prop_Photocopier_02" }, new string[0]) },
            { "trash_bin", (new[] { "SM_Prop_Bin_01", "SM_Prop_Bin_02", "SM_Prop_Bin_03", "SM_Prop_Bin_Rubbish_01" }, new string[0]) },
            { "cardboard_boxes", (new[] { "SM_Prop_CardboardBox_Stack_01", "SM_Prop_CardboardBox_01", "SM_Prop_CardboardBox_03" }, new string[0]) },
            { "cart", (new[] { "SM_Prop_Cart_01", "SM_Prop_Cart_02", "SM_Prop_Janitor_Cart_01" }, new string[0]) },
            { "divider", (new[] { "SM_Prop_Divider_01", "SM_Prop_Divider_Half_01" }, new string[0]) },
            { "plant", (new[] { "SM_Prop_Plant_01", "SM_Prop_Plant_02", "SM_Prop_Plant_03", "SM_Prop_Plant_05", "SM_Prop_Plant_07" }, new string[0]) }
        };

        private static readonly string[] People =
        {
            "SM_Chr_Developer_Female_02", "SM_Chr_Business_Male_02", "SM_Chr_Business_Female_03", "SM_Chr_Cleaner_Female_01", "SM_Chr_Business_Male_03"
        };

        [MenuItem("EscapeFire/V4/Build Generated Scene (prompt -> room)")]
        public static void Run()
        {
            var log = new List<string>();
            EditorSceneManager.OpenScene(LAB, OpenSceneMode.Single);
            System.IO.Directory.CreateDirectory(PREFABS);
            var co2 = SavePrefab("[ZONE_C1_CO2_EXTINGUISHER]", "Extinguisher_CO2", log);
            var water = SavePrefab("[ZONE_C2_WATER_EXTINGUISHER]", "Extinguisher_WaterFoam", log);
            var fire = SavePrefab("[ZONE_A_SERVER_RACK]", "Fire", log);

            var lib = AssetDatabase.LoadAssetAtPath<ScenarioAssetLibrary>(LIBRARY);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<ScenarioAssetLibrary>();
                AssetDatabase.CreateAsset(lib, LIBRARY);
            }
            lib.categories.Clear();
            var all = AssetDatabase.FindAssets("t:Prefab", new[] { PO }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            GameObject Load(string name) => all.Where(p => System.IO.Path.GetFileNameWithoutExtension(p) == name)
                                                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
            int missing = 0;
            foreach (var kv in Map)
            {
                var models = kv.Value.models.Select(Load).Where(g => g != null).ToArray();
                var toppers = kv.Value.toppers.Select(Load).Where(g => g != null).ToArray();
                missing += kv.Value.models.Length - models.Length;
                lib.categories.Add(new ScenarioAssetLibrary.Entry { category = kv.Key, prefabs = models, toppers = toppers });
            }
            lib.co2Extinguisher = co2;
            lib.waterExtinguisher = water;
            lib.fire = fire;
            lib.people = People.Select(Load).Where(g => g != null).ToArray();
            lib.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            log.Add($"library: {lib.categories.Count} categories, {lib.PrefabCount} prefabs ({missing} names not found), {lib.people.Length} people");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.6f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(XR_RIG));
            rig.transform.position = Vector3.zero;
            var sys = new GameObject("[SYSTEMS]");
            sys.AddComponent<PerformanceBudgetGovernor>();
            var gen = new GameObject("[GENERATED]");
            var drill = gen.AddComponent<GeneratedDrill>();
            gen.AddComponent<ScenarioBuilder>().Configure(lib, drill);
            RigRealismSetup.Apply(log);
            EditorSceneManager.SaveScene(scene, PATH);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == PATH)) { scenes.Add(new EditorBuildSettingsScene(PATH, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Debug.Log("[EscapeFire] V4 generated scene: " + string.Join("; ", log));
        }

        private static GameObject SavePrefab(string sceneObject, string name, List<string> log)
        {
            var go = GameObject.Find(sceneObject);
            if (go == null) { log.Add("missing " + sceneObject); return null; }
            string path = $"{PREFABS}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
            log.Add(ok ? "saved " + path : "FAILED " + path);
            return prefab;
        }
    }
}
