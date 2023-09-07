using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Rendering/Screen Space Ambient Obscurance")]
    class ScreenSpaceAmbientObscurance : PostEffectsBase {
        [Range (0,3)]
        public float Intensity = 0.5f;
        [Range (0.1f,3)]
        public float Radius = 0.2f;
        [Range (0,3)]
        public int BlurIterations = 1;
        [Range (0,5)]
        public float BlurFilterDistance = 1.25f;
        [Range (0,1)]
        public int Downsample = 0;

        public Texture2D Rand = null;
        public Shader AOShader= null;

        private Material m_aoMaterial = null;

        public override bool CheckResources () {
            CheckSupport (true);

            m_aoMaterial = CheckShaderAndCreateMaterial (AOShader, m_aoMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnDisable () {
            if (m_aoMaterial)
                DestroyImmediate (m_aoMaterial);
            m_aoMaterial = null;
        }

        [ImageEffectOpaque]
        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (CheckResources () == false) {
                Graphics.Blit (source, destination);
                return;
            }

            Matrix4x4 p = GetComponent<Camera>().projectionMatrix;
            var invP= p.inverse;
            Vector4 projInfo = new Vector4
                ((-2.0f / (Screen.width * p[0])),
                 (-2.0f / (Screen.height * p[5])),
                 ((1.0f - p[2]) / p[0]),
                 ((1.0f + p[6]) / p[5]));

            m_aoMaterial.SetVector ("_ProjInfo", projInfo); // used for unprojection
            m_aoMaterial.SetMatrix ("_ProjectionInv", invP); // only used for reference
            m_aoMaterial.SetTexture ("_Rand", Rand); // not needed for DX11 :)
            m_aoMaterial.SetFloat ("_Radius", Radius);
            m_aoMaterial.SetFloat ("_Radius2", Radius*Radius);
            m_aoMaterial.SetFloat ("_Intensity", Intensity);
            m_aoMaterial.SetFloat ("_BlurFilterDistance", BlurFilterDistance);

            int rtW = source.width;
            int rtH = source.height;

            RenderTexture tmpRt  = RenderTexture.GetTemporary (rtW>>Downsample, rtH>>Downsample);
            RenderTexture tmpRt2;

            Graphics.Blit (source, tmpRt, m_aoMaterial, 0);

            if (Downsample > 0) {
                tmpRt2 = RenderTexture.GetTemporary (rtW, rtH);
                Graphics.Blit(tmpRt, tmpRt2, m_aoMaterial, 4);
                RenderTexture.ReleaseTemporary (tmpRt);
                tmpRt = tmpRt2;

                // @NOTE: it's probably worth a shot to blur in low resolution
                //  instead with a bilat-upsample afterwards ...
            }

            for (int i = 0; i < BlurIterations; i++) {
                m_aoMaterial.SetVector("_Axis", new Vector2(1.0f,0.0f));
                tmpRt2 = RenderTexture.GetTemporary (rtW, rtH);
                Graphics.Blit (tmpRt, tmpRt2, m_aoMaterial, 1);
                RenderTexture.ReleaseTemporary (tmpRt);

                m_aoMaterial.SetVector("_Axis", new Vector2(0.0f,1.0f));
                tmpRt = RenderTexture.GetTemporary (rtW, rtH);
                Graphics.Blit (tmpRt2, tmpRt, m_aoMaterial, 1);
                RenderTexture.ReleaseTemporary (tmpRt2);
            }

            m_aoMaterial.SetTexture ("_AOTex", tmpRt);
            Graphics.Blit (source, destination, m_aoMaterial, 2);

            RenderTexture.ReleaseTemporary (tmpRt);
        }
    }
}
