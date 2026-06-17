using UnityEngine;

namespace DemonSlaughter.Gameplay.Camera
{
    public sealed class RendererData
    {
        public readonly Material[] OriginalMaterials;
        public readonly Color[] OriginalColors;
        public readonly Material[] FadedMaterials;

        public RendererData(Renderer renderer)
        {
            var sharedMats = renderer.sharedMaterials;
            OriginalMaterials = new Material[sharedMats.Length];
            OriginalColors = new Color[sharedMats.Length];
            FadedMaterials = new Material[sharedMats.Length];

            for (int i = 0; i < sharedMats.Length; i++)
            {
                // Создаём instance материала чтобы не портить shared
                OriginalMaterials[i] = sharedMats[i];
                OriginalColors[i] = sharedMats[i].GetColor("_BaseColor");
                FadedMaterials[i] = new Material(sharedMats[i]);
            }
        }
    }
}