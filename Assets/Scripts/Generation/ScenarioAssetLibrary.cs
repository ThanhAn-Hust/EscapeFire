using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Prefabs behind the catalog categories (plan V4.2): which PolygonOffice models a generated room may use for a
    /// "bookshelf", "server_rack"…, plus the working extinguisher / fire / character prefabs. Built by
    /// EscapeFire > V4 > Build Generated Scene; lives in Resources so the headset can build rooms at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "EscapeFire/Scenario Asset Library")]
    public class ScenarioAssetLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string category;
            public GameObject[] prefabs = new GameObject[0];
            [Tooltip("Optional things put on top (computer on a computer desk).")]
            public GameObject[] toppers = new GameObject[0];
        }

        public List<Entry> categories = new List<Entry>();
        public GameObject co2Extinguisher;
        public GameObject waterExtinguisher;
        public GameObject fire;
        public GameObject[] people = new GameObject[0];
        public TMP_FontAsset font;

        public Entry Find(string category) => categories.Find(e => e.category == category);

        public int PrefabCount
        {
            get
            {
                int n = 0;
                foreach (var e in categories) n += e.prefabs.Length;
                return n;
            }
        }
    }
}
