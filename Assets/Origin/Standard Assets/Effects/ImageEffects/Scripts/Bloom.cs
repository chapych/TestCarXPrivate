using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Bloom and Glow/Bloom")]
    public class Bloom : PostEffectsBase
    {
        public enum LensFlareStyle
        {
            Ghosting = 0,
            Anamorphic = 1,
            Combined = 2,
        }

        public enum TweakMode
        {
            Basic = 0,
            Complex = 1,
        }

        public enum HDRBloomMode
        {
            Auto = 0,
            On = 1,
            Off = 2,
        }

        public enum BloomScreenBlendMode
        {
            Screen = 0,
            Add = 1,
        }

        public enum BloomQuality
        {
            Cheap = 0,
            High = 1,
        }

        public TweakMode TweakMode = 0;
        public BloomScreenBlendMode ScreenBlendMode = BloomScreenBlendMode.Add;

        public HDRBloomMode HDR = HDRBloomMode.Auto;
        private bool m_doHdr = false;
        public float SepBlurSpread = 2.5f;

        public BloomQuality Quality = BloomQuality.High;

        public float BloomIntensity = 0.5f;
        public float BloomThreshold = 0.5f;
        public Color BloomThresholdColor = Color.white;
        public int BloomBlurIterations = 2;

        public int HollywoodFlareBlurIterations = 2;
        public float FlareRotation = 0.0f;
        public LensFlareStyle LensflareMode = (LensFlareStyle) 1;
        public float HollyStretchWidth = 2.5f;
        public float LensflareIntensity = 0.0f;
        public float LensflareThreshold = 0.3f;
        public float LensFlareSaturation = 0.75f;
        public Color FlareColorA = new Color (0.4f, 0.4f, 0.8f, 0.75f);
        public Color FlareColorB = new Color (0.4f, 0.8f, 0.8f, 0.75f);
        public Color FlareColorC = new Color (0.8f, 0.4f, 0.8f, 0.75f);
        public Color FlareColorD = new Color (0.8f, 0.4f, 0.0f, 0.75f);
        public Texture2D LensFlareVignetteMask;

        public Shader LensFlareShader;
        private Material m_lensFlareMaterial;

        public Shader ScreenBlendShader;
        private Material m_screenBlend;

        public Shader BlurAndFlaresShader;
        private Material m_blurAndFlaresMaterial;

        public Shader BrightPassFilterShader;
        private Material m_brightPassFilterMaterial;


        public override bool CheckResources ()
        {
            CheckSupport (false);

            m_screenBlend = CheckShaderAndCreateMaterial (ScreenBlendShader, m_screenBlend);
            m_lensFlareMaterial = CheckShaderAndCreateMaterial(LensFlareShader,m_lensFlareMaterial);
            m_blurAndFlaresMaterial = CheckShaderAndCreateMaterial (BlurAndFlaresShader, m_blurAndFlaresMaterial);
            m_brightPassFilterMaterial = CheckShaderAndCreateMaterial(BrightPassFilterShader, m_brightPassFilterMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        public void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            if (CheckResources()==false)
            {
                Graphics.Blit (source, destination);
                return;
            }

            // screen blend is not supported when HDR is enabled (will cap values)

            m_doHdr = false;
            if (HDR == HDRBloomMode.Auto)
                m_doHdr = source.format == RenderTextureFormat.ARGBHalf && GetComponent<Camera>().allowHDR;
            else {
                m_doHdr = HDR == HDRBloomMode.On;
            }

            m_doHdr = m_doHdr && SupportHDRTextures;

            BloomScreenBlendMode realBlendMode = ScreenBlendMode;
            if (m_doHdr)
                realBlendMode = BloomScreenBlendMode.Add;

            var rtFormat= (m_doHdr) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
            var rtW2= source.width/2;
            var rtH2= source.height/2;
            var rtW4= source.width/4;
            var rtH4= source.height/4;

            float widthOverHeight = (1.0f * source.width) / (1.0f * source.height);
            float oneOverBaseSize = 1.0f / 512.0f;

            // downsample
            RenderTexture quarterRezColor = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
            RenderTexture halfRezColorDown = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat);
            if (Quality > BloomQuality.Cheap) {
                Graphics.Blit (source, halfRezColorDown, m_screenBlend, 2);
                RenderTexture rtDown4 = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
                Graphics.Blit (halfRezColorDown, rtDown4, m_screenBlend, 2);
                Graphics.Blit (rtDown4, quarterRezColor, m_screenBlend, 6);
                RenderTexture.ReleaseTemporary(rtDown4);
            }
            else {
                Graphics.Blit (source, halfRezColorDown);
                Graphics.Blit (halfRezColorDown, quarterRezColor, m_screenBlend, 6);
            }
            RenderTexture.ReleaseTemporary (halfRezColorDown);

            // cut colors (thresholding)
            RenderTexture secondQuarterRezColor = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
            BrightFilter (BloomThreshold * BloomThresholdColor, quarterRezColor, secondQuarterRezColor);

            // blurring

            if (BloomBlurIterations < 1) BloomBlurIterations = 1;
            else if (BloomBlurIterations > 10) BloomBlurIterations = 10;

            for (int iter = 0; iter < BloomBlurIterations; iter++)
			{
                float spreadForPass = (1.0f + (iter * 0.25f)) * SepBlurSpread;

                // vertical blur
                RenderTexture blur4 = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
                m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 (0.0f, spreadForPass * oneOverBaseSize, 0.0f, 0.0f));
                Graphics.Blit (secondQuarterRezColor, blur4, m_blurAndFlaresMaterial, 4);
                RenderTexture.ReleaseTemporary(secondQuarterRezColor);
                secondQuarterRezColor = blur4;

                // horizontal blur
                blur4 = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
                m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 ((spreadForPass / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                Graphics.Blit (secondQuarterRezColor, blur4, m_blurAndFlaresMaterial, 4);
                RenderTexture.ReleaseTemporary (secondQuarterRezColor);
                secondQuarterRezColor = blur4;

                if (Quality > BloomQuality.Cheap)
				{
                    if (iter == 0)
                    {
                        Graphics.SetRenderTarget(quarterRezColor);
                        GL.Clear(false, true, Color.black); // Clear to avoid RT restore
                        Graphics.Blit (secondQuarterRezColor, quarterRezColor);
                    }
                    else
                    {
                        quarterRezColor.MarkRestoreExpected(); // using max blending, RT restore expected
                        Graphics.Blit (secondQuarterRezColor, quarterRezColor, m_screenBlend, 10);
                    }
                }
            }

            if (Quality > BloomQuality.Cheap)
            {
                Graphics.SetRenderTarget(secondQuarterRezColor);
                GL.Clear(false, true, Color.black); // Clear to avoid RT restore
                Graphics.Blit (quarterRezColor, secondQuarterRezColor, m_screenBlend, 6);
            }

            // lens flares: ghosting, anamorphic or both (ghosted anamorphic flares)

            if (LensflareIntensity > Mathf.Epsilon)
			{

                RenderTexture rtFlares4 = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);

                if (LensflareMode == 0)
				{
                    // ghosting only

                    BrightFilter (LensflareThreshold, secondQuarterRezColor, rtFlares4);

                    if (Quality > BloomQuality.Cheap)
					{
                        // smooth a little
                        m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 (0.0f, (1.5f) / (1.0f * quarterRezColor.height), 0.0f, 0.0f));
                        Graphics.SetRenderTarget(quarterRezColor);
                        GL.Clear(false, true, Color.black); // Clear to avoid RT restore
                        Graphics.Blit (rtFlares4, quarterRezColor, m_blurAndFlaresMaterial, 4);

                        m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 ((1.5f) / (1.0f * quarterRezColor.width), 0.0f, 0.0f, 0.0f));
                        Graphics.SetRenderTarget(rtFlares4);
                        GL.Clear(false, true, Color.black); // Clear to avoid RT restore
                        Graphics.Blit (quarterRezColor, rtFlares4, m_blurAndFlaresMaterial, 4);
                    }

                    // no ugly edges!
                    Vignette (0.975f, rtFlares4, rtFlares4);
                    BlendFlares (rtFlares4, secondQuarterRezColor);
                }
                else
				{

                    //Vignette (0.975ff, rtFlares4, rtFlares4);
                    //DrawBorder(rtFlares4, screenBlend, 8);

                    float flareXRot = 1.0f * Mathf.Cos(FlareRotation);
                    float flareyRot = 1.0f * Mathf.Sin(FlareRotation);

                    float stretchWidth = (HollyStretchWidth * 1.0f / widthOverHeight) * oneOverBaseSize;

                    m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 (flareXRot, flareyRot, 0.0f, 0.0f));
                    m_blurAndFlaresMaterial.SetVector ("_Threshhold", new Vector4 (LensflareThreshold, 1.0f, 0.0f, 0.0f));
                    m_blurAndFlaresMaterial.SetVector ("_TintColor", new Vector4 (FlareColorA.r, FlareColorA.g, FlareColorA.b, FlareColorA.a) * FlareColorA.a * LensflareIntensity);
                    m_blurAndFlaresMaterial.SetFloat ("_Saturation", LensFlareSaturation);

                    // "pre and cut"
                    quarterRezColor.DiscardContents();
                    Graphics.Blit (rtFlares4, quarterRezColor, m_blurAndFlaresMaterial, 2);
                    // "post"
                    rtFlares4.DiscardContents();
                    Graphics.Blit (quarterRezColor, rtFlares4, m_blurAndFlaresMaterial, 3);

                    m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 (flareXRot * stretchWidth, flareyRot * stretchWidth, 0.0f, 0.0f));
                    // stretch 1st
                    m_blurAndFlaresMaterial.SetFloat ("_StretchWidth", HollyStretchWidth);
                    quarterRezColor.DiscardContents();
                    Graphics.Blit (rtFlares4, quarterRezColor, m_blurAndFlaresMaterial, 1);
                    // stretch 2nd
                    m_blurAndFlaresMaterial.SetFloat ("_StretchWidth", HollyStretchWidth * 2.0f);
                    rtFlares4.DiscardContents();
                    Graphics.Blit (quarterRezColor, rtFlares4, m_blurAndFlaresMaterial, 1);
                    // stretch 3rd
                    m_blurAndFlaresMaterial.SetFloat ("_StretchWidth", HollyStretchWidth * 4.0f);
                    quarterRezColor.DiscardContents();
                    Graphics.Blit (rtFlares4, quarterRezColor, m_blurAndFlaresMaterial, 1);

                    // additional blur passes
                    for (int iter = 0; iter < HollywoodFlareBlurIterations; iter++)
					{
                        stretchWidth = (HollyStretchWidth * 2.0f / widthOverHeight) * oneOverBaseSize;

                        m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 (stretchWidth * flareXRot, stretchWidth * flareyRot, 0.0f, 0.0f));
                        rtFlares4.DiscardContents();
                        Graphics.Blit (quarterRezColor, rtFlares4, m_blurAndFlaresMaterial, 4);

                        m_blurAndFlaresMaterial.SetVector ("_Offsets", new Vector4 (stretchWidth * flareXRot, stretchWidth * flareyRot, 0.0f, 0.0f));
                        quarterRezColor.DiscardContents();
                        Graphics.Blit (rtFlares4, quarterRezColor, m_blurAndFlaresMaterial, 4);
                    }

                    if (LensflareMode == (LensFlareStyle) 1)
                        // anamorphic lens flares
                        AddTo (1.0f, quarterRezColor, secondQuarterRezColor);
                    else
					{
                        // "combined" lens flares

                        Vignette (1.0f, quarterRezColor, rtFlares4);
                        BlendFlares (rtFlares4, quarterRezColor);
                        AddTo (1.0f, quarterRezColor, secondQuarterRezColor);
                    }
                }
                RenderTexture.ReleaseTemporary (rtFlares4);
            }

            int blendPass = (int) realBlendMode;
            //if (Mathf.Abs(chromaticBloom) < Mathf.Epsilon)
            //	blendPass += 4;

            m_screenBlend.SetFloat ("_Intensity", BloomIntensity);
            m_screenBlend.SetTexture ("_ColorBuffer", source);

            if (Quality > BloomQuality.Cheap)
			{
                RenderTexture halfRezColorUp = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat);
                Graphics.Blit (secondQuarterRezColor, halfRezColorUp);
                Graphics.Blit (halfRezColorUp, destination, m_screenBlend, blendPass);
                RenderTexture.ReleaseTemporary (halfRezColorUp);
            }
            else
                Graphics.Blit (secondQuarterRezColor, destination, m_screenBlend, blendPass);

            RenderTexture.ReleaseTemporary (quarterRezColor);
            RenderTexture.ReleaseTemporary (secondQuarterRezColor);
        }

        private void AddTo (float intensity, RenderTexture from, RenderTexture to)
        {
            m_screenBlend.SetFloat ("_Intensity", intensity);
            to.MarkRestoreExpected(); // additive blending, RT restore expected
            Graphics.Blit (from, to, m_screenBlend, 9);
        }

        private void BlendFlares (RenderTexture from, RenderTexture to)
        {
            m_lensFlareMaterial.SetVector ("colorA", new Vector4 (FlareColorA.r, FlareColorA.g, FlareColorA.b, FlareColorA.a) * LensflareIntensity);
            m_lensFlareMaterial.SetVector ("colorB", new Vector4 (FlareColorB.r, FlareColorB.g, FlareColorB.b, FlareColorB.a) * LensflareIntensity);
            m_lensFlareMaterial.SetVector ("colorC", new Vector4 (FlareColorC.r, FlareColorC.g, FlareColorC.b, FlareColorC.a) * LensflareIntensity);
            m_lensFlareMaterial.SetVector ("colorD", new Vector4 (FlareColorD.r, FlareColorD.g, FlareColorD.b, FlareColorD.a) * LensflareIntensity);
            to.MarkRestoreExpected(); // additive blending, RT restore expected
            Graphics.Blit (from, to, m_lensFlareMaterial);
        }

        private void BrightFilter (float thresh, RenderTexture from, RenderTexture to)
        {
            m_brightPassFilterMaterial.SetVector ("_Threshhold", new Vector4 (thresh, thresh, thresh, thresh));
            Graphics.Blit (from, to, m_brightPassFilterMaterial, 0);
        }

        private void BrightFilter (Color threshColor,  RenderTexture from, RenderTexture to)
        {
            m_brightPassFilterMaterial.SetVector ("_Threshhold", threshColor);
            Graphics.Blit (from, to, m_brightPassFilterMaterial, 1);
        }

        private void Vignette (float amount, RenderTexture from, RenderTexture to)
        {
            if (LensFlareVignetteMask)
            {
                m_screenBlend.SetTexture ("_ColorBuffer", LensFlareVignetteMask);
                to.MarkRestoreExpected(); // using blending, RT restore expected
                Graphics.Blit (from == to ? null : from, to, m_screenBlend, from == to ? 7 : 3);
            }
            else if (from != to)
            {
                Graphics.SetRenderTarget (to);
                GL.Clear(false, true, Color.black); // clear destination to avoid RT restore
                Graphics.Blit (from, to);
            }
        }
    }
}
