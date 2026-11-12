using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using EscapeFire.Generation;

namespace EscapeFire.Tests
{
    /// <summary>V4: generated rooms get a real floor plan where the fire, extinguishers, NPCs and start reach an exit.</summary>
    public class V4LayoutTests
    {
        private static string RepoFile(string relative)
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), System.AppContext.BaseDirectory })
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    string p = Path.Combine(dir.FullName, relative);
                    if (File.Exists(p)) return p;
                }
            Assert.Fail("not found: " + relative);
            return null;
        }

        private static ScenarioSpec Sample(string room)
        {
            V4Catalog.Load(File.ReadAllText(RepoFile("Assets/Resources/V4Catalog.json")));
            var spec = ScenarioSpec.Parse(JObject.Parse(File.ReadAllText(RepoFile($"Assets/Resources/Scenarios/{room}.json"))), out var errors);
            Assert.IsNotNull(spec, string.Join(",", errors));
            return spec;
        }

        [Test]
        public void AllFiveSampleRooms_GetAPlayableLayout_ForSeveralSeeds()
        {
            var failures = new List<string>();
            foreach (var room in new[] { "lab", "library", "office", "canteen", "workshop" })
                for (int seed = 1; seed <= 5; seed++)
                {
                    var L = new LayoutSolver(Sample(room), seed).Solve();
                    if (!L.Ok) failures.Add($"{room}/{seed}: {L.Reason}");
                    else if (L.Items.Count < 3) failures.Add($"{room}/{seed}: only {L.Items.Count} items");
                    else if (!L.Items.Exists(i => i.IsFire)) failures.Add($"{room}/{seed}: no fire");
                }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void SameSeed_SameRoom_DifferentSeed_DifferentRoom()
        {
            var a = new LayoutSolver(Sample("library"), 7).Solve();
            var b = new LayoutSolver(Sample("library"), 7).Solve();
            var c = new LayoutSolver(Sample("library"), 8).Solve();
            Assert.AreEqual(a.Items[1].X, b.Items[1].X);
            bool differs = false;
            for (int i = 0; i < System.Math.Min(a.Items.Count, c.Items.Count); i++)
                if (a.Items[i].X != c.Items[i].X || a.Items[i].Z != c.Items[i].Z) differs = true;
            Assert.IsTrue(differs, "another seed gives another arrangement");
        }

        [Test]
        public void NothingStandsInFrontOfAnExit()
        {
            var L = new LayoutSolver(Sample("canteen"), 3).Solve();
            Assert.IsTrue(L.Ok, L.Reason);
            foreach (var e in L.Exits)
                foreach (var it in L.Items)
                {
                    bool ns = e.Kind == "north" || e.Kind == "south";
                    float along = ns ? System.Math.Abs(it.X - e.X) : System.Math.Abs(it.Z - e.Z);
                    float depth = ns ? System.Math.Abs(it.Z - e.Z) : System.Math.Abs(it.X - e.X);
                    Assert.IsFalse(along < 0.5f && depth < 1.2f, $"{it.Category} blocks the {e.Kind} exit");
                }
        }

        [Test]
        public void ExtinguisherHangsBesideTheFirstExit_AndUnconsciousVictimHasRoom()
        {
            var L = new LayoutSolver(Sample("lab"), 2).Solve();
            Assert.IsTrue(L.Ok, L.Reason);
            var ex = L.Extinguishers[0];
            var door = L.Exits[0];
            Assert.Less(System.Math.Abs(ex.X - door.X) + System.Math.Abs(ex.Z - door.Z), 1.6f, "within reach of the door");
            Assert.IsTrue(L.Npcs.Exists(n => n.Kind == "unconscious"));
        }
    }
}
