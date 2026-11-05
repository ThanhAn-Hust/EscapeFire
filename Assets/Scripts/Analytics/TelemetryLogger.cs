using System.IO;
using UnityEngine;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Saves student performance JSON report to local disk for Web Dashboard ingestion.
    /// </summary>
    public class TelemetryLogger : MonoBehaviour
    {
        [SerializeField] private string outputDirectoryName = "TelemetryReports";

        /// <summary>Tests redirect reports here so they never mix with real learner data.</summary>
        public static string FolderOverride { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOverride() => FolderOverride = null;

        public string SaveTelemetryReport(string jsonReport, string studentId)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, FolderOverride ?? outputDirectoryName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = $"Report_{studentId}_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(folderPath, fileName);

            File.WriteAllText(filePath, jsonReport);
            Debug.Log($"[TelemetryLogger] Report saved successfully to: {filePath}");

            return filePath;
        }
    }
}
