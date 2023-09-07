using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Other/Screen Overlay")]
    public class ScreenOverlay : PostEffectsBase
	{
	    public enum OverlayBlendMode
		{
            Additive = 0,
            ScreenBlend = 1,
            Multiply = 2,
            Overlay = 3,
            AlphaBlend = 4,
        }

        public OverlayBlendMode BlendMode = OverlayBlendMode.Overlay;
        public float Intensity = 1.0f;
        public Texture2D Texture = null;

        public Shader OverlayShader = null;
        private Material m_overlayMaterial = null;


        public override bool CheckResources ()
		{
            CheckSupport (false);

            m_overlayMaterial = CheckShaderAndCreateMaterial (OverlayShader, m_overlayMaterial);

            if	(!IsSupported)
                ReportAutoDisable ();
            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
            if (CheckResources() == false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            Vector4 uvTransform = new  Vector4(1, 0, 0, 1);

			#if UNITY_WP8
	    	// WP8 has no OS support for rotating screen with device orientation,
	    	// so we do those transformations ourselves.
			if (Screen.orientation == ScreenOrientation.LandscapeLeft) {
				UV_Transform = new Vector4(0, -1, 1, 0);
			}
			if (Screen.orientation == ScreenOrientation.LandscapeRight) {
				UV_Transform = new Vector4(0, 1, -1, 0);
			}
			if (Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
				UV_Transform = new Vector4(-1, 0, 0, -1);
			}
			#endif

            m_overlayMaterial.SetVector("_UV_Transform", uvTransform);
            m_overlayMaterial.SetFloat ("_Intensity", Intensity);
            m_overlayMaterial.SetTexture ("_Overlay", Texture);
            Graphics.Blit (source, destination, m_overlayMaterial, (int) BlendMode);
        }
    }
}
