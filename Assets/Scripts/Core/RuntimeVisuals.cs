using UnityEngine;
using UnityEngine.Rendering;

namespace EscapeFire.Core
{
    /// <summary>
    /// Small runtime-built visuals (soft particle texture, transparent particle material) so effects such as the
    /// CO2 jet or the smoke plume work without extra assets and look the same on Quest and in the editor.
    /// </summary>
    public static class RuntimeVisuals
    {
        private static Texture2D _softDot;

        /// <summary>64x64 white disc with a smooth falloff (alpha), shared by all runtime particle effects.</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (_softDot != null) return _softDot;
                const int n = 64;
                _softDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "EF_SoftDot" };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * (3f - 2f * a) * 255f));
                }
                _softDot.SetPixels32(px);
                _softDot.Apply(false, true);
                return _softDot;
            }
        }

        /// <summary>Alpha-blended (or additive) URP particle material tinted with <paramref name="tint"/>.</summary>
        public static Material ParticleMaterial(Color tint, bool additive = false, Texture texture = null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "EF_RuntimeParticles" };
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture != null ? texture : SoftDot);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            MakeTransparent(mat, additive);
            return mat;
        }

        /// <summary>Switches a URP Unlit / Particles material to a transparent surface at runtime.</summary>
        public static void MakeTransparent(Material mat, bool additive = false)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
