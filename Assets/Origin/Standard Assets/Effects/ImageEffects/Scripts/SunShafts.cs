using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Rendering/Sun Shafts")]
    public class SunShafts : PostEffectsBase
    {
        public enum SunShaftsResolution
        {
            Low = 0,
            Normal = 1,
            High = 2,
        }

        public enum ShaftsScreenBlendMode
        {
            Screen = 0,
            Add = 1,
        }


        public SunShaftsResolution Resolution = SunShaftsResolution.Normal;
        public ShaftsScreenBlendMode ScreenBlendMode = ShaftsScreenBlendMode.Screen;

        public Transform SunTransform;
        public int RadialBlurIterations = 2;
        public Color SunColor = Color.white;
        public Color SunThreshold = new Color(0.87f,0.74f,0.65f);
        public float SunShaftBlurRadius = 2.5f;
        public float SunShaftIntensity = 1.15f;

        public float MAXRadius = 0.75f;

        public bool  UseDepthTexture = true;

        public Shader SunShaftsShader;
        private Material m_sunShaftsMaterial;

        public Shader SimpleClearShader;
        private Material m_simpleClearMaterial;


        public override bool CheckResources () {
            CheckSupport (UseDepthTexture);

            m_sunShaftsMaterial = CheckShaderAndCreateMaterial (SunShaftsShader, m_sunShaftsMaterial);
            m_simpleClearMaterial = CheckShaderAndCreateMaterial (SimpleClearShader, m_simpleClearMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (CheckResources()==false) {
                Graphics.Blit (source, destination);
                return;
            }

            // we actually need to check this every frame
            if (UseDepthTexture)
                GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            int divider = 4;
            if (Resolution == SunShaftsResolution.Normal)
                divider = 2;
            else if (Resolution == SunShaftsResolution.High)
                divider = 1;

            Vector3 v = Vector3.one * 0.5f;
            if (SunTransform)
                v = GetComponent<Camera>().WorldToViewportPoint (SunTransform.position);
            else
                v = new Vector3(0.5f, 0.5f, 0.0f);

            int rtW = source.width / divider;
            int rtH = source.height / divider;

            RenderTexture lrColorB;
            RenderTexture lrDepthBuffer = RenderTexture.GetTemporary (rtW, rtH, 0);

            // mask out everything except the skybox
            // we have 2 methods, one of which requires depth buffer support, the other one is just comparing images

            m_sunShaftsMaterial.SetVector ("_BlurRadius4", new Vector4 (1.0f, 1.0f, 0.0f, 0.0f) * SunShaftBlurRadius );
            m_sunShaftsMaterial.SetVector ("_SunPosition", new Vector4 (v.x, v.y, v.z, MAXRadius));
            m_sunShaftsMaterial.SetVector ("_SunThreshold", SunThreshold);

            if (!UseDepthTexture) {
                var format= GetComponent<Camera>().allowHDR ? RenderTextureFormat.DefaultHDR: RenderTextureFormat.Default;
                RenderTexture tmpBuffer = RenderTexture.GetTemporary (source.width, source.height, 0, format);
                RenderTexture.active = tmpBuffer;
                GL.ClearWithSkybox (false, GetComponent<Camera>());

                m_sunShaftsMaterial.SetTexture ("_Skybox", tmpBuffer);
                Graphics.Blit (source, lrDepthBuffer, m_sunShaftsMaterial, 3);
                RenderTexture.ReleaseTemporary (tmpBuffer);
            }
            else {
                Graphics.Blit (source, lrDepthBuffer, m_sunShaftsMaterial, 2);
            }

            // paint a small black small border to get rid of clamping problems
            DrawBorder (lrDepthBuffer, m_simpleClearMaterial);

            // radial blur:

            RadialBlurIterations = Mathf.Clamp (RadialBlurIterations, 1, 4);

            float ofs = SunShaftBlurRadius * (1.0f / 768.0f);

            m_sunShaftsMaterial.SetVector ("_BlurRadius4", new Vector4 (ofs, ofs, 0.0f, 0.0f));
            m_sunShaftsMaterial.SetVector ("_SunPosition", new Vector4 (v.x, v.y, v.z, MAXRadius));

            for (int it2 = 0; it2 < RadialBlurIterations; it2++ ) {
                // each iteration takes 2 * 6 samples
                // we update _BlurRadius each time to cheaply get a very smooth look

                lrColorB = RenderTexture.GetTemporary (rtW, rtH, 0);
                Graphics.Blit (lrDepthBuffer, lrColorB, m_sunShaftsMaterial, 1);
                RenderTexture.ReleaseTemporary (lrDepthBuffer);
                ofs = SunShaftBlurRadius * (((it2 * 2.0f + 1.0f) * 6.0f)) / 768.0f;
                m_sunShaftsMaterial.SetVector ("_BlurRadius4", new Vector4 (ofs, ofs, 0.0f, 0.0f) );

                lrDepthBuffer = RenderTexture.GetTemporary (rtW, rtH, 0);
                Graphics.Blit (lrColorB, lrDepthBuffer, m_sunShaftsMaterial, 1);
                RenderTexture.ReleaseTemporary (lrColorB);
                ofs = SunShaftBlurRadius * (((it2 * 2.0f + 2.0f) * 6.0f)) / 768.0f;
                m_sunShaftsMaterial.SetVector ("_BlurRadius4", new Vector4 (ofs, ofs, 0.0f, 0.0f) );
            }

            // put together:

            if (v.z >= 0.0f)
                m_sunShaftsMaterial.SetVector ("_SunColor", new Vector4 (SunColor.r, SunColor.g, SunColor.b, SunColor.a) * SunShaftIntensity);
            else
                m_sunShaftsMaterial.SetVector ("_SunColor", Vector4.zero); // no backprojection !
            m_sunShaftsMaterial.SetTexture ("_ColorBuffer", lrDepthBuffer);
            Graphics.Blit (source, destination, m_sunShaftsMaterial, (ScreenBlendMode == ShaftsScreenBlendMode.Screen) ? 0 : 4);

            RenderTexture.ReleaseTemporary (lrDepthBuffer);
        }
    }
}
