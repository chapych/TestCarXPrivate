using System;
using UnityEngine;

// This class implements simple ghosting type Motion Blur.
// If Extra Blur is selected, the scene will allways be a little blurred,
// as it is scaled to a smaller resolution.
// The effect works by accumulating the previous frames in an accumulation
// texture.
namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Blur/Motion Blur (Color Accumulation)")]
    [RequireComponent(typeof(Camera))]
    public class MotionBlur : ImageEffectBase
    {
        public float BlurAmount = 0.8f;
        public bool ExtraBlur = false;

        private RenderTexture m_accumTexture;

        override protected void Start()
        {
            if (!SystemInfo.supportsRenderTextures)
            {
                enabled = false;
                return;
            }
            base.Start();
        }

        override protected void OnDisable()
        {
            base.OnDisable();
            DestroyImmediate(m_accumTexture);
        }

        // Called by camera to apply image effect
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            // Create the accumulation texture
            if (m_accumTexture == null || m_accumTexture.width != source.width || m_accumTexture.height != source.height)
            {
                DestroyImmediate(m_accumTexture);
                m_accumTexture = new RenderTexture(source.width, source.height, 0);
                m_accumTexture.hideFlags = HideFlags.HideAndDontSave;
                Graphics.Blit( source, m_accumTexture );
            }

            // If Extra Blur is selected, downscale the texture to 4x4 smaller resolution.
            if (ExtraBlur)
            {
                RenderTexture blurbuffer = RenderTexture.GetTemporary(source.width/4, source.height/4, 0);
                m_accumTexture.MarkRestoreExpected();
                Graphics.Blit(m_accumTexture, blurbuffer);
                Graphics.Blit(blurbuffer,m_accumTexture);
                RenderTexture.ReleaseTemporary(blurbuffer);
            }

            // Clamp the motion blur variable, so it can never leave permanent trails in the image
            BlurAmount = Mathf.Clamp( BlurAmount, 0.0f, 0.92f );

            // Setup the texture and floating point values in the shader
            material.SetTexture("_MainTex", m_accumTexture);
            material.SetFloat("_AccumOrig", 1.0F-BlurAmount);

            // We are accumulating motion over frames without clear/discard
            // by design, so silence any performance warnings from Unity
            m_accumTexture.MarkRestoreExpected();

            // Render the image using the motion blur shader
            Graphics.Blit (source, m_accumTexture, material);
            Graphics.Blit (m_accumTexture, destination);
        }
    }
}
