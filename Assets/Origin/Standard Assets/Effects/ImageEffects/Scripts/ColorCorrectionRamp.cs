using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Color Adjustments/Color Correction (Ramp)")]
    public class ColorCorrectionRamp : ImageEffectBase {
        public Texture  TextureRamp;

        // Called by camera to apply image effect
        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            material.SetTexture ("_RampTex", TextureRamp);
            Graphics.Blit (source, destination, material);
        }
    }
}
