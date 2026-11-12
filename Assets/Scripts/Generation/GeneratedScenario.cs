using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Hand-off between the menu / edge server and the generated-room scene: the spec to build next, and a small client
    /// for POST /scenario/generate (teacher prompt -> validated ScenarioSpec, see Server/escapefire_edge/scenario.py).
    /// </summary>
    public static class GeneratedScenario
    {
        /// <summary>Spec to build when 03_Generated loads (null = the scene's default sample).</summary>
        public static JObject Pending;
        public static int Seed = 1;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Pending = null;
            Seed = 1;
        }

        /// <summary>Asks the edge server for a room from a teacher prompt. onDone(spec, source, error).</summary>
        public static IEnumerator Request(string httpBase, string prompt, int level, Action<JObject, string, string> onDone)
        {
            var body = new JObject { ["prompt"] = prompt, ["level"] = level }.ToString(Newtonsoft.Json.Formatting.None);
            using (var req = new UnityWebRequest(httpBase.TrimEnd('/') + "/scenario/generate", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 60;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    onDone(null, "", req.error);
                    yield break;
                }
                JObject reply;
                try { reply = JObject.Parse(req.downloadHandler.text); }
                catch (Newtonsoft.Json.JsonException e) { onDone(null, "", e.Message); yield break; }
                onDone(reply["spec"] as JObject, (string)reply["source"] ?? "", null);
            }
        }
    }
}
