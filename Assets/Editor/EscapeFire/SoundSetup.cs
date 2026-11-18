using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Scenario;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V6 — the drill had no sound: the components had audio slots but nothing was wired. Wires them in the lab scene:
    /// the server explosion and the fire's roar (Real Fire &amp; Smoke clips), the fire bell rung by the call point (it
    /// is the learner's alarm that rings it), the extinguisher jets and the breaker clack (synthesised by
    /// Tools/make_sounds.py). Called by RescueBuilder.Build.
    /// </summary>
    public static class SoundSetup
    {
        private const string FIRE = "Assets/Real Fire & Smoke/Sound/";
        private const string OWN = "Assets/Art/Audio/";

        public static void Apply(List<string> log)
        {
            int n = 0;
            var fireLoop = Clip(FIRE + "fire_big_loop.wav");
            foreach (var f in Object.FindObjectsByType<FireHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var src = Source(f.transform, "FireAudio", fireLoop, true, 1f, 1.5f, 20f, 0.8f);
                Assign(f, "fireAudioSource", src);
                n++;
            }

            var c = Object.FindFirstObjectByType<LabProgressiveScenarioController>(FindObjectsInactive.Include);
            if (c != null)
            {
                // this source is played when the fire breaks out: the server cabinet bursting into flames
                Transform at = c.ServerFire != null ? c.ServerFire.transform : c.transform;
                if (c.ServerFire == null) // resolved at runtime; in the editor find it by name
                    foreach (var f in Object.FindObjectsByType<FireHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (f.name.ToLowerInvariant().Contains("server")) at = f.transform;
                var boom = Source(at, "ExplosionAudio", Clip(FIRE + "fire_explosion_small.wav"), false, 1f, 3f, 30f, 1f);
                Assign(c, "alarmAudioSource", boom);
                n++;
            }

            var bell = Clip(OWN + "EF_FireAlarmBell.wav");
            foreach (var a in Object.FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // heard on the whole floor (mostly 2D), a little louder near the bell
                var src = Source(a.transform, "FireAlarmBell", bell, true, 0.35f, 4f, 40f, 0.45f);
                Assign(a, "alarmAudio", src);
                n++;
            }

            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var clip = Clip(OWN + (e.Type == ExtinguisherType.CO2 ? "EF_CO2_Spray.wav" : "EF_Water_Spray.wav"));
                Transform horn = e.transform.Find("Horn") ?? e.transform;
                var src = Source(horn, "SprayAudio", clip, true, 1f, 0.8f, 15f, 0.9f);
                Assign(e, "sprayAudioSource", src);
                n++;
            }

            foreach (var b in Object.FindObjectsByType<CircuitBreakerSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var src = Source(b.transform, "BreakerAudio", null, false, 1f, 0.8f, 12f, 1f);
                Assign(b, "audioSource", src);
                var so = new SerializedObject(b);
                var p = so.FindProperty("switchSound");
                if (p != null) { p.objectReferenceValue = Clip(OWN + "EF_Breaker_Clack.wav"); so.ApplyModifiedPropertiesWithoutUndo(); }
                n++;
            }
            log.Add($"sounds wired: {n} sources (fire, explosion, bell, extinguishers, breaker)");
        }

        private static AudioClip Clip(string path)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (c == null) Debug.LogWarning("[Sound] missing " + path);
            return c;
        }

        private static AudioSource Source(Transform parent, string name, AudioClip clip, bool loop, float spatial, float min, float max, float volume)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = spatial;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = min;
            s.maxDistance = max;
            s.volume = volume;
            s.dopplerLevel = 0f;
            return s;
        }

        private static void Assign(Object target, string field, AudioSource src)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Sound] no field {field} on {target.name}"); return; }
            p.objectReferenceValue = src;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
