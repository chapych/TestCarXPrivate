using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Camera/Depth of Field (deprecated)") ]
    public class DepthOfFieldDeprecated : PostEffectsBase
    {
        public enum Dof34QualitySetting
        {
            OnlyBackground = 1,
            BackgroundAndForeground = 2,
        }

        public enum DofResolution
        {
            High = 2,
            Medium = 3,
            Low = 4,
        }

        public enum DofBlurriness
        {
            Low = 1,
            High = 2,
            VeryHigh = 4,
        }

        public enum BokehDestination
        {
            Background = 0x1,
            Foreground = 0x2,
            BackgroundAndForeground = 0x3,
        }

        static private int smoothDownsamplePass = 6;
        static private float bokehExtraBlur = 2.0f;

        public Dof34QualitySetting Quality = Dof34QualitySetting.OnlyBackground;
        public DofResolution Resolution  = DofResolution.Low;
        public bool  SimpleTweakMode = true;

        public float FocalPoint = 1.0f;
        public float Smoothness = 0.5f;

        public float FocalZDistance = 0.0f;
        public float FocalZStartCurve = 1.0f;
        public float FocalZEndCurve = 1.0f;

        private float m_focalStartCurve = 2.0f;
        private float m_focalEndCurve = 2.0f;
        private float m_focalDistance01 = 0.1f;

        public Transform ObjectFocus = null;
        public float FocalSize = 0.0f;

        public DofBlurriness Bluriness = DofBlurriness.High;
        public float MAXBlurSpread = 1.75f;

        public float ForegroundBlurExtrude = 1.15f;

        public Shader DofBlurShader;
        private Material m_dofBlurMaterial = null;

        public Shader DofShader;
        private Material m_dofMaterial = null;

        public bool  Visualize = false;
        public BokehDestination BokehDestination = BokehDestination.Background;

        private float m_widthOverHeight = 1.25f;
        private float m_oneOverBaseSize = 1.0f / 512.0f;

        public bool  Bokeh = false;
        public bool  BokehSupport = true;
        public Shader BokehShader;
        public Texture2D BokehTexture;
        public float BokehScale = 2.4f;
        public float BokehIntensity = 0.15f;
        public float BokehThresholdContrast = 0.1f;
        public float BokehThresholdLuminance = 0.55f;
        public int BokehDownsample = 1;
        private Material m_bokehMaterial;

        private Camera m_camera;

        void CreateMaterials () {
            m_dofBlurMaterial = CheckShaderAndCreateMaterial (DofBlurShader, m_dofBlurMaterial);
            m_dofMaterial = CheckShaderAndCreateMaterial (DofShader,m_dofMaterial);
            BokehSupport = BokehShader.isSupported;

            if (Bokeh && BokehSupport && BokehShader)
                m_bokehMaterial = CheckShaderAndCreateMaterial (BokehShader, m_bokehMaterial);
        }


        public override bool CheckResources () {
            CheckSupport (true);

            m_dofBlurMaterial = CheckShaderAndCreateMaterial (DofBlurShader, m_dofBlurMaterial);
            m_dofMaterial = CheckShaderAndCreateMaterial (DofShader,m_dofMaterial);
            BokehSupport = BokehShader.isSupported;

            if (Bokeh && BokehSupport && BokehShader)
                m_bokehMaterial = CheckShaderAndCreateMaterial (BokehShader, m_bokehMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnDisable () {
            Quads.Cleanup ();
        }

        void OnEnable () {
            m_camera = GetComponent<Camera>();
            m_camera.depthTextureMode |= DepthTextureMode.Depth;
        }

        float FocalDistance01 ( float worldDist) {
            return m_camera.WorldToViewportPoint((worldDist-m_camera.nearClipPlane) * m_camera.transform.forward + m_camera.transform.position).z / (m_camera.farClipPlane-m_camera.nearClipPlane);
        }

        int GetDividerBasedOnQuality () {
            int divider = 1;
            if (Resolution == DofResolution.Medium)
                divider = 2;
            else if (Resolution == DofResolution.Low)
                divider = 2;
            return divider;
        }

        int GetLowResolutionDividerBasedOnQuality ( int baseDivider) {
            int lowTexDivider = baseDivider;
            if (Resolution == DofResolution.High)
                lowTexDivider *= 2;
            if (Resolution == DofResolution.Low)
                lowTexDivider *= 2;
            return lowTexDivider;
        }

        private RenderTexture m_foregroundTexture = null;
        private RenderTexture m_mediumRezWorkTexture = null;
        private RenderTexture m_finalDefocus = null;
        private RenderTexture m_lowRezWorkTexture = null;
        private RenderTexture m_bokehSource = null;
        private RenderTexture m_bokehSource2 = null;

        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (CheckResources()==false) {
                Graphics.Blit (source, destination);
                return;
            }

            if (Smoothness < 0.1f)
                Smoothness = 0.1f;

            // update needed focal & rt size parameter

            Bokeh = Bokeh && BokehSupport;
            float bokehBlurAmplifier = Bokeh ? bokehExtraBlur : 1.0f;

            bool  blurForeground = Quality > Dof34QualitySetting.OnlyBackground;
            float focal01Size = FocalSize / (m_camera.farClipPlane - m_camera.nearClipPlane);;

            if (SimpleTweakMode) {
                m_focalDistance01 = ObjectFocus ? (m_camera.WorldToViewportPoint (ObjectFocus.position)).z / (m_camera.farClipPlane) : FocalDistance01 (FocalPoint);
                m_focalStartCurve = m_focalDistance01 * Smoothness;
                m_focalEndCurve = m_focalStartCurve;
                blurForeground = blurForeground && (FocalPoint > (m_camera.nearClipPlane + Mathf.Epsilon));
            }
            else {
                if (ObjectFocus) {
                    var vpPoint= m_camera.WorldToViewportPoint (ObjectFocus.position);
                    vpPoint.z = (vpPoint.z) / (m_camera.farClipPlane);
                    m_focalDistance01 = vpPoint.z;
                }
                else
                    m_focalDistance01 = FocalDistance01 (FocalZDistance);

                m_focalStartCurve = FocalZStartCurve;
                m_focalEndCurve = FocalZEndCurve;
                blurForeground = blurForeground && (FocalPoint > (m_camera.nearClipPlane + Mathf.Epsilon));
            }

            m_widthOverHeight = (1.0f * source.width) / (1.0f * source.height);
            m_oneOverBaseSize = 1.0f / 512.0f;

            m_dofMaterial.SetFloat ("_ForegroundBlurExtrude", ForegroundBlurExtrude);
            m_dofMaterial.SetVector ("_CurveParams", new Vector4 (SimpleTweakMode ? 1.0f / m_focalStartCurve : m_focalStartCurve, SimpleTweakMode ? 1.0f / m_focalEndCurve : m_focalEndCurve, focal01Size * 0.5f, m_focalDistance01));
            m_dofMaterial.SetVector ("_InvRenderTargetSize", new Vector4 (1.0f / (1.0f * source.width), 1.0f / (1.0f * source.height),0.0f,0.0f));

            int divider =  GetDividerBasedOnQuality ();
            int lowTexDivider = GetLowResolutionDividerBasedOnQuality (divider);

            AllocateTextures (blurForeground, source, divider, lowTexDivider);

            // WRITE COC to alpha channel
            // source is only being bound to detect y texcoord flip
            Graphics.Blit (source, source, m_dofMaterial, 3);

            // better DOWNSAMPLE (could actually be weighted for higher quality)
            Downsample (source, m_mediumRezWorkTexture);

            // BLUR A LITTLE first, which has two purposes
            // 1.) reduce jitter, noise, aliasing
            // 2.) produce the little-blur buffer used in composition later
            Blur (m_mediumRezWorkTexture, m_mediumRezWorkTexture, DofBlurriness.Low, 4, MAXBlurSpread);

            if ((Bokeh) && ((BokehDestination.Foreground & bokehDestination) != 0))
            {
                m_dofMaterial.SetVector ("_Threshhold", new Vector4(BokehThresholdContrast, BokehThresholdLuminance, 0.95f, 0.0f));

                // add and mark the parts that should end up as bokeh shapes
                Graphics.Blit (m_mediumRezWorkTexture, m_bokehSource2, m_dofMaterial, 11);

                // remove those parts (maybe even a little tittle bittle more) from the regurlarly blurred buffer
                //Graphics.Blit (mediumRezWorkTexture, lowRezWorkTexture, dofMaterial, 10);
                Graphics.Blit (m_mediumRezWorkTexture, m_lowRezWorkTexture);//, dofMaterial, 10);

                // maybe you want to reblur the small blur ... but not really needed.
                //Blur (mediumRezWorkTexture, mediumRezWorkTexture, DofBlurriness.Low, 4, maxBlurSpread);

                // bigger BLUR
                Blur (m_lowRezWorkTexture, m_lowRezWorkTexture, Bluriness, 0, MAXBlurSpread * bokehBlurAmplifier);
            }
            else  {
                // bigger BLUR
                Downsample (m_mediumRezWorkTexture, m_lowRezWorkTexture);
                Blur (m_lowRezWorkTexture, m_lowRezWorkTexture, Bluriness, 0, MAXBlurSpread);
            }

            m_dofBlurMaterial.SetTexture ("_TapLow", m_lowRezWorkTexture);
            m_dofBlurMaterial.SetTexture ("_TapMedium", m_mediumRezWorkTexture);
            Graphics.Blit (null, m_finalDefocus, m_dofBlurMaterial, 3);

            // we are only adding bokeh now if the background is the only part we have to deal with
            if ((Bokeh) && ((BokehDestination.Foreground & bokehDestination) != 0))
                AddBokeh (m_bokehSource2, m_bokehSource, m_finalDefocus);

            m_dofMaterial.SetTexture ("_TapLowBackground", m_finalDefocus);
            m_dofMaterial.SetTexture ("_TapMedium", m_mediumRezWorkTexture); // needed for debugging/visualization

            // FINAL DEFOCUS (background)
            Graphics.Blit (source, blurForeground ? m_foregroundTexture : destination, m_dofMaterial, Visualize ? 2 : 0);

            // FINAL DEFOCUS (foreground)
            if (blurForeground) {
                // WRITE COC to alpha channel
                Graphics.Blit (m_foregroundTexture, source, m_dofMaterial, 5);

                // DOWNSAMPLE (unweighted)
                Downsample (source, m_mediumRezWorkTexture);

                // BLUR A LITTLE first, which has two purposes
                // 1.) reduce jitter, noise, aliasing
                // 2.) produce the little-blur buffer used in composition later
                BlurFg (m_mediumRezWorkTexture, m_mediumRezWorkTexture, DofBlurriness.Low, 2, MAXBlurSpread);

                if ((Bokeh) && ((BokehDestination.Foreground & bokehDestination) != 0))
                {
                    m_dofMaterial.SetVector ("_Threshhold", new Vector4(BokehThresholdContrast * 0.5f, BokehThresholdLuminance, 0.0f, 0.0f));

                    // add and mark the parts that should end up as bokeh shapes
                    Graphics.Blit (m_mediumRezWorkTexture, m_bokehSource2, m_dofMaterial, 11);

                    // remove the parts (maybe even a little tittle bittle more) that will end up in bokeh space
                    //Graphics.Blit (mediumRezWorkTexture, lowRezWorkTexture, dofMaterial, 10);
                    Graphics.Blit (m_mediumRezWorkTexture, m_lowRezWorkTexture);//, dofMaterial, 10);

                    // big BLUR
                    BlurFg (m_lowRezWorkTexture, m_lowRezWorkTexture, Bluriness, 1, MAXBlurSpread * bokehBlurAmplifier);
                }
                else  {
                    // big BLUR
                    BlurFg (m_mediumRezWorkTexture, m_lowRezWorkTexture, Bluriness, 1, MAXBlurSpread);
                }

                // simple upsample once
                Graphics.Blit (m_lowRezWorkTexture, m_finalDefocus);

                m_dofMaterial.SetTexture ("_TapLowForeground", m_finalDefocus);
                Graphics.Blit (source, destination, m_dofMaterial, Visualize ? 1 : 4);

                if ((Bokeh) && ((BokehDestination.Foreground & bokehDestination) != 0))
                    AddBokeh (m_bokehSource2, m_bokehSource, destination);
            }

            ReleaseTextures ();
        }

        void Blur ( RenderTexture from, RenderTexture to, DofBlurriness iterations, int blurPass, float spread) {
            RenderTexture tmp = RenderTexture.GetTemporary (to.width, to.height);
            if ((int)iterations > 1) {
                BlurHex (from, to, blurPass, spread, tmp);
                if ((int)iterations > 2) {
                    m_dofBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, spread * m_oneOverBaseSize, 0.0f, 0.0f));
                    Graphics.Blit (to, tmp, m_dofBlurMaterial, blurPass);
                    m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  0.0f, 0.0f, 0.0f));
                    Graphics.Blit (tmp, to, m_dofBlurMaterial, blurPass);
                }
            }
            else {
                m_dofBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, spread * m_oneOverBaseSize, 0.0f, 0.0f));
                Graphics.Blit (from, tmp, m_dofBlurMaterial, blurPass);
                m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  0.0f, 0.0f, 0.0f));
                Graphics.Blit (tmp, to, m_dofBlurMaterial, blurPass);
            }
            RenderTexture.ReleaseTemporary (tmp);
        }

        void BlurFg ( RenderTexture from, RenderTexture to, DofBlurriness iterations, int blurPass, float spread) {
            // we want a nice, big coc, hence we need to tap once from this (higher resolution) texture
            m_dofBlurMaterial.SetTexture ("_TapHigh", from);

            RenderTexture tmp = RenderTexture.GetTemporary (to.width, to.height);
            if ((int)iterations > 1) {
                BlurHex (from, to, blurPass, spread, tmp);
                if ((int)iterations > 2) {
                    m_dofBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, spread * m_oneOverBaseSize, 0.0f, 0.0f));
                    Graphics.Blit (to, tmp, m_dofBlurMaterial, blurPass);
                    m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  0.0f, 0.0f, 0.0f));
                    Graphics.Blit (tmp, to, m_dofBlurMaterial, blurPass);
                }
            }
            else {
                m_dofBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, spread * m_oneOverBaseSize, 0.0f, 0.0f));
                Graphics.Blit (from, tmp, m_dofBlurMaterial, blurPass);
                m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  0.0f, 0.0f, 0.0f));
                Graphics.Blit (tmp, to, m_dofBlurMaterial, blurPass);
            }
            RenderTexture.ReleaseTemporary (tmp);
        }

        void BlurHex ( RenderTexture from, RenderTexture to, int blurPass, float spread, RenderTexture tmp) {
            m_dofBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, spread * m_oneOverBaseSize, 0.0f, 0.0f));
            Graphics.Blit (from, tmp, m_dofBlurMaterial, blurPass);
            m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  0.0f, 0.0f, 0.0f));
            Graphics.Blit (tmp, to, m_dofBlurMaterial, blurPass);
            m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  spread * m_oneOverBaseSize, 0.0f, 0.0f));
            Graphics.Blit (to, tmp, m_dofBlurMaterial, blurPass);
            m_dofBlurMaterial.SetVector ("offsets", new Vector4 (spread / m_widthOverHeight * m_oneOverBaseSize,  -spread * m_oneOverBaseSize, 0.0f, 0.0f));
            Graphics.Blit (tmp, to, m_dofBlurMaterial, blurPass);
        }

        void Downsample ( RenderTexture from, RenderTexture to) {
            m_dofMaterial.SetVector ("_InvRenderTargetSize", new Vector4 (1.0f / (1.0f * to.width), 1.0f / (1.0f * to.height), 0.0f, 0.0f));
            Graphics.Blit (from, to, m_dofMaterial, smoothDownsamplePass);
        }

        void AddBokeh ( RenderTexture bokehInfo, RenderTexture tempTex, RenderTexture finalTarget) {
            if (m_bokehMaterial) {
                var meshes = Quads.GetMeshes (tempTex.width, tempTex.height);	// quads: exchanging more triangles with less overdraw

                RenderTexture.active = tempTex;
                GL.Clear (false, true, new Color (0.0f, 0.0f, 0.0f, 0.0f));

                GL.PushMatrix ();
                GL.LoadIdentity ();

                // point filter mode is important, otherwise we get bokeh shape & size artefacts
                bokehInfo.filterMode = FilterMode.Point;

                float arW = (bokehInfo.width * 1.0f) / (bokehInfo.height * 1.0f);
                float sc = 2.0f / (1.0f * bokehInfo.width);
                sc += BokehScale * MAXBlurSpread * bokehExtraBlur * m_oneOverBaseSize;

                m_bokehMaterial.SetTexture ("_Source", bokehInfo);
                m_bokehMaterial.SetTexture ("_MainTex", BokehTexture);
                m_bokehMaterial.SetVector ("_ArScale",new Vector4 (sc, sc * arW, 0.5f, 0.5f * arW));
                m_bokehMaterial.SetFloat ("_Intensity", BokehIntensity);
                m_bokehMaterial.SetPass (0);

                foreach(Mesh m in meshes)
                    if (m) Graphics.DrawMeshNow (m, Matrix4x4.identity);

                GL.PopMatrix ();

                Graphics.Blit (tempTex, finalTarget, m_dofMaterial, 8);

                // important to set back as we sample from this later on
                bokehInfo.filterMode = FilterMode.Bilinear;
            }
        }


        void ReleaseTextures () {
            if (m_foregroundTexture) RenderTexture.ReleaseTemporary (m_foregroundTexture);
            if (m_finalDefocus) RenderTexture.ReleaseTemporary (m_finalDefocus);
            if (m_mediumRezWorkTexture) RenderTexture.ReleaseTemporary (m_mediumRezWorkTexture);
            if (m_lowRezWorkTexture) RenderTexture.ReleaseTemporary (m_lowRezWorkTexture);
            if (m_bokehSource) RenderTexture.ReleaseTemporary (m_bokehSource);
            if (m_bokehSource2) RenderTexture.ReleaseTemporary (m_bokehSource2);
        }

        void AllocateTextures ( bool blurForeground,  RenderTexture source, int divider, int lowTexDivider) {
            m_foregroundTexture = null;
            if (blurForeground)
                m_foregroundTexture = RenderTexture.GetTemporary (source.width, source.height, 0);
            m_mediumRezWorkTexture = RenderTexture.GetTemporary (source.width / divider, source.height / divider, 0);
            m_finalDefocus = RenderTexture.GetTemporary (source.width / divider, source.height / divider, 0);
            m_lowRezWorkTexture  = RenderTexture.GetTemporary (source.width / lowTexDivider, source.height / lowTexDivider, 0);
            m_bokehSource = null;
            m_bokehSource2 = null;
            if (Bokeh) {
                m_bokehSource  = RenderTexture.GetTemporary (source.width / (lowTexDivider * BokehDownsample), source.height / (lowTexDivider * BokehDownsample), 0, RenderTextureFormat.ARGBHalf);
                m_bokehSource2  = RenderTexture.GetTemporary (source.width / (lowTexDivider * BokehDownsample), source.height / (lowTexDivider * BokehDownsample), 0,  RenderTextureFormat.ARGBHalf);
                m_bokehSource.filterMode = FilterMode.Bilinear;
                m_bokehSource2.filterMode = FilterMode.Bilinear;
                RenderTexture.active = m_bokehSource2;
                GL.Clear (false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            }

            // to make sure: always use bilinear filter setting

            source.filterMode = FilterMode.Bilinear;
            m_finalDefocus.filterMode = FilterMode.Bilinear;
            m_mediumRezWorkTexture.filterMode = FilterMode.Bilinear;
            m_lowRezWorkTexture.filterMode = FilterMode.Bilinear;
            if (m_foregroundTexture)
                m_foregroundTexture.filterMode = FilterMode.Bilinear;
        }
    }
}
