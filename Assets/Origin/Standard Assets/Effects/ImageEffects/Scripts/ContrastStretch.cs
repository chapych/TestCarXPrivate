using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Color Adjustments/Contrast Stretch")]
    public class ContrastStretch : MonoBehaviour
    {
        /// Adaptation speed - percents per frame, if playing at 30FPS.
        /// Default is 0.02 (2% each 1/30s).
        public float AdaptationSpeed = 0.02f;

        /// If our scene is really dark (or really bright), we might not want to
        /// stretch its contrast to the full range.
        /// limitMinimum=0, limitMaximum=1 is the same as not applying the effect at all.
        /// limitMinimum=1, limitMaximum=0 is always stretching colors to full range.

        /// The limit on the minimum luminance (0...1) - we won't go above this.
        public float LimitMinimum = 0.2f;

        /// The limit on the maximum luminance (0...1) - we won't go below this.
        public float LimitMaximum = 0.6f;


        // To maintain adaptation levels over time, we need two 1x1 render textures
        // and ping-pong between them.
        private RenderTexture[] m_adaptRenderTex = new RenderTexture[2];
        private int m_curAdaptIndex = 0;


        // Computes scene luminance (grayscale) image
        public Shader   ShaderLum;
        private Material m_materialLum;
        protected Material materialLum {
            get {
                if ( m_materialLum == null ) {
                    m_materialLum = new Material(ShaderLum);
                    m_materialLum.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_materialLum;
            }
        }

        // Reduces size of the image by 2x2, while computing maximum/minimum values.
        // By repeatedly applying this shader, we reduce the initial luminance image
        // to 1x1 image with minimum/maximum luminances found.
        public Shader   ShaderReduce;
        private Material m_materialReduce;
        protected Material materialReduce {
            get {
                if ( m_materialReduce == null ) {
                    m_materialReduce = new Material(ShaderReduce);
                    m_materialReduce.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_materialReduce;
            }
        }

        // Adaptation shader - gradually "adapts" minimum/maximum luminances,
        // based on currently adapted 1x1 image and the actual 1x1 image of the current scene.
        public Shader   ShaderAdapt;
        private Material m_materialAdapt;
        protected Material materialAdapt {
            get {
                if ( m_materialAdapt == null ) {
                    m_materialAdapt = new Material(ShaderAdapt);
                    m_materialAdapt.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_materialAdapt;
            }
        }

        // Final pass - stretches the color values of the original scene, based on currently
        // adpated minimum/maximum values.
        public Shader   ShaderApply;
        private Material m_materialApply;
        protected Material materialApply {
            get {
                if ( m_materialApply == null ) {
                    m_materialApply = new Material(ShaderApply);
                    m_materialApply.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_materialApply;
            }
        }

        void Start()
        {
            // Disable if we don't support image effects
            if (!SystemInfo.supportsImageEffects) {
                enabled = false;
                return;
            }

            if (!ShaderAdapt.isSupported || !ShaderApply.isSupported || !ShaderLum.isSupported || !ShaderReduce.isSupported) {
                enabled = false;
                return;
            }
        }

        void OnEnable()
        {
            for( int i = 0; i < 2; ++i )
            {
                if ( !m_adaptRenderTex[i] ) {
                    m_adaptRenderTex[i] = new RenderTexture(1, 1, 0);
                    m_adaptRenderTex[i].hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }

        void OnDisable()
        {
            for( int i = 0; i < 2; ++i )
            {
                DestroyImmediate( m_adaptRenderTex[i] );
                m_adaptRenderTex[i] = null;
            }
            if ( m_materialLum )
                DestroyImmediate( m_materialLum );
            if ( m_materialReduce )
                DestroyImmediate( m_materialReduce );
            if ( m_materialAdapt )
                DestroyImmediate( m_materialAdapt );
            if ( m_materialApply )
                DestroyImmediate( m_materialApply );
        }


        /// Apply the filter
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            // Blit to smaller RT and convert to luminance on the way
            const int tempRatio = 1; // 4x4 smaller
            RenderTexture rtTempSrc = RenderTexture.GetTemporary(source.width/tempRatio, source.height/tempRatio);
            Graphics.Blit (source, rtTempSrc, materialLum);

            // Repeatedly reduce this image in size, computing min/max luminance values
            // In the end we'll have 1x1 image with min/max luminances found.
            const int finalSize = 1;
            //const int FINAL_SIZE = 1;
            while( rtTempSrc.width > finalSize || rtTempSrc.height > finalSize )
            {
                const int reduceRatio = 2; // our shader does 2x2 reduction
                int destW = rtTempSrc.width / reduceRatio;
                if ( destW < finalSize ) destW = finalSize;
                int destH = rtTempSrc.height / reduceRatio;
                if ( destH < finalSize ) destH = finalSize;
                RenderTexture rtTempDst = RenderTexture.GetTemporary(destW,destH);
                Graphics.Blit (rtTempSrc, rtTempDst, materialReduce);

                // Release old src temporary, and make new temporary the source
                RenderTexture.ReleaseTemporary( rtTempSrc );
                rtTempSrc = rtTempDst;
            }

            // Update viewer's adaptation level
            CalculateAdaptation( rtTempSrc );

            // Apply contrast strech to the original scene, using currently adapted parameters
            materialApply.SetTexture("_AdaptTex", m_adaptRenderTex[m_curAdaptIndex] );
            Graphics.Blit (source, destination, materialApply);

            RenderTexture.ReleaseTemporary( rtTempSrc );
        }


        /// Helper function to do gradual adaptation to min/max luminances
        private void CalculateAdaptation( Texture curTexture )
        {
            int prevAdaptIndex = m_curAdaptIndex;
            m_curAdaptIndex = (m_curAdaptIndex+1) % 2;

            // Adaptation speed is expressed in percents/frame, based on 30FPS.
            // Calculate the adaptation lerp, based on current FPS.
            float adaptLerp = 1.0f - Mathf.Pow( 1.0f - AdaptationSpeed, 30.0f * Time.deltaTime );
            const float kMinAdaptLerp = 0.01f;
            adaptLerp = Mathf.Clamp( adaptLerp, kMinAdaptLerp, 1 );

            materialAdapt.SetTexture("_CurTex", curTexture );
            materialAdapt.SetVector("_AdaptParams", new Vector4(
                                                        adaptLerp,
                                                        LimitMinimum,
                                                        LimitMaximum,
                                                        0.0f
                                                        ));
            // clear destination RT so its contents don't need to be restored
            Graphics.SetRenderTarget(m_adaptRenderTex[m_curAdaptIndex]);
            GL.Clear(false, true, Color.black);
            Graphics.Blit (
                m_adaptRenderTex[prevAdaptIndex],
                m_adaptRenderTex[m_curAdaptIndex],
                materialAdapt);
        }
    }
}
