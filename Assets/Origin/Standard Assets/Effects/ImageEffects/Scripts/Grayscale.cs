using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Color Adjustments/Grayscale")]
    public class Grayscale : ImageEffectBase {
        public Texture  TextureRamp;
        public float    RampOffset;

        // Called by camera to apply image effect
        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            material.SetTexture("_RampTex", TextureRamp);
            material.SetFloat("_RampOffset", RampOffset);
            Graphics.Blit (source, destination, material);
        }
    }
}
