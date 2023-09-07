using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Edge Detection/Crease Shading")]
    class CreaseShading : PostEffectsBase
	{
        public float Intensity = 0.5f;
        public int Softness = 1;
        public float Spread = 1.0f;

        public Shader BlurShader = null;
        private Material m_blurMaterial = null;

        public Shader DepthFetchShader = null;
        private Material m_depthFetchMaterial = null;

        public Shader CreaseApplyShader = null;
        private Material m_creaseApplyMaterial = null;


        public override bool CheckResources ()
		{
            CheckSupport (true);

            m_blurMaterial = CheckShaderAndCreateMaterial (BlurShader, m_blurMaterial);
            m_depthFetchMaterial = CheckShaderAndCreateMaterial (DepthFetchShader, m_depthFetchMaterial);
            m_creaseApplyMaterial = CheckShaderAndCreateMaterial (CreaseApplyShader, m_creaseApplyMaterial);

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

            float widthOverHeight = (1.0f * rtW) / (1.0f * rtH);
            float oneOverBaseSize = 1.0f / 512.0f;

            RenderTexture hrTex = RenderTexture.GetTemporary (rtW, rtH, 0);
            RenderTexture lrTex1 = RenderTexture.GetTemporary (rtW/2, rtH/2, 0);

            Graphics.Blit (source,hrTex, m_depthFetchMaterial);
            Graphics.Blit (hrTex, lrTex1);

            for(int i = 0; i < Softness; i++)
			{
                RenderTexture lrTex2 = RenderTexture.GetTemporary (rtW/2, rtH/2, 0);
                m_blurMaterial.SetVector ("offsets", new Vector4 (0.0f, Spread * oneOverBaseSize, 0.0f, 0.0f));
                Graphics.Blit (lrTex1, lrTex2, m_blurMaterial);
                RenderTexture.ReleaseTemporary (lrTex1);
                lrTex1 = lrTex2;

                lrTex2 = RenderTexture.GetTemporary (rtW/2, rtH/2, 0);
                m_blurMaterial.SetVector ("offsets", new Vector4 (Spread * oneOverBaseSize / widthOverHeight,  0.0f, 0.0f, 0.0f));
                Graphics.Blit (lrTex1, lrTex2, m_blurMaterial);
                RenderTexture.ReleaseTemporary (lrTex1);
                lrTex1 = lrTex2;
            }

            m_creaseApplyMaterial.SetTexture ("_HrDepthTex", hrTex);
            m_creaseApplyMaterial.SetTexture ("_LrDepthTex", lrTex1);
            m_creaseApplyMaterial.SetFloat ("intensity", Intensity);
            Graphics.Blit (source,destination, m_creaseApplyMaterial);

            RenderTexture.ReleaseTemporary (hrTex);
            RenderTexture.ReleaseTemporary (lrTex1);
        }
    }
}
