using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Color Adjustments/Contrast Enhance (Unsharp Mask)")]
    class ContrastEnhance : PostEffectsBase
	{
        public float Intensity = 0.5f;
        public float Threshold = 0.0f;

        private Material m_separableBlurMaterial;
        private Material m_contrastCompositeMaterial;

        public float BlurSpread = 1.0f;

        public Shader SeparableBlurShader = null;
        public Shader ContrastCompositeShader = null;


        public override bool CheckResources ()
		{
            CheckSupport (false);

            m_contrastCompositeMaterial = CheckShaderAndCreateMaterial (ContrastCompositeShader, m_contrastCompositeMaterial);
            m_separableBlurMaterial = CheckShaderAndCreateMaterial (SeparableBlurShader, m_separableBlurMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources()==false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            int rtW = source.width;
            int rtH = source.height;

            RenderTexture color2 = RenderTexture.GetTemporary (rtW/2, rtH/2, 0);

            // downsample

            Graphics.Blit (source, color2);
            RenderTexture color4A = RenderTexture.GetTemporary (rtW/4, rtH/4, 0);
            Graphics.Blit (color2, color4A);
            RenderTexture.ReleaseTemporary (color2);

            // blur

            m_separableBlurMaterial.SetVector ("offsets", new Vector4 (0.0f, (BlurSpread * 1.0f) / color4A.height, 0.0f, 0.0f));
            RenderTexture color4B = RenderTexture.GetTemporary (rtW/4, rtH/4, 0);
            Graphics.Blit (color4A, color4B, m_separableBlurMaterial);
            RenderTexture.ReleaseTemporary (color4A);

            m_separableBlurMaterial.SetVector ("offsets", new Vector4 ((BlurSpread * 1.0f) / color4A.width, 0.0f, 0.0f, 0.0f));
            color4A = RenderTexture.GetTemporary (rtW/4, rtH/4, 0);
            Graphics.Blit (color4B, color4A, m_separableBlurMaterial);
            RenderTexture.ReleaseTemporary (color4B);

            // composite

            m_contrastCompositeMaterial.SetTexture ("_MainTexBlurred", color4A);
            m_contrastCompositeMaterial.SetFloat ("intensity", Intensity);
            m_contrastCompositeMaterial.SetFloat ("threshhold", Threshold);
            Graphics.Blit (source, destination, m_contrastCompositeMaterial);

            RenderTexture.ReleaseTemporary (color4A);
        }
    }
}
