using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    public enum AAMode
    {
        Fxaa2 = 0,
        Fxaa3Console = 1,
        Fxaa1PresetA = 2,
        Fxaa1PresetB = 3,
        Nfaa = 4,
        Ssaa = 5,
        Dlaa = 6,
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof (Camera))]
    [AddComponentMenu("Image Effects/Other/Antialiasing")]
    public class Antialiasing : PostEffectsBase
    {
        public AAMode Mode = AAMode.Fxaa3Console;

        public bool ShowGeneratedNormals = false;
        public float OffsetScale = 0.2f;
        public float BlurRadius = 18.0f;

        public float EdgeThresholdMin = 0.05f;
        public float EdgeThreshold = 0.2f;
        public float EdgeSharpness = 4.0f;

        public bool DlaaSharp = false;

        public Shader SsaaShader;
        private Material m_ssaa;
        public Shader DlaaShader;
        private Material m_dlaa;
        public Shader NfaaShader;
        private Material m_nfaa;
        public Shader ShaderFxaaPreset2;
        private Material m_materialFxaaPreset2;
        public Shader ShaderFxaaPreset3;
        private Material m_materialFxaaPreset3;
        public Shader ShaderFxaaii;
        private Material m_materialFxaaii;
        public Shader ShaderFxaaiii;
        private Material m_materialFxaaiii;


        public Material CurrentAAMaterial()
        {
            Material returnValue = null;

            switch (Mode)
            {
                case AAMode.Fxaa3Console:
                    returnValue = m_materialFxaaiii;
                    break;
                case AAMode.Fxaa2:
                    returnValue = m_materialFxaaii;
                    break;
                case AAMode.Fxaa1PresetA:
                    returnValue = m_materialFxaaPreset2;
                    break;
                case AAMode.Fxaa1PresetB:
                    returnValue = m_materialFxaaPreset3;
                    break;
                case AAMode.Nfaa:
                    returnValue = m_nfaa;
                    break;
                case AAMode.Ssaa:
                    returnValue = m_ssaa;
                    break;
                case AAMode.Dlaa:
                    returnValue = m_dlaa;
                    break;
                default:
                    returnValue = null;
                    break;
            }

            return returnValue;
        }


        public override bool CheckResources()
        {
            CheckSupport(false);

            m_materialFxaaPreset2 = CreateMaterial(ShaderFxaaPreset2, m_materialFxaaPreset2);
            m_materialFxaaPreset3 = CreateMaterial(ShaderFxaaPreset3, m_materialFxaaPreset3);
            m_materialFxaaii = CreateMaterial(ShaderFxaaii, m_materialFxaaii);
            m_materialFxaaiii = CreateMaterial(ShaderFxaaiii, m_materialFxaaiii);
            m_nfaa = CreateMaterial(NfaaShader, m_nfaa);
            m_ssaa = CreateMaterial(SsaaShader, m_ssaa);
            m_dlaa = CreateMaterial(DlaaShader, m_dlaa);

            if (!SsaaShader.isSupported)
            {
                NotSupported();
                ReportAutoDisable();
            }

            return IsSupported;
        }


        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CheckResources() == false)
            {
                Graphics.Blit(source, destination);
                return;
            }

			// ----------------------------------------------------------------
            // FXAA antialiasing modes

            if (Mode == AAMode.Fxaa3Console && (m_materialFxaaiii != null))
            {
                m_materialFxaaiii.SetFloat("_EdgeThresholdMin", EdgeThresholdMin);
                m_materialFxaaiii.SetFloat("_EdgeThreshold", EdgeThreshold);
                m_materialFxaaiii.SetFloat("_EdgeSharpness", EdgeSharpness);

                Graphics.Blit(source, destination, m_materialFxaaiii);
            }
            else if (Mode == AAMode.Fxaa1PresetB && (m_materialFxaaPreset3 != null))
            {
                Graphics.Blit(source, destination, m_materialFxaaPreset3);
            }
            else if (Mode == AAMode.Fxaa1PresetA && m_materialFxaaPreset2 != null)
            {
                source.anisoLevel = 4;
                Graphics.Blit(source, destination, m_materialFxaaPreset2);
                source.anisoLevel = 0;
            }
            else if (Mode == AAMode.Fxaa2 && m_materialFxaaii != null)
            {
                Graphics.Blit(source, destination, m_materialFxaaii);
            }
            else if (Mode == AAMode.Ssaa && m_ssaa != null)
            {
				// ----------------------------------------------------------------
                // SSAA antialiasing
                Graphics.Blit(source, destination, m_ssaa);
            }
            else if (Mode == AAMode.Dlaa && m_dlaa != null)
            {
				// ----------------------------------------------------------------
				// DLAA antialiasing

                source.anisoLevel = 0;
                RenderTexture interim = RenderTexture.GetTemporary(source.width, source.height);
                Graphics.Blit(source, interim, m_dlaa, 0);
                Graphics.Blit(interim, destination, m_dlaa, DlaaSharp ? 2 : 1);
                RenderTexture.ReleaseTemporary(interim);
            }
            else if (Mode == AAMode.Nfaa && m_nfaa != null)
            {
                // ----------------------------------------------------------------
                // nfaa antialiasing

                source.anisoLevel = 0;

                m_nfaa.SetFloat("_OffsetScale", OffsetScale);
                m_nfaa.SetFloat("_BlurRadius", BlurRadius);

                Graphics.Blit(source, destination, m_nfaa, ShowGeneratedNormals ? 1 : 0);
            }
            else
            {
                // none of the AA is supported, fallback to a simple blit
                Graphics.Blit(source, destination);
            }
        }
    }
}
