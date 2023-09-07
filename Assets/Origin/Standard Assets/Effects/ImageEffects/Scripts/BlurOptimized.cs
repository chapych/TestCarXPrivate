using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Blur/Blur (Optimized)")]
    public class BlurOptimized : PostEffectsBase
    {

        [Range(0, 2)]
        public int Downsample = 1;

        public enum BlurType {
            StandardGauss = 0,
            SgxGauss = 1,
        }

        [Range(0.0f, 10.0f)]
        public float BlurSize = 3.0f;

        [Range(1, 4)]
        public int BlurIterations = 2;

        public BlurType BlurType= BlurType.StandardGauss;

        public Shader BlurShader = null;
        private Material m_blurMaterial = null;


        public override bool CheckResources () {
            CheckSupport (false);

            m_blurMaterial = CheckShaderAndCreateMaterial (BlurShader, m_blurMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        public void OnDisable () {
            if (m_blurMaterial)
                DestroyImmediate (m_blurMaterial);
        }

        public void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (CheckResources() == false) {
                Graphics.Blit (source, destination);
                return;
            }

            float widthMod = 1.0f / (1.0f * (1<<Downsample));

            m_blurMaterial.SetVector ("_Parameter", new Vector4 (BlurSize * widthMod, -BlurSize * widthMod, 0.0f, 0.0f));
            source.filterMode = FilterMode.Bilinear;

            int rtW = source.width >> Downsample;
            int rtH = source.height >> Downsample;

            // downsample
            RenderTexture rt = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);

            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit (source, rt, m_blurMaterial, 0);

            var passOffs= blurType == BlurType.StandardGauss ? 0 : 2;

            for(int i = 0; i < BlurIterations; i++) {
                float iterationOffs = (i*1.0f);
                m_blurMaterial.SetVector ("_Parameter", new Vector4 (BlurSize * widthMod + iterationOffs, -BlurSize * widthMod - iterationOffs, 0.0f, 0.0f));

                // vertical blur
                RenderTexture rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit (rt, rt2, m_blurMaterial, 1 + passOffs);
                RenderTexture.ReleaseTemporary (rt);
                rt = rt2;

                // horizontal blur
                rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit (rt, rt2, m_blurMaterial, 2 + passOffs);
                RenderTexture.ReleaseTemporary (rt);
                rt = rt2;
            }

            Graphics.Blit (rt, destination);

            RenderTexture.ReleaseTemporary (rt);
        }
    }
}
