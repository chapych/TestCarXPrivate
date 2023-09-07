using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof (Camera))]
    [AddComponentMenu ("Image Effects/Edge Detection/Edge Detection")]
    public class EdgeDetection : PostEffectsBase
    {
        public enum EdgeDetectMode
        {
            TriangleDepthNormals = 0,
            RobertsCrossDepthNormals = 1,
            SobelDepth = 2,
            SobelDepthThin = 3,
            TriangleLuminance = 4,
        }


        public EdgeDetectMode Mode = EdgeDetectMode.SobelDepthThin;
        public float SensitivityDepth = 1.0f;
        public float SensitivityNormals = 1.0f;
        public float LumThreshold = 0.2f;
        public float EdgeExp = 1.0f;
        public float SampleDist = 1.0f;
        public float EdgesOnly = 0.0f;
        public Color EdgesOnlyBgColor = Color.white;

        public Shader EdgeDetectShader;
        private Material m_edgeDetectMaterial = null;
        private EdgeDetectMode m_oldMode = EdgeDetectMode.SobelDepthThin;


        public override bool CheckResources ()
		{
            CheckSupport (true);

            m_edgeDetectMaterial = CheckShaderAndCreateMaterial (EdgeDetectShader,m_edgeDetectMaterial);
            if (Mode != m_oldMode)
                SetCameraFlag ();

            m_oldMode = Mode;

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }


        new void Start ()
		{
            m_oldMode	= Mode;
        }

        void SetCameraFlag ()
		{
            if (Mode == EdgeDetectMode.SobelDepth || Mode == EdgeDetectMode.SobelDepthThin)
                GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
            else if (Mode == EdgeDetectMode.TriangleDepthNormals || Mode == EdgeDetectMode.RobertsCrossDepthNormals)
                GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
        }

        void OnEnable ()
		{
            SetCameraFlag();
        }

        [ImageEffectOpaque]
        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources () == false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            Vector2 sensitivity = new Vector2 (SensitivityDepth, SensitivityNormals);
            m_edgeDetectMaterial.SetVector ("_Sensitivity", new Vector4 (sensitivity.x, sensitivity.y, 1.0f, sensitivity.y));
            m_edgeDetectMaterial.SetFloat ("_BgFade", EdgesOnly);
            m_edgeDetectMaterial.SetFloat ("_SampleDistance", SampleDist);
            m_edgeDetectMaterial.SetVector ("_BgColor", EdgesOnlyBgColor);
            m_edgeDetectMaterial.SetFloat ("_Exponent", EdgeExp);
            m_edgeDetectMaterial.SetFloat ("_Threshold", LumThreshold);

            Graphics.Blit (source, destination, m_edgeDetectMaterial, (int) Mode);
        }
    }
}
