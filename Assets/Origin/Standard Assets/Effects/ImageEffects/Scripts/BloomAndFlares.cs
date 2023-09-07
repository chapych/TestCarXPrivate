using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    public enum LensflareStyle34
    {
        Ghosting = 0,
        Anamorphic = 1,
        Combined = 2,
    }

    public enum TweakMode34
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

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Bloom and Glow/BloomAndFlares (3.5, Deprecated)")]
    public class BloomAndFlares : PostEffectsBase
    {
        public TweakMode34 TweakMode = 0;
        public BloomScreenBlendMode ScreenBlendMode = BloomScreenBlendMode.Add;

        public HDRBloomMode HDR = HDRBloomMode.Auto;
        private bool m_doHdr = false;
        public float SepBlurSpread = 1.5f;
        public float UseSrcAlphaAsMask = 0.5f;

        public float BloomIntensity = 1.0f;
        public float BloomThreshold = 0.5f;
        public int BloomBlurIterations = 2;

        public bool Lensflares = false;
        public int HollywoodFlareBlurIterations = 2;
        public LensflareStyle34 LensflareMode = (LensflareStyle34)1;
        public float HollyStretchWidth = 3.5f;
        public float LensflareIntensity = 1.0f;
        public float LensflareThreshold = 0.3f;
        public Color FlareColorA = new Color(0.4f, 0.4f, 0.8f, 0.75f);
        public Color FlareColorB = new Color(0.4f, 0.8f, 0.8f, 0.75f);
        public Color FlareColorC = new Color(0.8f, 0.4f, 0.8f, 0.75f);
        public Color FlareColorD = new Color(0.8f, 0.4f, 0.0f, 0.75f);
        public Texture2D LensFlareVignetteMask;

        public Shader LensFlareShader;
        private Material m_lensFlareMaterial;

        public Shader VignetteShader;
        private Material m_vignetteMaterial;

        public Shader SeparableBlurShader;
        private Material m_separableBlurMaterial;

        public Shader AddBrightStuffOneOneShader;
        private Material m_addBrightStuffBlendOneOneMaterial;

        public Shader ScreenBlendShader;
        private Material m_screenBlend;

        public Shader HollywoodFlaresShader;
        private Material m_hollywoodFlaresMaterial;

        public Shader BrightPassFilterShader;
        private Material m_brightPassFilterMaterial;


        public override bool CheckResources()
        {
            CheckSupport(false);

            m_screenBlend = CheckShaderAndCreateMaterial(ScreenBlendShader, m_screenBlend);
            m_lensFlareMaterial = CheckShaderAndCreateMaterial(LensFlareShader, m_lensFlareMaterial);
            m_vignetteMaterial = CheckShaderAndCreateMaterial(VignetteShader, m_vignetteMaterial);
            m_separableBlurMaterial = CheckShaderAndCreateMaterial(SeparableBlurShader, m_separableBlurMaterial);
            m_addBrightStuffBlendOneOneMaterial = CheckShaderAndCreateMaterial(AddBrightStuffOneOneShader, m_addBrightStuffBlendOneOneMaterial);
            m_hollywoodFlaresMaterial = CheckShaderAndCreateMaterial(HollywoodFlaresShader, m_hollywoodFlaresMaterial);
            m_brightPassFilterMaterial = CheckShaderAndCreateMaterial(BrightPassFilterShader, m_brightPassFilterMaterial);

            if (!IsSupported)
                ReportAutoDisable();
            return IsSupported;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CheckResources() == false)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // screen blend is not supported when HDR is enabled (will cap values)

            m_doHdr = false;
            if (HDR == HDRBloomMode.Auto)
                m_doHdr = source.format == RenderTextureFormat.ARGBHalf && GetComponent<Camera>().allowHDR;
            else
            {
                m_doHdr = HDR == HDRBloomMode.On;
            }

            m_doHdr = m_doHdr && SupportHDRTextures;

            BloomScreenBlendMode realBlendMode = ScreenBlendMode;
            if (m_doHdr)
                realBlendMode = BloomScreenBlendMode.Add;

            var rtFormat = (m_doHdr) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
            RenderTexture halfRezColor = RenderTexture.GetTemporary(source.width / 2, source.height / 2, 0, rtFormat);
            RenderTexture quarterRezColor = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, rtFormat);
            RenderTexture secondQuarterRezColor = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, rtFormat);
            RenderTexture thirdQuarterRezColor = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, rtFormat);

            float widthOverHeight = (1.0f * source.width) / (1.0f * source.height);
            float oneOverBaseSize = 1.0f / 512.0f;

            // downsample

            Graphics.Blit(source, halfRezColor, m_screenBlend, 2); // <- 2 is stable downsample
            Graphics.Blit(halfRezColor, quarterRezColor, m_screenBlend, 2); // <- 2 is stable downsample

            RenderTexture.ReleaseTemporary(halfRezColor);

            // cut colors (thresholding)

            BrightFilter(BloomThreshold, UseSrcAlphaAsMask, quarterRezColor, secondQuarterRezColor);
            quarterRezColor.DiscardContents();

            // blurring

            if (BloomBlurIterations < 1) BloomBlurIterations = 1;

            for (int iter = 0; iter < BloomBlurIterations; iter++)
            {
                float spreadForPass = (1.0f + (iter * 0.5f)) * SepBlurSpread;
                m_separableBlurMaterial.SetVector("offsets", new Vector4(0.0f, spreadForPass * oneOverBaseSize, 0.0f, 0.0f));

                RenderTexture src = iter == 0 ? secondQuarterRezColor : quarterRezColor;
                Graphics.Blit(src, thirdQuarterRezColor, m_separableBlurMaterial);
                src.DiscardContents();

                m_separableBlurMaterial.SetVector("offsets", new Vector4((spreadForPass / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                Graphics.Blit(thirdQuarterRezColor, quarterRezColor, m_separableBlurMaterial);
                thirdQuarterRezColor.DiscardContents();
            }

            // lens flares: ghosting, anamorphic or a combination

            if (Lensflares)
            {

                if (LensflareMode == 0)
                {

                    BrightFilter(LensflareThreshold, 0.0f, quarterRezColor, thirdQuarterRezColor);
                    quarterRezColor.DiscardContents();

                    // smooth a little, this needs to be resolution dependent
                    /*
                    separableBlurMaterial.SetVector ("offsets", Vector4 (0.0ff, (2.0ff) / (1.0ff * quarterRezColor.height), 0.0ff, 0.0ff));
                    Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, separableBlurMaterial);
                    separableBlurMaterial.SetVector ("offsets", Vector4 ((2.0ff) / (1.0ff * quarterRezColor.width), 0.0ff, 0.0ff, 0.0ff));
                    Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, separableBlurMaterial);
                    */
                    // no ugly edges!

                    Vignette(0.975f, thirdQuarterRezColor, secondQuarterRezColor);
                    thirdQuarterRezColor.DiscardContents();

                    BlendFlares(secondQuarterRezColor, quarterRezColor);
                    secondQuarterRezColor.DiscardContents();
                }

                // (b) hollywood/anamorphic flares?

                else
                {

                    // thirdQuarter has the brightcut unblurred colors
                    // quarterRezColor is the blurred, brightcut buffer that will end up as bloom

                    m_hollywoodFlaresMaterial.SetVector("_threshold", new Vector4(LensflareThreshold, 1.0f / (1.0f - LensflareThreshold), 0.0f, 0.0f));
                    m_hollywoodFlaresMaterial.SetVector("tintColor", new Vector4(FlareColorA.r, FlareColorA.g, FlareColorA.b, FlareColorA.a) * FlareColorA.a * LensflareIntensity);
                    Graphics.Blit(thirdQuarterRezColor, secondQuarterRezColor, m_hollywoodFlaresMaterial, 2);
                    thirdQuarterRezColor.DiscardContents();

                    Graphics.Blit(secondQuarterRezColor, thirdQuarterRezColor, m_hollywoodFlaresMaterial, 3);
                    secondQuarterRezColor.DiscardContents();

                    m_hollywoodFlaresMaterial.SetVector("offsets", new Vector4((SepBlurSpread * 1.0f / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                    m_hollywoodFlaresMaterial.SetFloat("stretchWidth", HollyStretchWidth);
                    Graphics.Blit(thirdQuarterRezColor, secondQuarterRezColor, m_hollywoodFlaresMaterial, 1);
                    thirdQuarterRezColor.DiscardContents();

                    m_hollywoodFlaresMaterial.SetFloat("stretchWidth", HollyStretchWidth * 2.0f);
                    Graphics.Blit(secondQuarterRezColor, thirdQuarterRezColor, m_hollywoodFlaresMaterial, 1);
                    secondQuarterRezColor.DiscardContents();

                    m_hollywoodFlaresMaterial.SetFloat("stretchWidth", HollyStretchWidth * 4.0f);
                    Graphics.Blit(thirdQuarterRezColor, secondQuarterRezColor, m_hollywoodFlaresMaterial, 1);
                    thirdQuarterRezColor.DiscardContents();

                    if (LensflareMode == (LensflareStyle34)1)
                    {
                        for (int itera = 0; itera < HollywoodFlareBlurIterations; itera++)
                        {
                            m_separableBlurMaterial.SetVector("offsets", new Vector4((HollyStretchWidth * 2.0f / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                            Graphics.Blit(secondQuarterRezColor, thirdQuarterRezColor, m_separableBlurMaterial);
                            secondQuarterRezColor.DiscardContents();

                            m_separableBlurMaterial.SetVector("offsets", new Vector4((HollyStretchWidth * 2.0f / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                            Graphics.Blit(thirdQuarterRezColor, secondQuarterRezColor, m_separableBlurMaterial);
                            thirdQuarterRezColor.DiscardContents();
                        }

                        AddTo(1.0f, secondQuarterRezColor, quarterRezColor);
                        secondQuarterRezColor.DiscardContents();
                    }
                    else
                    {

                        // (c) combined

                        for (int ix = 0; ix < HollywoodFlareBlurIterations; ix++)
                        {
                            m_separableBlurMaterial.SetVector("offsets", new Vector4((HollyStretchWidth * 2.0f / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                            Graphics.Blit(secondQuarterRezColor, thirdQuarterRezColor, m_separableBlurMaterial);
                            secondQuarterRezColor.DiscardContents();

                            m_separableBlurMaterial.SetVector("offsets", new Vector4((HollyStretchWidth * 2.0f / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
                            Graphics.Blit(thirdQuarterRezColor, secondQuarterRezColor, m_separableBlurMaterial);
                            thirdQuarterRezColor.DiscardContents();
                        }

                        Vignette(1.0f, secondQuarterRezColor, thirdQuarterRezColor);
                        secondQuarterRezColor.DiscardContents();

                        BlendFlares(thirdQuarterRezColor, secondQuarterRezColor);
                        thirdQuarterRezColor.DiscardContents();

                        AddTo(1.0f, secondQuarterRezColor, quarterRezColor);
                        secondQuarterRezColor.DiscardContents();
                    }
                }
            }

            // screen blend bloom results to color buffer

            m_screenBlend.SetFloat("_Intensity", BloomIntensity);
            m_screenBlend.SetTexture("_ColorBuffer", source);
            Graphics.Blit(quarterRezColor, destination, m_screenBlend, (int)realBlendMode);

            RenderTexture.ReleaseTemporary(quarterRezColor);
            RenderTexture.ReleaseTemporary(secondQuarterRezColor);
            RenderTexture.ReleaseTemporary(thirdQuarterRezColor);
        }

        private void AddTo(float intensity, RenderTexture from, RenderTexture to)
        {
            m_addBrightStuffBlendOneOneMaterial.SetFloat("_Intensity", intensity);
            Graphics.Blit(from, to, m_addBrightStuffBlendOneOneMaterial);
        }

        private void BlendFlares(RenderTexture from, RenderTexture to)
        {
            m_lensFlareMaterial.SetVector("colorA", new Vector4(FlareColorA.r, FlareColorA.g, FlareColorA.b, FlareColorA.a) * LensflareIntensity);
            m_lensFlareMaterial.SetVector("colorB", new Vector4(FlareColorB.r, FlareColorB.g, FlareColorB.b, FlareColorB.a) * LensflareIntensity);
            m_lensFlareMaterial.SetVector("colorC", new Vector4(FlareColorC.r, FlareColorC.g, FlareColorC.b, FlareColorC.a) * LensflareIntensity);
            m_lensFlareMaterial.SetVector("colorD", new Vector4(FlareColorD.r, FlareColorD.g, FlareColorD.b, FlareColorD.a) * LensflareIntensity);
            Graphics.Blit(from, to, m_lensFlareMaterial);
        }

        private void BrightFilter(float thresh, float useAlphaAsMask, RenderTexture from, RenderTexture to)
        {
            if (m_doHdr)
                m_brightPassFilterMaterial.SetVector("threshold", new Vector4(thresh, 1.0f, 0.0f, 0.0f));
            else
                m_brightPassFilterMaterial.SetVector("threshold", new Vector4(thresh, 1.0f / (1.0f - thresh), 0.0f, 0.0f));
            m_brightPassFilterMaterial.SetFloat("useSrcAlphaAsMask", useAlphaAsMask);
            Graphics.Blit(from, to, m_brightPassFilterMaterial);
        }

        private void Vignette(float amount, RenderTexture from, RenderTexture to)
        {
            if (LensFlareVignetteMask)
            {
                m_screenBlend.SetTexture("_ColorBuffer", LensFlareVignetteMask);
                Graphics.Blit(from, to, m_screenBlend, 3);
            }
            else
            {
                m_vignetteMaterial.SetFloat("vignetteIntensity", amount);
                Graphics.Blit(from, to, m_vignetteMaterial);
            }
        }

    }
}
