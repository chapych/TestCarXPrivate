using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Camera/Depth of Field (Lens Blur, Scatter, DX11)") ]
    public class DepthOfField : PostEffectsBase {

        public bool  VisualizeFocus = false;
        public float FocalLength = 10.0f;
        public float FocalSize = 0.05f;
        public float Aperture = 11.5f;
        public Transform FocalTransform = null;
        public float MAXBlurSize = 2.0f;
        public bool  HighResolution = false;

        public enum BlurType {
            DiscBlur = 0,
            Dx11 = 1,
        }

        public enum BlurSampleCount {
            Low = 0,
            Medium = 1,
            High = 2,
        }

        public BlurType BlurType = BlurType.DiscBlur;
        public BlurSampleCount BlurSampleCount = BlurSampleCount.High;

        public bool  NearBlur = false;
        public float ForegroundOverlap = 1.0f;

        public Shader DofHdrShader;
        private Material m_dofHdrMaterial = null;

        public Shader Dx11BokehShader;
        private Material m_dx11BokehMaterial;

        public float Dx11BokehThreshold = 0.5f;
        public float Dx11SpawnHeuristic = 0.0875f;
        public Texture2D Dx11BokehTexture = null;
        public float Dx11BokehScale = 1.2f;
        public float Dx11BokehIntensity = 2.5f;

        private float m_focalDistance01 = 10.0f;
        private ComputeBuffer m_cbDrawArgs;
        private ComputeBuffer m_cbPoints;
        private float m_internalBlurWidth = 1.0f;


        public override bool CheckResources () {
            CheckSupport (true); // only requires depth, not HDR

            m_dofHdrMaterial = CheckShaderAndCreateMaterial (DofHdrShader, m_dofHdrMaterial);
            if (SupportDx11 && blurType == BlurType.Dx11) {
                m_dx11BokehMaterial = CheckShaderAndCreateMaterial(Dx11BokehShader, m_dx11BokehMaterial);
                CreateComputeResources ();
            }

            if (!IsSupported)
                ReportAutoDisable ();

            return IsSupported;
        }

        void OnEnable () {
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }

        void OnDisable () {
            ReleaseComputeResources ();

            if (m_dofHdrMaterial) DestroyImmediate(m_dofHdrMaterial);
            m_dofHdrMaterial = null;
            if (m_dx11BokehMaterial) DestroyImmediate(m_dx11BokehMaterial);
            m_dx11BokehMaterial = null;
        }

        void ReleaseComputeResources () {
            if (m_cbDrawArgs != null) m_cbDrawArgs.Release();
            m_cbDrawArgs = null;
            if (m_cbPoints != null) m_cbPoints.Release();
            m_cbPoints = null;
        }

        void CreateComputeResources () {
            if (m_cbDrawArgs == null)
            {
                m_cbDrawArgs = new ComputeBuffer (1, 16, ComputeBufferType.IndirectArguments);
                var args= new int[4];
                args[0] = 0; args[1] = 1; args[2] = 0; args[3] = 0;
                m_cbDrawArgs.SetData (args);
            }
            if (m_cbPoints == null)
            {
                m_cbPoints = new ComputeBuffer (90000, 12+16, ComputeBufferType.Append);
            }
        }

        float FocalDistance01 ( float worldDist) {
            return GetComponent<Camera>().WorldToViewportPoint((worldDist-GetComponent<Camera>().nearClipPlane) * GetComponent<Camera>().transform.forward + GetComponent<Camera>().transform.position).z / (GetComponent<Camera>().farClipPlane-GetComponent<Camera>().nearClipPlane);
        }

        private void WriteCoc ( RenderTexture fromTo, bool fgDilate) {
            m_dofHdrMaterial.SetTexture("_FgOverlap", null);

            if (NearBlur && fgDilate) {

                int rtW = fromTo.width/2;
                int rtH = fromTo.height/2;

                // capture fg coc
                RenderTexture temp2 = RenderTexture.GetTemporary (rtW, rtH, 0, fromTo.format);
                Graphics.Blit (fromTo, temp2, m_dofHdrMaterial, 4);

                // special blur
                float fgAdjustment = m_internalBlurWidth * ForegroundOverlap;

                m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, fgAdjustment , 0.0f, fgAdjustment));
                RenderTexture temp1 = RenderTexture.GetTemporary (rtW, rtH, 0, fromTo.format);
                Graphics.Blit (temp2, temp1, m_dofHdrMaterial, 2);
                RenderTexture.ReleaseTemporary(temp2);

                m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (fgAdjustment, 0.0f, 0.0f, fgAdjustment));
                temp2 = RenderTexture.GetTemporary (rtW, rtH, 0, fromTo.format);
                Graphics.Blit (temp1, temp2, m_dofHdrMaterial, 2);
                RenderTexture.ReleaseTemporary(temp1);

                // "merge up" with background COC
                m_dofHdrMaterial.SetTexture("_FgOverlap", temp2);
                fromTo.MarkRestoreExpected(); // only touching alpha channel, RT restore expected
                Graphics.Blit (fromTo, fromTo, m_dofHdrMaterial,  13);
                RenderTexture.ReleaseTemporary(temp2);
            }
            else {
                // capture full coc in alpha channel (fromTo is not read, but bound to detect screen flip)
				fromTo.MarkRestoreExpected(); // only touching alpha channel, RT restore expected
                Graphics.Blit (fromTo, fromTo, m_dofHdrMaterial,  0);
            }
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (!CheckResources ()) {
                Graphics.Blit (source, destination);
                return;
            }

            // clamp & prepare values so they make sense

            if (Aperture < 0.0f) Aperture = 0.0f;
            if (MAXBlurSize < 0.1f) MAXBlurSize = 0.1f;
            FocalSize = Mathf.Clamp(FocalSize, 0.0f, 2.0f);
            m_internalBlurWidth = Mathf.Max(MAXBlurSize, 0.0f);

            // focal & coc calculations

            m_focalDistance01 = (FocalTransform) ? (GetComponent<Camera>().WorldToViewportPoint (FocalTransform.position)).z / (GetComponent<Camera>().farClipPlane) : FocalDistance01 (FocalLength);
            m_dofHdrMaterial.SetVector ("_CurveParams", new Vector4 (1.0f, FocalSize, Aperture/10.0f, m_focalDistance01));

            // possible render texture helpers

            RenderTexture rtLow = null;
            RenderTexture rtLow2 = null;
            RenderTexture rtSuperLow1 = null;
            RenderTexture rtSuperLow2 = null;
            float fgBlurDist = m_internalBlurWidth * ForegroundOverlap;

            if (VisualizeFocus)
            {

                //
                // 2.
                // visualize coc
                //
                //

                WriteCoc (source, true);
                Graphics.Blit (source, destination, m_dofHdrMaterial, 16);
            }
            else if ((blurType == BlurType.Dx11) && m_dx11BokehMaterial)
            {

                //
                // 1.
                // optimized dx11 bokeh scatter
                //
                //


                if (HighResolution) {

                    m_internalBlurWidth = m_internalBlurWidth < 0.1f ? 0.1f : m_internalBlurWidth;
                    fgBlurDist = m_internalBlurWidth * ForegroundOverlap;

                    rtLow = RenderTexture.GetTemporary (source.width, source.height, 0, source.format);

                    var dest2= RenderTexture.GetTemporary (source.width, source.height, 0, source.format);

                    // capture COC
                    WriteCoc (source, false);

                    // blur a bit so we can do a frequency check
                    rtSuperLow1 = RenderTexture.GetTemporary(source.width>>1, source.height>>1, 0, source.format);
                    rtSuperLow2 = RenderTexture.GetTemporary(source.width>>1, source.height>>1, 0, source.format);

                    Graphics.Blit(source, rtSuperLow1, m_dofHdrMaterial, 15);
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, 1.5f , 0.0f, 1.5f));
                    Graphics.Blit (rtSuperLow1, rtSuperLow2, m_dofHdrMaterial, 19);
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (1.5f, 0.0f, 0.0f, 1.5f));
                    Graphics.Blit (rtSuperLow2, rtSuperLow1, m_dofHdrMaterial, 19);

                    // capture fg coc
                    if (NearBlur)
                        Graphics.Blit (source, rtSuperLow2, m_dofHdrMaterial, 4);

                    m_dx11BokehMaterial.SetTexture ("_BlurredColor", rtSuperLow1);
                    m_dx11BokehMaterial.SetFloat ("_SpawnHeuristic", Dx11SpawnHeuristic);
                    m_dx11BokehMaterial.SetVector ("_BokehParams", new Vector4(Dx11BokehScale, Dx11BokehIntensity, Mathf.Clamp(Dx11BokehThreshold, 0.005f, 4.0f), m_internalBlurWidth));
                    m_dx11BokehMaterial.SetTexture ("_FgCocMask", NearBlur ? rtSuperLow2 : null);

                    // collect bokeh candidates and replace with a darker pixel
                    Graphics.SetRandomWriteTarget (1, m_cbPoints);
                    Graphics.Blit (source, rtLow, m_dx11BokehMaterial, 0);
                    Graphics.ClearRandomWriteTargets ();

                    // fg coc blur happens here (after collect!)
                    if (NearBlur) {
                        m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, fgBlurDist , 0.0f, fgBlurDist));
                        Graphics.Blit (rtSuperLow2, rtSuperLow1, m_dofHdrMaterial, 2);
                        m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (fgBlurDist, 0.0f, 0.0f, fgBlurDist));
                        Graphics.Blit (rtSuperLow1, rtSuperLow2, m_dofHdrMaterial, 2);

                        // merge fg coc with bg coc
                        Graphics.Blit (rtSuperLow2, rtLow, m_dofHdrMaterial, 3);
                    }

                    // NEW: LAY OUT ALPHA on destination target so we get nicer outlines for the high rez version
                    Graphics.Blit (rtLow, dest2, m_dofHdrMaterial, 20);

                    // box blur (easier to merge with bokeh buffer)
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (m_internalBlurWidth, 0.0f , 0.0f, m_internalBlurWidth));
                    Graphics.Blit (rtLow, source, m_dofHdrMaterial, 5);
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, m_internalBlurWidth, 0.0f, m_internalBlurWidth));
                    Graphics.Blit (source, dest2, m_dofHdrMaterial, 21);

                    // apply bokeh candidates
                    Graphics.SetRenderTarget (dest2);
                    ComputeBuffer.CopyCount (m_cbPoints, m_cbDrawArgs, 0);
                    m_dx11BokehMaterial.SetBuffer ("pointBuffer", m_cbPoints);
                    m_dx11BokehMaterial.SetTexture ("_MainTex", Dx11BokehTexture);
                    m_dx11BokehMaterial.SetVector ("_Screen", new Vector3(1.0f/(1.0f*source.width), 1.0f/(1.0f*source.height), m_internalBlurWidth));
                    m_dx11BokehMaterial.SetPass (2);

                    Graphics.DrawProceduralIndirectNow (MeshTopology.Points, m_cbDrawArgs, 0);

                    Graphics.Blit (dest2, destination);	// hackaround for DX11 high resolution flipfun (OPTIMIZEME)

                    RenderTexture.ReleaseTemporary(dest2);
                    RenderTexture.ReleaseTemporary(rtSuperLow1);
                    RenderTexture.ReleaseTemporary(rtSuperLow2);
                }
                else {
                    rtLow = RenderTexture.GetTemporary (source.width>>1, source.height>>1, 0, source.format);
                    rtLow2 = RenderTexture.GetTemporary (source.width>>1, source.height>>1, 0, source.format);

                    fgBlurDist = m_internalBlurWidth * ForegroundOverlap;

                    // capture COC & color in low resolution
                    WriteCoc (source, false);
                    source.filterMode = FilterMode.Bilinear;
                    Graphics.Blit (source, rtLow, m_dofHdrMaterial, 6);

                    // blur a bit so we can do a frequency check
                    rtSuperLow1 = RenderTexture.GetTemporary(rtLow.width>>1, rtLow.height>>1, 0, rtLow.format);
                    rtSuperLow2 = RenderTexture.GetTemporary(rtLow.width>>1, rtLow.height>>1, 0, rtLow.format);

                    Graphics.Blit(rtLow, rtSuperLow1, m_dofHdrMaterial, 15);
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, 1.5f , 0.0f, 1.5f));
                    Graphics.Blit (rtSuperLow1, rtSuperLow2, m_dofHdrMaterial, 19);
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (1.5f, 0.0f, 0.0f, 1.5f));
                    Graphics.Blit (rtSuperLow2, rtSuperLow1, m_dofHdrMaterial, 19);

                    RenderTexture rtLow3 = null;

                    if (NearBlur) {
                        // capture fg coc
                        rtLow3 = RenderTexture.GetTemporary (source.width>>1, source.height>>1, 0, source.format);
                        Graphics.Blit (source, rtLow3, m_dofHdrMaterial, 4);
                    }

                    m_dx11BokehMaterial.SetTexture ("_BlurredColor", rtSuperLow1);
                    m_dx11BokehMaterial.SetFloat ("_SpawnHeuristic", Dx11SpawnHeuristic);
                    m_dx11BokehMaterial.SetVector ("_BokehParams", new Vector4(Dx11BokehScale, Dx11BokehIntensity, Mathf.Clamp(Dx11BokehThreshold, 0.005f, 4.0f), m_internalBlurWidth));
                    m_dx11BokehMaterial.SetTexture ("_FgCocMask", rtLow3);

                    // collect bokeh candidates and replace with a darker pixel
                    Graphics.SetRandomWriteTarget (1, m_cbPoints);
                    Graphics.Blit (rtLow, rtLow2, m_dx11BokehMaterial, 0);
                    Graphics.ClearRandomWriteTargets ();

                    RenderTexture.ReleaseTemporary(rtSuperLow1);
                    RenderTexture.ReleaseTemporary(rtSuperLow2);

                    // fg coc blur happens here (after collect!)
                    if (NearBlur) {
                        m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, fgBlurDist , 0.0f, fgBlurDist));
                        Graphics.Blit (rtLow3, rtLow, m_dofHdrMaterial, 2);
                        m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (fgBlurDist, 0.0f, 0.0f, fgBlurDist));
                        Graphics.Blit (rtLow, rtLow3, m_dofHdrMaterial, 2);

                        // merge fg coc with bg coc
                        Graphics.Blit (rtLow3, rtLow2, m_dofHdrMaterial, 3);
                    }

                    // box blur (easier to merge with bokeh buffer)
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (m_internalBlurWidth, 0.0f , 0.0f, m_internalBlurWidth));
                    Graphics.Blit (rtLow2, rtLow, m_dofHdrMaterial, 5);
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, m_internalBlurWidth, 0.0f, m_internalBlurWidth));
                    Graphics.Blit (rtLow, rtLow2, m_dofHdrMaterial, 5);

                    // apply bokeh candidates
                    Graphics.SetRenderTarget (rtLow2);
                    ComputeBuffer.CopyCount (m_cbPoints, m_cbDrawArgs, 0);
                    m_dx11BokehMaterial.SetBuffer ("pointBuffer", m_cbPoints);
                    m_dx11BokehMaterial.SetTexture ("_MainTex", Dx11BokehTexture);
                    m_dx11BokehMaterial.SetVector ("_Screen", new Vector3(1.0f/(1.0f*rtLow2.width), 1.0f/(1.0f*rtLow2.height), m_internalBlurWidth));
                    m_dx11BokehMaterial.SetPass (1);
                    Graphics.DrawProceduralIndirectNow (MeshTopology.Points, m_cbDrawArgs, 0);

                    // upsample & combine
                    m_dofHdrMaterial.SetTexture ("_LowRez", rtLow2);
                    m_dofHdrMaterial.SetTexture ("_FgOverlap", rtLow3);
                    m_dofHdrMaterial.SetVector ("_Offsets",  ((1.0f*source.width)/(1.0f*rtLow2.width)) * m_internalBlurWidth * Vector4.one);
                    Graphics.Blit (source, destination, m_dofHdrMaterial, 9);

                    if (rtLow3) RenderTexture.ReleaseTemporary(rtLow3);
                }
            }
            else
            {

                //
                // 2.
                // poisson disc style blur in low resolution
                //
                //

                source.filterMode = FilterMode.Bilinear;

                if (HighResolution) m_internalBlurWidth *= 2.0f;

                WriteCoc (source, true);

                rtLow = RenderTexture.GetTemporary (source.width >> 1, source.height >> 1, 0, source.format);
                rtLow2 = RenderTexture.GetTemporary (source.width >> 1, source.height >> 1, 0, source.format);

                int blurPass = (blurSampleCount == BlurSampleCount.High || blurSampleCount == BlurSampleCount.Medium) ? 17 : 11;

                if (HighResolution) {
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, m_internalBlurWidth, 0.025f, m_internalBlurWidth));
                    Graphics.Blit (source, destination, m_dofHdrMaterial, blurPass);
                }
                else {
                    m_dofHdrMaterial.SetVector ("_Offsets", new Vector4 (0.0f, m_internalBlurWidth, 0.1f, m_internalBlurWidth));

                    // blur
                    Graphics.Blit (source, rtLow, m_dofHdrMaterial, 6);
                    Graphics.Blit (rtLow, rtLow2, m_dofHdrMaterial, blurPass);

                    // cheaper blur in high resolution, upsample and combine
                    m_dofHdrMaterial.SetTexture("_LowRez", rtLow2);
                    m_dofHdrMaterial.SetTexture("_FgOverlap", null);
                    m_dofHdrMaterial.SetVector ("_Offsets",  Vector4.one * ((1.0f*source.width)/(1.0f*rtLow2.width)) * m_internalBlurWidth);
                    Graphics.Blit (source, destination, m_dofHdrMaterial, blurSampleCount == BlurSampleCount.High ? 18 : 12);
                }
            }

            if (rtLow) RenderTexture.ReleaseTemporary(rtLow);
            if (rtLow2) RenderTexture.ReleaseTemporary(rtLow2);
        }
    }
}
