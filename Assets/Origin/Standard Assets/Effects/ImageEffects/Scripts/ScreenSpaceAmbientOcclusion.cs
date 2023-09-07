using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu("Image Effects/Rendering/Screen Space Ambient Occlusion")]
    public class ScreenSpaceAmbientOcclusion : MonoBehaviour
    {
        public enum SsaoSamples
		{
            Low = 0,
            Medium = 1,
            High = 2,
        }

        public float Radius = 0.4f;
        public SsaoSamples SampleCount = SsaoSamples.Medium;
        public float OcclusionIntensity = 1.5f;
        public int Blur = 2;
        public int Downsampling = 2;
        public float OcclusionAttenuation = 1.0f;
        public float MinZ = 0.01f;

        public Shader SsaoShader;
        private Material m_ssaoMaterial;

        public Texture2D RandomTexture;

        private bool m_supported;

        private static Material CreateMaterial (Shader shader)
        {
            if (!shader)
                return null;
            Material m = new Material (shader);
            m.hideFlags = HideFlags.HideAndDontSave;
            return m;
        }
        private static void DestroyMaterial (Material mat)
        {
            if (mat)
            {
                DestroyImmediate (mat);
                mat = null;
            }
        }


        void OnDisable()
        {
            DestroyMaterial (m_ssaoMaterial);
        }

        void Start()
        {
            if (!SystemInfo.supportsImageEffects || !SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth))
            {
                m_supported = false;
                enabled = false;
                return;
            }

            CreateMaterials ();
            if (!m_ssaoMaterial || m_ssaoMaterial.passCount != 5)
            {
                m_supported = false;
                enabled = false;
                return;
            }

            //CreateRandomTable (26, 0.2f);

            m_supported = true;
        }

        void OnEnable () {
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
        }

        private void CreateMaterials ()
        {
            if (!m_ssaoMaterial && SsaoShader.isSupported)
            {
                m_ssaoMaterial = CreateMaterial (SsaoShader);
                m_ssaoMaterial.SetTexture ("_RandomTexture", RandomTexture);
            }
        }

        [ImageEffectOpaque]
        void OnRenderImage (RenderTexture source, RenderTexture destination)
        {
            if (!m_supported || !SsaoShader.isSupported) {
                enabled = false;
                return;
            }
            CreateMaterials ();

            Downsampling = Mathf.Clamp (Downsampling, 1, 6);
            Radius = Mathf.Clamp (Radius, 0.05f, 1.0f);
            MinZ = Mathf.Clamp (MinZ, 0.00001f, 0.5f);
            OcclusionIntensity = Mathf.Clamp (OcclusionIntensity, 0.5f, 4.0f);
            OcclusionAttenuation = Mathf.Clamp (OcclusionAttenuation, 0.2f, 2.0f);
            Blur = Mathf.Clamp (Blur, 0, 4);

            // Render SSAO term into a smaller texture
            RenderTexture rtAO = RenderTexture.GetTemporary (source.width / Downsampling, source.height / Downsampling, 0);
            float fovY = GetComponent<Camera>().fieldOfView;
            float far = GetComponent<Camera>().farClipPlane;
            float y = Mathf.Tan (fovY * Mathf.Deg2Rad * 0.5f) * far;
            float x = y * GetComponent<Camera>().aspect;
            m_ssaoMaterial.SetVector ("_FarCorner", new Vector3(x,y,far));
            int noiseWidth, noiseHeight;
            if (RandomTexture) {
                noiseWidth = RandomTexture.width;
                noiseHeight = RandomTexture.height;
            } else {
                noiseWidth = 1; noiseHeight = 1;
            }
            m_ssaoMaterial.SetVector ("_NoiseScale", new Vector3 ((float)rtAO.width / noiseWidth, (float)rtAO.height / noiseHeight, 0.0f));
            m_ssaoMaterial.SetVector ("_Params", new Vector4(
                                                     Radius,
                                                     MinZ,
                                                     1.0f / OcclusionAttenuation,
                                                     OcclusionIntensity));

            bool doBlur = Blur > 0;
            Graphics.Blit (doBlur ? null : source, rtAO, m_ssaoMaterial, (int)SampleCount);

            if (doBlur)
            {
                // Blur SSAO horizontally
                RenderTexture rtBlurX = RenderTexture.GetTemporary (source.width, source.height, 0);
                m_ssaoMaterial.SetVector ("_TexelOffsetScale",
                                          new Vector4 ((float)Blur / source.width, 0,0,0));
                m_ssaoMaterial.SetTexture ("_SSAO", rtAO);
                Graphics.Blit (null, rtBlurX, m_ssaoMaterial, 3);
                RenderTexture.ReleaseTemporary (rtAO); // original rtAO not needed anymore

                // Blur SSAO vertically
                RenderTexture rtBlurY = RenderTexture.GetTemporary (source.width, source.height, 0);
                m_ssaoMaterial.SetVector ("_TexelOffsetScale",
                                          new Vector4 (0, (float)Blur/source.height, 0,0));
                m_ssaoMaterial.SetTexture ("_SSAO", rtBlurX);
                Graphics.Blit (source, rtBlurY, m_ssaoMaterial, 3);
                RenderTexture.ReleaseTemporary (rtBlurX); // blurX RT not needed anymore

                rtAO = rtBlurY; // AO is the blurred one now
            }

            // Modulate scene rendering with SSAO
            m_ssaoMaterial.SetTexture ("_SSAO", rtAO);
            Graphics.Blit (source, destination, m_ssaoMaterial, 4);

            RenderTexture.ReleaseTemporary (rtAO);
        }

        /*
		private void CreateRandomTable (int count, float minLength)
		{
			Random.seed = 1337;
			Vector3[] samples = new Vector3[count];
			// initial samples
			for (int i = 0; i < count; ++i)
				samples[i] = Random.onUnitSphere;
			// energy minimization: push samples away from others
			int iterations = 100;
			while (iterations-- > 0) {
				for (int i = 0; i < count; ++i) {
					Vector3 vec = samples[i];
					Vector3 res = Vector3.zero;
					// minimize with other samples
					for (int j = 0; j < count; ++j) {
						Vector3 force = vec - samples[j];
						float fac = Vector3.Dot (force, force);
						if (fac > 0.00001f)
							res += force * (1.0f / fac);
					}
					samples[i] = (samples[i] + res * 0.5f).normalized;
				}
			}
			// now scale samples between minLength and 1.0
			for (int i = 0; i < count; ++i) {
				samples[i] = samples[i] * Random.Range (minLength, 1.0f);
			}

			string table = string.Format ("#define SAMPLE_COUNT {0}\n", count);
			table += "const float3 RAND_SAMPLES[SAMPLE_COUNT] = {\n";
			for (int i = 0; i < count; ++i) {
				Vector3 v = samples[i];
				table += string.Format("\tfloat3({0},{1},{2}),\n", v.x, v.y, v.z);
			}
			table += "};\n";
			Debug.Log (table);
		}
		*/
    }
}
