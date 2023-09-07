using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Camera/Tilt Shift (Lens Blur)")]
    class TiltShift : PostEffectsBase {
        public enum TiltShiftMode
        {
            TiltShiftMode,
            IrisMode,
        }
        public enum TiltShiftQuality
        {
            Preview,
            Normal,
            High,
        }

        public TiltShiftMode Mode = TiltShiftMode.TiltShiftMode;
        public TiltShiftQuality Quality = TiltShiftQuality.Normal;

        [Range(0.0f, 15.0f)]
        public float BlurArea = 1.0f;

        [Range(0.0f, 25.0f)]
        public float MAXBlurSize = 5.0f;

        [Range(0, 1)]
        public int Downsample = 0;

        public Shader TiltShiftShader = null;
        private Material m_tiltShiftMaterial = null;


        public override bool CheckResources () {
            CheckSupport (true);

            m_tiltShiftMaterial = CheckShaderAndCreateMaterial (TiltShiftShader, m_tiltShiftMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (CheckResources() == false) {
                Graphics.Blit (source, destination);
                return;
            }

            m_tiltShiftMaterial.SetFloat("_BlurSize", MAXBlurSize < 0.0f ? 0.0f : MAXBlurSize);
            m_tiltShiftMaterial.SetFloat("_BlurArea", BlurArea);
            source.filterMode = FilterMode.Bilinear;

            RenderTexture rt = destination;
            if (Downsample > 0f) {
                rt = RenderTexture.GetTemporary (source.width>>Downsample, source.height>>Downsample, 0, source.format);
                rt.filterMode = FilterMode.Bilinear;
            }

            int basePassNr = (int) Quality; basePassNr *= 2;
            Graphics.Blit (source, rt, m_tiltShiftMaterial, Mode == TiltShiftMode.TiltShiftMode ? basePassNr : basePassNr + 1);

            if (Downsample > 0) {
                m_tiltShiftMaterial.SetTexture ("_Blurred", rt);
                Graphics.Blit (source, destination, m_tiltShiftMaterial, 6);
            }

            if (rt != destination)
                RenderTexture.ReleaseTemporary (rt);
        }
    }
}
