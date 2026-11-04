using NUnit.Framework;
using UnityEditor.SceneManagement;
using EscapeFire.Core;
using EscapeFire.EditorTools;

namespace EscapeFire.Tests
{
    /// <summary>Static quality gate: the lab scene must pass every LabSceneAudit error check.</summary>
    public class LabSceneValidationTests
    {
        [Test]
        public void LabScene_HasNoAuditErrors()
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{SceneNames.LAB}.unity", OpenSceneMode.Single);
            LabSceneAudit.Collect(out var errors, out var warnings);
            if (warnings.Count > 0) UnityEngine.Debug.LogWarning("Lab audit warnings:\n" + string.Join("\n", warnings));
            Assert.IsEmpty(errors, "Lab scene audit errors:\n" + string.Join("\n", errors));
        }
    }
}
