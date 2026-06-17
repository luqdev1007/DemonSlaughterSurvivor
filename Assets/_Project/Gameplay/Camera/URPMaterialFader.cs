using UnityEngine;
using UnityEngine.Rendering;

namespace DemonSlaughter.Gameplay.Camera
{
    public static class URPMaterialFader
    {
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int Surface = Shader.PropertyToID("_Surface");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static void SetTransparent(Material material, float alpha)
        {
            material.SetFloat(Surface, 1f); 
            material.SetFloat(SrcBlend, (float)BlendMode.SrcAlpha);
            material.SetFloat(DstBlend, (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat(ZWrite, 0f);
            material.renderQueue = (int)RenderQueue.Transparent;

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            var color = material.GetColor(BaseColor);
            color.a = alpha;
            material.SetColor(BaseColor, color);
        }

        public static void SetOpaque(Material material, Color originalColor)
        {
            material.SetFloat(Surface, 0f); 
            material.SetFloat(SrcBlend, (float)BlendMode.One);
            material.SetFloat(DstBlend, (float)BlendMode.Zero);
            material.SetFloat(ZWrite, 1f);
            material.renderQueue = (int)RenderQueue.Geometry;

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            material.SetColor(BaseColor, originalColor);
        }
    }
}