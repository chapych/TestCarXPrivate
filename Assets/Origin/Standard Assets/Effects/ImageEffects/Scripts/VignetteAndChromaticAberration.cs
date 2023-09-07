using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Camera/Vignette and Chromatic Aberration")]
    public class VignetteAndChromaticAberration : PostEffectsBase
    {
        public enum AberrationMode
        {
            Simple = 0,
            Advanced = 1,
        }


        public AberrationMode Mode = AberrationMode.Simple;
        public float Intensity = 0.375f;                    // intensity == 0 disables pre pass (optimization)
        public float ChromaticAberration = 0.2f;
        public float AxialAberration = 0.5f;
        public float Blur = 0.0f;                           // blur == 0 disables blur pass (optimization)
        public float BlurSpread = 0.75f;
        public float LuminanceDependency = 0.25f;
        public float BlurDistance = 2.5f;
        public Shader VignetteShader;
        public Shader SeparableBlurShader;
        public Shader ChromAberrationShader;
        
        
        private Material m_vignetteMaterial;
        private Material m_separableBlurMaterial;
        private Material m_chromAberrationMaterial;


        public override bool CheckResources ()
        {
            CheckSupport (false);

            m_vignetteMaterial = CheckShaderAndCreateMaterial (VignetteShader, m_vignetteMaterial);
            m_separableBlurMaterial = CheckShaderAndCreateMaterial (SeparableBlurShader, m_separableBlurMaterial);
            m_chromAberrationMaterial = CheckShaderAndCreateMaterial (ChromAberrationShader, m_chromAberrationMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }


        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            if ( CheckResources () == false)
            {
                Graphics.Blit (source, destination);
                return;
            }

            int rtW = source.width;
            int rtH = source.height;

            bool  doPrepass = (Mathf.Abs(Blur)>0.0f || Mathf.Abs(Intensity)>0.0f);

            float widthOverHeight = (1.0f * rtW) / (1.0f * rtH);
            const float oneOverBaseSize = 1.0f / 512.0f;

            RenderTexture color = null;
            RenderTexture color2A = null;

            if (doPrepass)
            {
                color = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);

                // Blur corners
                if (Mathf.Abs (Blur)>0.0f)
                {
                    color2A = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);

                    Graphics.Blit (source, color2A, m_chromAberrationMaterial, 0);

                    for(int i = 0; i < 2; i++)
                    {	// maybe make iteration count tweakable
                        m_separableBlurMaterial.SetVector ("offsets",new Vector4 (0.0f, BlurSpread * oneOverBaseSize, 0.0f, 0.0f));
                        RenderTexture color2B = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);
                        Graphics.Blit (color2A, color2B, m_separableBlurMaterial);
                        RenderTexture.ReleaseTemporary (color2A);

                        m_separableBlurMaterial.SetVector ("offsets",new Vector4 (BlurSpread * oneOverBaseSize / widthOverHeight, 0.0f, 0.0f, 0.0f));
                        color2A = RenderTexture.GetTemporary (rtW / 2, rtH / 2, 0, source.format);
                        Graphics.Blit (color2B, color2A, m_separableBlurMaterial);
                        RenderTexture.ReleaseTemporary (color2B);
                    }
                }

                m_vignetteMaterial.SetFloat ("_Intensity", Intensity);		// intensity for vignette
                m_vignetteMaterial.SetFloat ("_Blur", Blur);					// blur intensity
                m_vignetteMaterial.SetTexture ("_VignetteTex", color2A);	// blurred texture

                Graphics.Blit (source, color, m_vignetteMaterial, 0);			// prepass blit: darken & blur corners
            }

            m_chromAberrationMaterial.SetFloat ("_ChromaticAberration", ChromaticAberration);
            m_chromAberrationMaterial.SetFloat ("_AxialAberration", AxialAberration);
            m_chromAberrationMaterial.SetVector ("_BlurDistance", new Vector2 (-BlurDistance, BlurDistance));
            m_chromAberrationMaterial.SetFloat ("_Luminance", 1.0f/Mathf.Max(Mathf.Epsilon, LuminanceDependency));

            if (doPrepass) color.wrapMode = TextureWrapMode.Clamp;
            else source.wrapMode = TextureWrapMode.Clamp;
            Graphics.Blit (doPrepass ? color : source, destination, m_chromAberrationMaterial, Mode == AberrationMode.Advanced ? 2 : 1);

            RenderTexture.ReleaseTemporary (color);
            RenderTexture.ReleaseTemporary (color2A);
        }
    }
}
