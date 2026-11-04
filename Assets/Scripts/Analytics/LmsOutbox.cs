using System;
using System.Collections.Generic;
using System.IO;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Durable offline queue for LMS reports: one JSON file per session in a folder.
    /// Reports survive app restarts and are retried until the LMS accepts them (spec: offline queue).
    /// </summary>
    public sealed class LmsOutbox
    {
        private readonly string _directory;

        public LmsOutbox(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Outbox directory required", nameof(directory));
            _directory = directory;
        }

        public int PendingCount => Directory.Exists(_directory) ? Directory.GetFiles(_directory, "*.json").Length : 0;

        public string Enqueue(string sessionId, string json)
        {
            Directory.CreateDirectory(_directory);
            string safeId = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("N") : Sanitize(sessionId);
            string path = Path.Combine(_directory, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{safeId}.json");
            File.WriteAllText(path, json);
            return path;
        }

        /// <summary>Oldest first. Each item: (sessionId, json, filePath).</summary>
        public List<(string sessionId, string json, string path)> Pending()
        {
            var result = new List<(string, string, string)>();
            if (!Directory.Exists(_directory)) return result;
            var files = Directory.GetFiles(_directory, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                int underscore = name.IndexOf('_');
                string id = underscore >= 0 ? name.Substring(underscore + 1) : name;
                result.Add((id, File.ReadAllText(file), file));
            }
            return result;
        }

        public void MarkDelivered(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static string Sanitize(string id)
        {
            var chars = id.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-') chars[i] = '-';
            }
            return new string(chars);
        }
    }
}
