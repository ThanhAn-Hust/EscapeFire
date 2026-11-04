using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Sends completed-session reports to the LMS (daotao.ai contract: POST {base}/api/v1/sessions/{id}/complete).
    /// Every report is first written to a durable outbox in persistentDataPath, then delivered; failures stay queued
    /// and are retried on the next submit / app start. Runs on a persistent object so scene loads do not cancel it.
    /// The edge server (Server/) implements the same endpoint as a local mock LMS for demos.
    /// </summary>
    public class LmsSync : MonoBehaviour
    {
        private static LmsSync _runner;
        private static bool _flushing;

        /// <summary>Base URL, e.g. http://192.168.1.100:8765 . Empty = offline only (reports stay in the outbox).</summary>
        public static string BaseUrl { get; set; } = string.Empty;
        public static string BearerToken { get; set; } = string.Empty;
        public static int TimeoutSeconds { get; set; } = 8;

        private static LmsOutbox Outbox => new LmsOutbox(Path.Combine(Application.persistentDataPath, "lms_outbox"));

        public static int PendingCount => Outbox.PendingCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _runner = null;
            _flushing = false;
        }

        public static void Submit(string sessionId, string reportJson)
        {
            string path = Outbox.Enqueue(sessionId, reportJson);
            Debug.Log($"[LmsSync] Report queued: {path}");
            Flush();
        }

        public static void Flush()
        {
            if (_flushing || string.IsNullOrEmpty(BaseUrl)) return;
            if (_runner == null)
            {
                var go = new GameObject("[LmsSync]");
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<LmsSync>();
            }
            _runner.StartCoroutine(_runner.FlushRoutine());
        }

        private IEnumerator FlushRoutine()
        {
            _flushing = true;
            var outbox = Outbox;
            foreach (var item in outbox.Pending())
            {
                string url = $"{BaseUrl.TrimEnd('/')}/api/v1/sessions/{UnityWebRequest.EscapeURL(item.sessionId)}/complete";
                using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(item.json));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrEmpty(BearerToken)) request.SetRequestHeader("Authorization", "Bearer " + BearerToken);
                    request.timeout = TimeoutSeconds;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        outbox.MarkDelivered(item.path);
                        Debug.Log($"[LmsSync] Delivered session {item.sessionId}.");
                    }
                    else
                    {
                        Debug.LogWarning($"[LmsSync] LMS unreachable ({request.error}); {outbox.PendingCount} report(s) kept offline.");
                        break; // keep order; retry later
                    }
                }
            }
            _flushing = false;
        }
    }
}
