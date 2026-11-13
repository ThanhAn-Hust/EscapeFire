using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V5.4 — rendering cost at the Quest quality level (the one Android uses) from the main viewpoints, with the fire and
    /// smoke going: draw calls (batches), SetPass calls, triangles, lights, live particles and frame time. Results go to
    /// Logs/perf/v5_perf.json. Editor GPU timing is only a relative measure; draw calls / SetPass / triangles carry over.
    /// </summary>
    [Category("Perf")]
    public class V5PerfTests : BuildingSceneTest
    {
        private static int QuestQualityLevel()
        {
            // QualitySettings.asset: m_PerPlatformDefaultQuality Android = 1 ("Low", Performance URP Config)
            return Mathf.Min(1, QualitySettings.names.Length - 1);
        }

        private struct View { public string Name; public Vector3 Origin; public float Yaw; }

        private static readonly View[] Views =
        {
            new View { Name = "lab_overview", Origin = new Vector3(-0.5f, 0f, -4.5f), Yaw = 40f },
            new View { Name = "corridor", Origin = new Vector3(0.2f, 0f, -8.0f), Yaw = 180f },
            new View { Name = "stairwell_a", Origin = new Vector3(0.6f, 0f, -14.0f), Yaw = 180f },
            new View { Name = "yard", Origin = new Vector3(-7f, -8f, -22.5f), Yaw = 20f },
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator RenderingCost_AtQuestQuality_MainViewpoints()
        {
            // rendering cost needs a GPU: under -nographics there is nothing to measure (and Camera.Render crashes)
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("needs a graphics device (run with Tools/run_playmode.sh)");
            int before = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(QuestQualityLevel(), true);
            try
            {
                yield return Setup(TrainingMode.Guided, Views[0].Origin, Views[0].Yaw);
                Application.targetFrameRate = -1;
                QualitySettings.vSyncCount = 0;
                LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                yield return Xr.Wait(3f); // fire and smoke running
                var rows = new List<Dictionary<string, object>>();
                foreach (var v in Views)
                {
                    PlaceRig(v.Origin);
                    Body(v.Yaw);
                    yield return Xr.Wait(0.8f);
                    Shot("perf_" + v.Name); // render this view first: the editor stats describe the last rendered frame
                    yield return null;
                    var frames = new List<float>();
                    for (int i = 0; i < 120; i++) { yield return null; frames.Add(Time.unscaledDeltaTime * 1000f); }
                    frames.Sort();
                    Xr.Camera.Render(); // the editor stats describe the last render: render this view now
                    var row = new Dictionary<string, object>
                    {
                        { "view", v.Name },
                        { "frame_ms_avg", System.Math.Round(frames.Average(), 2) },
                        { "frame_ms_p95", System.Math.Round(frames[(int)(frames.Count * 0.95f)], 2) },
                        { "lights", Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled) },
                        { "shadow_lights", Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled && l.shadows != LightShadows.None) },
                        { "particles", Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Sum(p => p.particleCount) },
                    };
#if UNITY_EDITOR
                    row["batches"] = UnityEditor.UnityStats.batches;
                    row["setpass"] = UnityEditor.UnityStats.setPassCalls;
                    row["triangles"] = UnityEditor.UnityStats.triangles;
                    row["shadow_casters"] = UnityEditor.UnityStats.shadowCasters;
#endif
                    // what is drawn: visible renderers by top-level object, and how many are dynamic / skinned
                    var visible = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.isVisible && r.enabled).ToList();
                    row["visible_renderers"] = visible.Count;
                    row["visible_skinned"] = visible.Count(r => r is SkinnedMeshRenderer);
                    row["visible_particles"] = visible.Count(r => r is ParticleSystemRenderer);
                    row["visible_dynamic_meshes"] = visible.Count(r => r is MeshRenderer && !r.gameObject.isStatic);
                    row["top_roots"] = string.Join("; ", visible.GroupBy(r => r.transform.root.name).OrderByDescending(g => g.Count()).Take(6)
                        .Select(g => $"{g.Key}={g.Count()}({g.Count(r => !r.gameObject.isStatic)} dyn)"));
                    rows.Add(row);
                    Debug.Log("[Perf] " + string.Join(", ", row.Select(kv => kv.Key + "=" + kv.Value)));
                }
                Directory.CreateDirectory("Logs/perf");
                File.WriteAllText("Logs/perf/v5_perf.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                    pipeline = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline.name : "default",
                    views = rows
                }, Newtonsoft.Json.Formatting.Indented));
                Assert.AreEqual(Views.Length, rows.Count);
            }
            finally
            {
                QualitySettings.SetQualityLevel(before, true);
                QualitySettings.vSyncCount = 1;
            }
        }
    }
}
