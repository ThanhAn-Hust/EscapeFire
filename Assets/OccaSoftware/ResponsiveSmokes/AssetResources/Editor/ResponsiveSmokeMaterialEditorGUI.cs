using UnityEditor;

namespace OccaSoftware.ResponsiveSmokes.Editor
{
    public class ResponsiveSmokeMaterialEditorGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor e, MaterialProperty[] properties)
        {
            EditorGUILayout.LabelField("Configure the Interactive Smoke properties from the Interactive Smoke component.");
        }
    }
}
