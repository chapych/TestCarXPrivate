using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent(typeof (Camera))]
    [AddComponentMenu("Image Effects/Color Adjustments/Tonemapping")]
    public class Tonemapping : PostEffectsBase
    {
        public enum TonemapperType
        {
            SimpleReinhard,
            UserCurve,
            Hable,
            Photographic,
            OptimizedHejiDawson,
            AdaptiveReinhard,
            AdaptiveReinhardAutoWhite,
        };

        public enum AdaptiveTexSize
        {
            Square16 = 16,
            Square32 = 32,
            Square64 = 64,
            Square128 = 128,
            Square256 = 256,
            Square512 = 512,
            Square1024 = 1024,
        };

        public TonemapperType Type = TonemapperType.Photographic;
        public AdaptiveTexSize AdaptiveTextureSize = AdaptiveTexSize.Square256;

        // CURVE parameter
        public AnimationCurve RemapCurve;
        private Texture2D m_curveTex = null;

        // UNCHARTED parameter
        public float ExposureAdjustment = 1.5f;

        // REINHARD parameter
        public float MiddleGrey = 0.4f;
        public float White = 2.0f;
        public float AdaptionSpeed = 1.5f;

        // usual & internal stuff
        public Shader Tonemapper = null;
        public bool ValidRenderTextureFormat = true;
        private Material m_tonemapMaterial = null;
        private RenderTexture m_rt = null;
        private RenderTextureFormat m_rtFormat = RenderTextureFormat.ARGBHalf;


        public override bool CheckResources()
        {
            CheckSupport(false, true);

            m_tonemapMaterial = CheckShaderAndCreateMaterial(Tonemapper, m_tonemapMaterial);
            if (!m_curveTex && Type == TonemapperType.UserCurve)
            {
                m_curveTex = new Texture2D(256, 1, TextureFormat.ARGB32, false, true);
                m_curveTex.filterMode = FilterMode.Bilinear;
                m_curveTex.wrapMode = TextureWrapMode.Clamp;
                m_curveTex.hideFlags = HideFlags.DontSave;
            }

            if (!IsSupported)
                ReportAutoDisable();
            return IsSupported;
        }


        public float UpdateCurve()
        {
            float range = 1.0f;
            if (RemapCurve.keys.Length < 1)
                RemapCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(2, 1));
            if (RemapCurve != null)
            {
                if (RemapCurve.length > 0)
                    range = RemapCurve[RemapCurve.length - 1].time;
                for (float i = 0.0f; i <= 1.0f; i += 1.0f/255.0f)
                {
                    float c = RemapCurve.Evaluate(i*1.0f*range);
                    m_curveTex.SetPixel((int) Mathf.Floor(i*255.0f), 0, new Color(c, c, c));
                }
                m_curveTex.Apply();
            }
            return 1.0f/range;
        }


        private void OnDisable()
        {
            if (m_rt)
            {
                DestroyImmediate(m_rt);
                m_rt = null;
            }
            if (m_tonemapMaterial)
            {
                DestroyImmediate(m_tonemapMaterial);
                m_tonemapMaterial = null;
            }
            if (m_curveTex)
            {
                DestroyImmediate(m_curveTex);
                m_curveTex = null;
            }
        }


        private bool CreateInternalRenderTexture()
        {
            if (m_rt)
            {
                return false;
            }
            m_rtFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGBHalf;
            m_rt = new RenderTexture(1, 1, 0, m_rtFormat);
            m_rt.hideFlags = HideFlags.DontSave;
            return true;
        }


        // attribute indicates that the image filter chain will continue in LDR
        [ImageEffectTransformsToLDR]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CheckResources() == false)
            {
                Graphics.Blit(source, destination);
                return;
            }

#if UNITY_EDITOR
            ValidRenderTextureFormat = true;
            if (source.format != RenderTextureFormat.ARGBHalf)
            {
                ValidRenderTextureFormat = false;
            }
