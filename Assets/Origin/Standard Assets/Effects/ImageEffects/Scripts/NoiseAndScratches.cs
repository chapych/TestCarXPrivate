using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu("Image Effects/Noise/Noise and Scratches")]
    public class NoiseAndScratches : MonoBehaviour
    {
        /// Monochrome noise just adds grain. Non-monochrome noise
        /// more resembles VCR as it adds noise in YUV color space,
        /// thus introducing magenta/green colors.
        public bool Monochrome = true;
        private bool m_rgbFallback = false;

        // Noise grain takes random intensity from Min to Max.
        public float GrainIntensityMin = 0.1f;
        public float GrainIntensityMax = 0.2f;

        /// The size of the noise grains (1 = one pixel).
        public float GrainSize = 2.0f;

        // Scratches take random intensity from Min to Max.
        public float ScratchIntensityMin = 0.05f;
        public float ScratchIntensityMax = 0.25f;

        /// Scratches jump to another locations at this times per second.
        public float ScratchFPS = 10.0f;
        /// While scratches are in the same location, they jitter a bit.
        public float ScratchJitter = 0.01f;

        public Texture GrainTexture;
        public Texture ScratchTexture;
        public Shader   ShaderRGB;
        public Shader   ShaderYuv;
        private Material m_materialRGB;
        private Material m_materialYuv;

        private float m_scratchTimeLeft = 0.0f;
        private float m_scratchX, m_scratchY;

        protected void Start ()
        {
            // Disable if we don't support image effects
            if (!SystemInfo.supportsImageEffects) {
                enabled = false;
                return;
            }

            if ( ShaderRGB == null || ShaderYuv == null )
            {
                Debug.Log( "Noise shaders are not set up! Disabling noise effect." );
                enabled = false;
            }
            else
            {
                if ( !ShaderRGB.isSupported ) // disable effect if RGB shader is not supported
                    enabled = false;
                else if ( !ShaderYuv.isSupported ) // fallback to RGB if YUV is not supported
                    m_rgbFallback = true;
            }
        }

        protected Material material {
            get {
                if ( m_materialRGB == null ) {
                    m_materialRGB = new Material( ShaderRGB );
                    m_materialRGB.hideFlags = HideFlags.HideAndDontSave;
                }
                if ( m_materialYuv == null && !m_rgbFallback ) {
                    m_materialYuv = new Material( ShaderYuv );
                    m_materialYuv.hideFlags = HideFlags.HideAndDontSave;
                }
                return (!m_rgbFallback && !Monochrome) ? m_materialYuv : m_materialRGB;
            }
        }

        protected void OnDisable() {
            if ( m_materialRGB )
                DestroyImmediate( m_materialRGB );
            if ( m_materialYuv )
                DestroyImmediate( m_materialYuv );
        }

        private void SanitizeParameters()
        {
            GrainIntensityMin = Mathf.Clamp( GrainIntensityMin, 0.0f, 5.0f );
            GrainIntensityMax = Mathf.Clamp( GrainIntensityMax, 0.0f, 5.0f );
            ScratchIntensityMin = Mathf.Clamp( ScratchIntensityMin, 0.0f, 5.0f );
            ScratchIntensityMax = Mathf.Clamp( ScratchIntensityMax, 0.0f, 5.0f );
            ScratchFPS = Mathf.Clamp( ScratchFPS, 1, 30 );
            ScratchJitter = Mathf.Clamp( ScratchJitter, 0.0f, 1.0f );
            GrainSize = Mathf.Clamp( GrainSize, 0.1f, 50.0f );
        }

        // Called by the camera to apply the image effect
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            SanitizeParameters();

            if ( m_scratchTimeLeft <= 0.0f )
            {
                m_scratchTimeLeft = Random.value * 2 / ScratchFPS; // we have sanitized it earlier, won't be zero
                m_scratchX = Random.value;
                m_scratchY = Random.value;
            }
            m_scratchTimeLeft -= Time.deltaTime;

            Material mat = material;

            mat.SetTexture("_GrainTex", GrainTexture);
            mat.SetTexture("_ScratchTex", ScratchTexture);
            float grainScale = 1.0f / GrainSize; // we have sanitized it earlier, won't be zero
            mat.SetVector("_GrainOffsetScale", new Vector4(
                                                   Random.value,
                                                   Random.value,
                                                   (float)Screen.width / (float)GrainTexture.width * grainScale,
                                                   (float)Screen.height / (float)GrainTexture.height * grainScale
                                                   ));
            mat.SetVector("_ScratchOffsetScale", new Vector4(
                                                     m_scratchX + Random.value*ScratchJitter,
                                                     m_scratchY + Random.value*ScratchJitter,
                                                     (float)Screen.width / (float) ScratchTexture.width,
                                                     (float)Screen.height / (float) ScratchTexture.height
                                                     ));
            mat.SetVector("_Intensity", new Vector4(
                                            Random.Range(GrainIntensityMin, GrainIntensityMax),
                                            Random.Range(ScratchIntensityMin, ScratchIntensityMax),
                                            0, 0 ));
            Graphics.Blit (source, destination, mat);
        }
    }
}
