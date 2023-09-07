using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Bloom and Glow/Bloom (Optimized)")]
    public class BloomOptimized : PostEffectsBase
    {

        public enum Resolution
		{
            Low = 0,
            High = 1,
        }

        public enum BlurType
		{
            Standard = 0,
            Sgx = 1,
        }

        [Range(0.0f, 1.5f)]
        public float Threshold = 0.25f;
        [Range(0.0f, 2.5f)]
        public float Intensity = 0.75f;

        [Range(0.25f, 5.5f)]
        public float BlurSize = 1.0f;

        Resolution m_resolution = Resolution.Low;
        [Range(1, 4)]
        public int BlurIterations = 1;

        public BlurType BlurType= BlurType.Standard;

        public Shader FastBloomShader = null;
        private Material m_fastBloomMaterial = null;


        public override bool CheckResources ()
		{
            CheckSupport (false);

            m_fastBloomMaterial = CheckShaderAndCreateMaterial (FastBloomShader, m_fastBloomMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnDisable ()
		{
            if (m_fastBloomMaterial)
                DestroyImmediate (m_fastBloomMaterial);
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources() == false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            int divider = m_resolution == Resolution.Low ? 4 : 2;
            float widthMod = m_resolution == Resolution.Low ? 0.5f : 1.0f;

            m_fastBloomMaterial.SetVector ("_Parameter", new Vector4 (BlurSize * widthMod, 0.0f, Threshold, Intensity));
            source.filterMode = FilterMode.Bilinear;

            var rtW= source.width/divider;
            var rtH= source.height/divider;

            // downsample
            RenderTexture rt = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit (source, rt, m_fastBloomMaterial, 1);

            var passOffs= blurType == BlurType.Standard ? 0 : 2;

            for(int i = 0; i < BlurIterations; i++)
			{
                m_fastBloomMaterial.SetVector ("_Parameter", new Vector4 (BlurSize * widthMod + (i*1.0f), 0.0f, Threshold, Intensity));

                // vertical blur
                RenderTexture rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit (rt, rt2, m_fastBloomMaterial, 2 + passOffs);
                RenderTexture.ReleaseTemporary (rt);
                rt = rt2;

                // horizontal blur
                rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit (rt, rt2, m_fastBloomMaterial, 3 + passOffs);
                RenderTexture.ReleaseTemporary (rt);
                rt = rt2;
            }

            m_fastBloomMaterial.SetTexture ("_Bloom", rt);

            Graphics.Blit (source, destination, m_fastBloomMaterial, 0);

            RenderTexture.ReleaseTemporary (rt);
        }
    }
}