#endif

            // clamp some values to not go out of a valid range

            ExposureAdjustment = ExposureAdjustment < 0.001f ? 0.001f : ExposureAdjustment;

            // SimpleReinhard tonemappers (local, non adaptive)

            if (Type == TonemapperType.UserCurve)
            {
                float rangeScale = UpdateCurve();
                m_tonemapMaterial.SetFloat("_RangeScale", rangeScale);
                m_tonemapMaterial.SetTexture("_Curve", m_curveTex);
                Graphics.Blit(source, destination, m_tonemapMaterial, 4);
                return;
            }

            if (Type == TonemapperType.SimpleReinhard)
            {
                m_tonemapMaterial.SetFloat("_ExposureAdjustment", ExposureAdjustment);
                Graphics.Blit(source, destination, m_tonemapMaterial, 6);
                return;
            }

            if (Type == TonemapperType.Hable)
            {
                m_tonemapMaterial.SetFloat("_ExposureAdjustment", ExposureAdjustment);
                Graphics.Blit(source, destination, m_tonemapMaterial, 5);
                return;
            }

            if (Type == TonemapperType.Photographic)
            {
                m_tonemapMaterial.SetFloat("_ExposureAdjustment", ExposureAdjustment);
                Graphics.Blit(source, destination, m_tonemapMaterial, 8);
                return;
            }

            if (Type == TonemapperType.OptimizedHejiDawson)
            {
                m_tonemapMaterial.SetFloat("_ExposureAdjustment", 0.5f*ExposureAdjustment);
                Graphics.Blit(source, destination, m_tonemapMaterial, 7);
                return;
            }

            // still here?
            // =>  adaptive tone mapping:
            // builds an average log luminance, tonemaps according to
            // middle grey and white values (user controlled)

            // AdaptiveReinhardAutoWhite will calculate white value automagically

            bool freshlyBrewedInternalRt = CreateInternalRenderTexture(); // this retrieves rtFormat, so should happen before rt allocations

            RenderTexture rtSquared = RenderTexture.GetTemporary((int) AdaptiveTextureSize, (int) AdaptiveTextureSize, 0, m_rtFormat);
            Graphics.Blit(source, rtSquared);

            int downsample = (int) Mathf.Log(rtSquared.width*1.0f, 2);

            int div = 2;
            var rts = new RenderTexture[downsample];
            for (int i = 0; i < downsample; i++)
            {
                rts[i] = RenderTexture.GetTemporary(rtSquared.width/div, rtSquared.width/div, 0, m_rtFormat);
                div *= 2;
            }

            // downsample pyramid

            var lumRt = rts[downsample - 1];
            Graphics.Blit(rtSquared, rts[0], m_tonemapMaterial, 1);
            if (Type == TonemapperType.AdaptiveReinhardAutoWhite)
            {
                for (int i = 0; i < downsample - 1; i++)
                {
                    Graphics.Blit(rts[i], rts[i + 1], m_tonemapMaterial, 9);
                    lumRt = rts[i + 1];
                }
            }
            else if (Type == TonemapperType.AdaptiveReinhard)
            {
                for (int i = 0; i < downsample - 1; i++)
                {
                    Graphics.Blit(rts[i], rts[i + 1]);
                    lumRt = rts[i + 1];
                }
            }

            // we have the needed values, let's apply adaptive tonemapping

            AdaptionSpeed = AdaptionSpeed < 0.001f ? 0.001f : AdaptionSpeed;
            m_tonemapMaterial.SetFloat("_AdaptionSpeed", AdaptionSpeed);

            m_rt.MarkRestoreExpected(); // keeping luminance values between frames, RT restore expected

#if UNITY_EDITOR
            if (Application.isPlaying && !freshlyBrewedInternalRt)
                Graphics.Blit(lumRt, m_rt, m_tonemapMaterial, 2);
            else
                Graphics.Blit(lumRt, m_rt, m_tonemapMaterial, 3);
#else
			Graphics.Blit (lumRt, rt, tonemapMaterial, freshlyBrewedInternalRt ? 3 : 2);
#endif

            MiddleGrey = MiddleGrey < 0.001f ? 0.001f : MiddleGrey;
            m_tonemapMaterial.SetVector("_HdrParams", new Vector4(MiddleGrey, MiddleGrey, MiddleGrey, White*White));
            m_tonemapMaterial.SetTexture("_SmallTex", m_rt);
            if (Type == TonemapperType.AdaptiveReinhard)
            {
                Graphics.Blit(source, destination, m_tonemapMaterial, 0);
            }
            else if (Type == TonemapperType.AdaptiveReinhardAutoWhite)
            {
                Graphics.Blit(source, destination, m_tonemapMaterial, 10);
            }
            else
            {
                Debug.LogError("No valid adaptive tonemapper type found!");
                Graphics.Blit(source, destination); // at least we get the TransformToLDR effect
            }

            // cleanup for adaptive

            for (int i = 0; i < downsample; i++)
            {
                RenderTexture.ReleaseTemporary(rts[i]);
            }
            RenderTexture.ReleaseTemporary(rtSquared);
        }
    }
}
