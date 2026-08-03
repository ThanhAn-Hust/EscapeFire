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

        public string SaveTelemetryReport(string jsonReport, string studentId)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, outputDirectoryName);
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
