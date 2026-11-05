using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// Renders the lab from fixed viewpoints to Logs/Snapshots/*.png so placement can be reviewed without a headset
    /// (and from batchmode: -executeMethod EscapeFire.EditorTools.SceneSnapshots.CaptureLab).
    /// </summary>
    public static class SceneSnapshots
    {
        private static readonly (string name, Vector3 pos, Vector3 lookAt)[] VIEWS =
        {
            ("01_overview_from_door", new Vector3(0f, 3.8f, -6.2f), new Vector3(0f, 0.5f, 2f)),
            ("02_player_eye_center", new Vector3(-1.5f, 1.6f, -4.5f), new Vector3(2f, 1.0f, 2f)),
            ("03_server_and_panel", new Vector3(3f, 1.7f, 0.5f), new Vector3(7.5f, 1.0f, 3.5f)),
            ("04_desks_and_npcs", new Vector3(7f, 1.7f, -4.5f), new Vector3(2f, 0.6f, 0f)),
            ("05_west_wall_water_ext", new Vector3(-3f, 1.6f, -2f), new Vector3(-8.5f, 1.0f, 0f)),
            ("06_exit_door_alarm_co2", new Vector3(0f, 1.7f, -1.5f), new Vector3(0f, 1.2f, -7f)),
            ("07_top_down", new Vector3(0f, 14f, 0f), new Vector3(0f, 0f, 0.01f)),
        };

        [MenuItem("EscapeFire/Capture Lab Snapshots")]
        public static void CaptureLab()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BasicScene.unity", OpenSceneMode.Single);
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Snapshots");
            Directory.CreateDirectory(dir);

            // The ceiling hides the top-down view; render it without the ceiling.
            var ceiling = GameObject.Find("Ceiling_HighRise");

            var go = new GameObject("_SnapshotCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            foreach (var (name, pos, lookAt) in VIEWS)
            {
                bool topDown = name.Contains("top_down");
                if (ceiling != null) ceiling.SetActive(!topDown);
                go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(lookAt - pos, topDown ? Vector3.forward : Vector3.up));
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            }

            RenderTexture.active = null;
            if (ceiling != null) ceiling.SetActive(true);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"[EscapeFire] Snapshots written to {dir}");
        }
    }
}
