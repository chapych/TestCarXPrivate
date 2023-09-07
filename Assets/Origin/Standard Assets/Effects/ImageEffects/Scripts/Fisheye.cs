using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Displacement/Fisheye")]
    class Fisheye : PostEffectsBase
	{
        public float StrengthX = 0.05f;
        public float StrengthY = 0.05f;

        public Shader FishEyeShader = null;
        private Material m_fisheyeMaterial = null;


        public override bool CheckResources ()
		{
            CheckSupport (false);
            m_fisheyeMaterial = CheckShaderAndCreateMaterial(FishEyeShader,m_fisheyeMaterial);

            if (!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources()==false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            float oneOverBaseSize = 80.0f / 512.0f; // to keep values more like in the old version of fisheye

            float ar = (source.width * 1.0f) / (source.height * 1.0f);

            m_fisheyeMaterial.SetVector ("intensity", new Vector4 (StrengthX * ar * oneOverBaseSize, StrengthY * oneOverBaseSize, StrengthX * ar * oneOverBaseSize, StrengthY * oneOverBaseSize));
            Graphics.Blit (source, destination, m_fisheyeMaterial);
        }
    }
}
