using System.Collections.Generic;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// AAR holographic mini-map: replays the learner's path from the drill, scaled onto a tabletop.
    /// Green = safe (crouched / out of smoke), red = standing upright inside the smoke layer.
    /// One LineRenderer per contiguous run of the same state (built once in Start, no per-frame cost).
    /// </summary>
    public class TrajectoryView : MonoBehaviour
    {
        [SerializeField] private float mapSize = 1.2f;   // metres on the table
        [SerializeField] private float lineWidth = 0.008f;
        [SerializeField] private Color safeColor = new Color(0.2f, 0.9f, 0.4f);
        [SerializeField] private Color smokeColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private Material lineMaterial;

        private void Start()
        {
            Build(SessionResult.TrajectoryPositions, SessionResult.TrajectoryInSmoke);
        }

        public void Build(Vector3[] positions, bool[] inSmoke)
        {
            if (positions == null || positions.Length < 2) return;

            Vector3 min = positions[0], max = positions[0];
            for (int i = 1; i < positions.Length; i++)
            {
                min = Vector3.Min(min, positions[i]);
                max = Vector3.Max(max, positions[i]);
            }
            float extent = Mathf.Max(max.x - min.x, max.z - min.z, 0.01f);
            float scale = mapSize / extent;
            Vector3 centre = (min + max) * 0.5f;

            var run = new List<Vector3>();
            bool runSmoke = inSmoke[0];
            for (int i = 0; i < positions.Length; i++)
            {
                var p = positions[i];
                var local = new Vector3((p.x - centre.x) * scale, 0.002f, (p.z - centre.z) * scale);
                if (inSmoke[i] != runSmoke && run.Count > 0)
                {
                    run.Add(local); // connect segments
                    CreateSegment(run, runSmoke);
                    run.Clear();
                    runSmoke = inSmoke[i];
                }
                run.Add(local);
            }
            CreateSegment(run, runSmoke);

            CreateMarker(positions[0], centre, scale, Color.white, "Start");
            CreateMarker(positions[positions.Length - 1], centre, scale, SessionResult.Passed ? safeColor : smokeColor, "End");
        }

        private void CreateSegment(List<Vector3> points, bool smoke)
        {
            if (points.Count < 2) return;
            var go = new GameObject(smoke ? "Path_SmokeStanding" : "Path_Safe");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
            lr.widthMultiplier = smoke ? lineWidth * 1.6f : lineWidth;
            lr.startColor = lr.endColor = smoke ? smokeColor : safeColor;
            if (lineMaterial != null) lr.sharedMaterial = lineMaterial;
            else lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void CreateMarker(Vector3 world, Vector3 centre, float scale, Color color, string name)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            Destroy(marker.GetComponent<Collider>());
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3((world.x - centre.x) * scale, 0.01f, (world.z - centre.z) * scale);
            marker.transform.localScale = Vector3.one * 0.03f;
            var r = marker.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default")) { color = color };
        }
    }
}
