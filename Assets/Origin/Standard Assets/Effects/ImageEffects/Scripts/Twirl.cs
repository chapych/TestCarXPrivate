using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Displacement/Twirl")]
    public class Twirl : ImageEffectBase
    {
        public Vector2 Radius = new Vector2(0.3F,0.3F);
        public float Angle = 50;
        public Vector2 Center = new Vector2 (0.5F, 0.5F);


        // Called by camera to apply image effect
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            ImageEffects.RenderDistortion (material, source, destination, Angle, Center, Radius);
        }
    }
}
