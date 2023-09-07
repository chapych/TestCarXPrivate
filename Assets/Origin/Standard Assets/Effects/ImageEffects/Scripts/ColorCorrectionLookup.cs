using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [AddComponentMenu ("Image Effects/Color Adjustments/Color Correction (3D Lookup Texture)")]
    public class ColorCorrectionLookup : PostEffectsBase
    {
        public Shader Shader;
        private Material m_material;

        // serialize this instead of having another 2d texture ref'ed
        public Texture3D Converted3DLut = null;
        public string BasedOnTempTex = "";


        public override bool CheckResources () {
            CheckSupport (false);

            m_material = CheckShaderAndCreateMaterial (Shader, m_material);

            if (!IsSupported || !SystemInfo.supports3DTextures)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnDisable () {
            if (m_material) {
                DestroyImmediate (m_material);
                m_material = null;
            }
        }

        void OnDestroy () {
            if (Converted3DLut)
                DestroyImmediate (Converted3DLut);
            Converted3DLut = null;
        }

        public void SetIdentityLut () {
            int dim = 16;
            var newC = new Color[dim*dim*dim];
            float oneOverDim = 1.0f / (1.0f * dim - 1.0f);

            for(int i = 0; i < dim; i++) {
                for(int j = 0; j < dim; j++) {
                    for(int k = 0; k < dim; k++) {
                        newC[i + (j*dim) + (k*dim*dim)] = new Color((i*1.0f)*oneOverDim, (j*1.0f)*oneOverDim, (k*1.0f)*oneOverDim, 1.0f);
                    }
                }
            }

            if (Converted3DLut)
                DestroyImmediate (Converted3DLut);
            Converted3DLut = new Texture3D (dim, dim, dim, TextureFormat.ARGB32, false);
            Converted3DLut.SetPixels (newC);
            Converted3DLut.Apply ();
            BasedOnTempTex = "";
        }

        public bool ValidDimensions ( Texture2D tex2d) {
            if (!tex2d) return false;
            int h = tex2d.height;
            if (h != Mathf.FloorToInt(Mathf.Sqrt(tex2d.width))) {
                return false;
            }
            return true;
        }

        public void Convert ( Texture2D temp2DTex, string path) {

            // conversion fun: the given 2D texture needs to be of the format
            //  w * h, wheras h is the 'depth' (or 3d dimension 'dim') and w = dim * dim

            if (temp2DTex) {
                int dim = temp2DTex.width * temp2DTex.height;
                dim = temp2DTex.height;

                if (!ValidDimensions(temp2DTex)) {
                    Debug.LogWarning ("The given 2D texture " + temp2DTex.name + " cannot be used as a 3D LUT.");
                    BasedOnTempTex = "";
                    return;
                }

                var c = temp2DTex.GetPixels();
                var newC = new Color[c.Length];

                for(int i = 0; i < dim; i++) {
                    for(int j = 0; j < dim; j++) {
                        for(int k = 0; k < dim; k++) {
                            int j = dim-j-1;
                            newC[i + (j*dim) + (k*dim*dim)] = c[k*dim+i+j*dim*dim];
                        }
                    }
                }

                if (Converted3DLut)
                    DestroyImmediate (Converted3DLut);
                Converted3DLut = new Texture3D (dim, dim, dim, TextureFormat.ARGB32, false);
                Converted3DLut.SetPixels (newC);
                Converted3DLut.Apply ();
                BasedOnTempTex = path;
            }
            else {
                // error, something went terribly wrong
                Debug.LogError ("Couldn't color correct with 3D LUT texture. Image Effect will be disabled.");
            }
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (CheckResources () == false || !SystemInfo.supports3DTextures) {
                Graphics.Blit (source, destination);
                return;
            }

            if (Converted3DLut == null) {
                SetIdentityLut ();
            }

            int lutSize = Converted3DLut.width;
            Converted3DLut.wrapMode = TextureWrapMode.Clamp;
            m_material.SetFloat("_Scale", (lutSize - 1) / (1.0f*lutSize));
            m_material.SetFloat("_Offset", 1.0f / (2.0f * lutSize));
            m_material.SetTexture("_ClutTex", Converted3DLut);

            Graphics.Blit (source, destination, m_material, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
        }
    }
}
