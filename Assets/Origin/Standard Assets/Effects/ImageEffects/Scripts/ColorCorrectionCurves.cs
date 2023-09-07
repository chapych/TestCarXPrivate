using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu ("Image Effects/Color Adjustments/Color Correction (Curves, Saturation)")]
    public class ColorCorrectionCurves : PostEffectsBase
	{
        public enum ColorCorrectionMode
		{
            Simple = 0,
            Advanced = 1
        }

        public AnimationCurve RedChannel = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));
        public AnimationCurve GreenChannel = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));
        public AnimationCurve BlueChannel = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));

        public bool  UseDepthCorrection = false;

        public AnimationCurve ZCurve = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));
        public AnimationCurve DepthRedChannel = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));
        public AnimationCurve DepthGreenChannel = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));
        public AnimationCurve DepthBlueChannel = new AnimationCurve(new Keyframe(0f,0f), new Keyframe(1f,1f));

        private Material m_ccMaterial;
        private Material m_ccDepthMaterial;
        private Material m_selectiveCcMaterial;

        private Texture2D m_rgbChannelTex;
        private Texture2D m_rgbDepthChannelTex;
        private Texture2D m_zCurveTex;

        public float Saturation = 1.0f;

        public bool  SelectiveCc = false;

        public Color SelectiveFromColor = Color.white;
        public Color SelectiveToColor = Color.white;

        public ColorCorrectionMode Mode;

        public bool  UpdateTextures = true;

        public Shader ColorCorrectionCurvesShader = null;
        public Shader SimpleColorCorrectionCurvesShader = null;
        public Shader ColorCorrectionSelectiveShader = null;

        private bool  m_updateTexturesOnStartup = true;


        new void Start ()
		{
            base.Start ();
            m_updateTexturesOnStartup = true;
        }

        void Awake () {	}


        public override bool CheckResources ()
		{
            CheckSupport (Mode == ColorCorrectionMode.Advanced);

            m_ccMaterial = CheckShaderAndCreateMaterial (SimpleColorCorrectionCurvesShader, m_ccMaterial);
            m_ccDepthMaterial = CheckShaderAndCreateMaterial (ColorCorrectionCurvesShader, m_ccDepthMaterial);
            m_selectiveCcMaterial = CheckShaderAndCreateMaterial (ColorCorrectionSelectiveShader, m_selectiveCcMaterial);

            if (!m_rgbChannelTex)
                m_rgbChannelTex = new Texture2D (256, 4, TextureFormat.ARGB32, false, true);
            if (!m_rgbDepthChannelTex)
                m_rgbDepthChannelTex = new Texture2D (256, 4, TextureFormat.ARGB32, false, true);
            if (!m_zCurveTex)
                m_zCurveTex = new Texture2D (256, 1, TextureFormat.ARGB32, false, true);

            m_rgbChannelTex.hideFlags = HideFlags.DontSave;
            m_rgbDepthChannelTex.hideFlags = HideFlags.DontSave;
            m_zCurveTex.hideFlags = HideFlags.DontSave;

            m_rgbChannelTex.wrapMode = TextureWrapMode.Clamp;
            m_rgbDepthChannelTex.wrapMode = TextureWrapMode.Clamp;
            m_zCurveTex.wrapMode = TextureWrapMode.Clamp;

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        public void UpdateParameters ()
		{
            CheckResources(); // textures might not be created if we're tweaking UI while disabled

            if (RedChannel != null && GreenChannel != null && BlueChannel != null)
			{
                for (float i = 0.0f; i <= 1.0f; i += 1.0f / 255.0f)
				{
                    float rCh = Mathf.Clamp (RedChannel.Evaluate(i), 0.0f, 1.0f);
                    float gCh = Mathf.Clamp (GreenChannel.Evaluate(i), 0.0f, 1.0f);
                    float bCh = Mathf.Clamp (BlueChannel.Evaluate(i), 0.0f, 1.0f);

                    m_rgbChannelTex.SetPixel ((int) Mathf.Floor(i*255.0f), 0, new Color(rCh,rCh,rCh) );
                    m_rgbChannelTex.SetPixel ((int) Mathf.Floor(i*255.0f), 1, new Color(gCh,gCh,gCh) );
                    m_rgbChannelTex.SetPixel ((int) Mathf.Floor(i*255.0f), 2, new Color(bCh,bCh,bCh) );

                    float zC = Mathf.Clamp (ZCurve.Evaluate(i), 0.0f,1.0f);

                    m_zCurveTex.SetPixel ((int) Mathf.Floor(i*255.0f), 0, new Color(zC,zC,zC) );

                    rCh = Mathf.Clamp (DepthRedChannel.Evaluate(i), 0.0f,1.0f);
                    gCh = Mathf.Clamp (DepthGreenChannel.Evaluate(i), 0.0f,1.0f);
                    bCh = Mathf.Clamp (DepthBlueChannel.Evaluate(i), 0.0f,1.0f);

                    m_rgbDepthChannelTex.SetPixel ((int) Mathf.Floor(i*255.0f), 0, new Color(rCh,rCh,rCh) );
                    m_rgbDepthChannelTex.SetPixel ((int) Mathf.Floor(i*255.0f), 1, new Color(gCh,gCh,gCh) );
                    m_rgbDepthChannelTex.SetPixel ((int) Mathf.Floor(i*255.0f), 2, new Color(bCh,bCh,bCh) );
                }

                m_rgbChannelTex.Apply ();
                m_rgbDepthChannelTex.Apply ();
                m_zCurveTex.Apply ();
            }
        }

        void UpdateTextures ()
		{
            UpdateParameters ();
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources()==false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            if (m_updateTexturesOnStartup)
			{
                UpdateParameters ();
                m_updateTexturesOnStartup = false;
            }

            if (UseDepthCorrection)
                GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            RenderTexture renderTarget2Use = destination;

            if (SelectiveCc)
			{
                renderTarget2Use = RenderTexture.GetTemporary (source.width, source.height);
            }

            if (UseDepthCorrection)
			{
                m_ccDepthMaterial.SetTexture ("_RgbTex", m_rgbChannelTex);
                m_ccDepthMaterial.SetTexture ("_ZCurve", m_zCurveTex);
                m_ccDepthMaterial.SetTexture ("_RgbDepthTex", m_rgbDepthChannelTex);
                m_ccDepthMaterial.SetFloat ("_Saturation", Saturation);

                Graphics.Blit (source, renderTarget2Use, m_ccDepthMaterial);
            }
            else
			{
                m_ccMaterial.SetTexture ("_RgbTex", m_rgbChannelTex);
                m_ccMaterial.SetFloat ("_Saturation", Saturation);

                Graphics.Blit (source, renderTarget2Use, m_ccMaterial);
            }

            if (SelectiveCc)
			{
                m_selectiveCcMaterial.SetColor ("selColor", SelectiveFromColor);
                m_selectiveCcMaterial.SetColor ("targetColor", SelectiveToColor);
                Graphics.Blit (renderTarget2Use, destination, m_selectiveCcMaterial);

                RenderTexture.ReleaseTemporary (renderTarget2Use);
            }
        }
    }
}
