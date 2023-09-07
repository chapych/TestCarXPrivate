using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Noise/Noise And Grain (Filmic)")]
    public class NoiseAndGrain : PostEffectsBase
	{

        public float IntensityMultiplier = 0.25f;

        public float GeneralIntensity = 0.5f;
        public float BlackIntensity = 1.0f;
        public float WhiteIntensity = 1.0f;
        public float MidGrey = 0.2f;

        public bool  Dx11Grain = false;
        public float Softness = 0.0f;
        public bool  Monochrome = false;

        public Vector3 Intensities = new Vector3(1.0f, 1.0f, 1.0f);
        public Vector3 Tiling = new Vector3(64.0f, 64.0f, 64.0f);
        public float MonochromeTiling = 64.0f;

        public FilterMode FilterMode = FilterMode.Bilinear;

        public Texture2D NoiseTexture;

        public Shader NoiseShader;
        private Material m_noiseMaterial = null;

        public Shader Dx11NoiseShader;
        private Material m_dx11NoiseMaterial = null;

        private static float tileAmount = 64.0f;


        public override bool CheckResources ()
		{
            CheckSupport (false);

            m_noiseMaterial = CheckShaderAndCreateMaterial (NoiseShader, m_noiseMaterial);

            if (Dx11Grain && SupportDx11)
			{
#if UNITY_EDITOR
                Dx11NoiseShader = Shader.Find("Hidden/NoiseAndGrainDX11");
#endif
                m_dx11NoiseMaterial = CheckShaderAndCreateMaterial (Dx11NoiseShader, m_dx11NoiseMaterial);
            }

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources()==false || (null==NoiseTexture))
			{
                Graphics.Blit (source, destination);
                if (null==NoiseTexture) {
                    Debug.LogWarning("Noise & Grain effect failing as noise texture is not assigned. please assign.", transform);
                }
                return;
            }

            Softness = Mathf.Clamp(Softness, 0.0f, 0.99f);

            if (Dx11Grain && SupportDx11)
			{
                // We have a fancy, procedural noise pattern in this version, so no texture needed

                m_dx11NoiseMaterial.SetFloat("_DX11NoiseTime", Time.frameCount);
                m_dx11NoiseMaterial.SetTexture ("_NoiseTex", NoiseTexture);
                m_dx11NoiseMaterial.SetVector ("_NoisePerChannel", Monochrome ? Vector3.one : Intensities);
                m_dx11NoiseMaterial.SetVector ("_MidGrey", new Vector3(MidGrey, 1.0f/(1.0f-MidGrey), -1.0f/MidGrey));
                m_dx11NoiseMaterial.SetVector ("_NoiseAmount", new Vector3(GeneralIntensity, BlackIntensity, WhiteIntensity) * IntensityMultiplier);

                if (Softness > Mathf.Epsilon)
                {
                    RenderTexture rt = RenderTexture.GetTemporary((int) (source.width * (1.0f-Softness)), (int) (source.height * (1.0f-Softness)));
                    DrawNoiseQuadGrid (source, rt, m_dx11NoiseMaterial, NoiseTexture, Monochrome ? 3 : 2);
                    m_dx11NoiseMaterial.SetTexture("_NoiseTex", rt);
                    Graphics.Blit(source, destination, m_dx11NoiseMaterial, 4);
                    RenderTexture.ReleaseTemporary(rt);
                }
                else
                    DrawNoiseQuadGrid (source, destination, m_dx11NoiseMaterial, NoiseTexture, (Monochrome ? 1 : 0));
            }
            else
			{
                // normal noise (DX9 style)

                if (NoiseTexture) {
                    NoiseTexture.wrapMode = TextureWrapMode.Repeat;
                    NoiseTexture.filterMode = FilterMode;
                }

                m_noiseMaterial.SetTexture ("_NoiseTex", NoiseTexture);
                m_noiseMaterial.SetVector ("_NoisePerChannel", Monochrome ? Vector3.one : Intensities);
                m_noiseMaterial.SetVector ("_NoiseTilingPerChannel", Monochrome ? Vector3.one * MonochromeTiling : Tiling);
                m_noiseMaterial.SetVector ("_MidGrey", new Vector3(MidGrey, 1.0f/(1.0f-MidGrey), -1.0f/MidGrey));
                m_noiseMaterial.SetVector ("_NoiseAmount", new Vector3(GeneralIntensity, BlackIntensity, WhiteIntensity) * IntensityMultiplier);

                if (Softness > Mathf.Epsilon)
                {
                    RenderTexture rt2 = RenderTexture.GetTemporary((int) (source.width * (1.0f-Softness)), (int) (source.height * (1.0f-Softness)));
                    DrawNoiseQuadGrid (source, rt2, m_noiseMaterial, NoiseTexture, 2);
                    m_noiseMaterial.SetTexture("_NoiseTex", rt2);
                    Graphics.Blit(source, destination, m_noiseMaterial, 1);
                    RenderTexture.ReleaseTemporary(rt2);
                }
                else
                    DrawNoiseQuadGrid (source, destination, m_noiseMaterial, NoiseTexture, 0);
            }
        }

        static void DrawNoiseQuadGrid (RenderTexture source, RenderTexture dest, Material fxMaterial, Texture2D noise, int passNr)
		{
            RenderTexture.active = dest;

            float noiseSize = (noise.width * 1.0f);
            float subDs = (1.0f * source.width) / tileAmount;

            fxMaterial.SetTexture ("_MainTex", source);

            GL.PushMatrix ();
            GL.LoadOrtho ();

            float aspectCorrection = (1.0f * source.width) / (1.0f * source.height);
            float stepSizeX = 1.0f / subDs;
            float stepSizeY = stepSizeX * aspectCorrection;
            float texTile = noiseSize / (noise.width * 1.0f);

            fxMaterial.SetPass (passNr);

            GL.Begin (GL.QUADS);

            for (float x1 = 0.0f; x1 < 1.0f; x1 += stepSizeX)
			{
                for (float y1 = 0.0f; y1 < 1.0f; y1 += stepSizeY)
				{
                    float tcXStart = Random.Range (0.0f, 1.0f);
                    float tcYStart = Random.Range (0.0f, 1.0f);

                    //Vector3 v3 = Random.insideUnitSphere;
                    //Color c = new Color(v3.x, v3.y, v3.z);

                    tcXStart = Mathf.Floor(tcXStart*noiseSize) / noiseSize;
                    tcYStart = Mathf.Floor(tcYStart*noiseSize) / noiseSize;

                    float texTileMod = 1.0f / noiseSize;

                    GL.MultiTexCoord2 (0, tcXStart, tcYStart);
                    GL.MultiTexCoord2 (1, 0.0f, 0.0f);
                    //GL.Color( c );
                    GL.Vertex3 (x1, y1, 0.1f);
                    GL.MultiTexCoord2 (0, tcXStart + texTile * texTileMod, tcYStart);
                    GL.MultiTexCoord2 (1, 1.0f, 0.0f);
                    //GL.Color( c );
                    GL.Vertex3 (x1 + stepSizeX, y1, 0.1f);
                    GL.MultiTexCoord2 (0, tcXStart + texTile * texTileMod, tcYStart + texTile * texTileMod);
                    GL.MultiTexCoord2 (1, 1.0f, 1.0f);
                    //GL.Color( c );
                    GL.Vertex3 (x1 + stepSizeX, y1 + stepSizeY, 0.1f);
                    GL.MultiTexCoord2 (0, tcXStart, tcYStart + texTile * texTileMod);
                    GL.MultiTexCoord2 (1, 0.0f, 1.0f);
                    //GL.Color( c );
                    GL.Vertex3 (x1, y1 + stepSizeY, 0.1f);
                }
            }

            GL.End ();
            GL.PopMatrix ();
        }
    }
}
