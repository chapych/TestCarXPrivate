using System;
using UnityEngine;

namespace UnityStandardAssets.Water
{
    public enum WaterQuality
    {
        High = 2,
        Medium = 1,
        Low = 0,
    }

    [ExecuteInEditMode]
    public class WaterBase : MonoBehaviour
    {
        public Material SharedMaterial;
        public WaterQuality WaterQuality = WaterQuality.High;
        public bool EdgeBlend = true;


        public void UpdateShader()
        {
            if (WaterQuality > WaterQuality.Medium)
            {
                SharedMaterial.shader.maximumLOD = 501;
            }
            else if (WaterQuality > WaterQuality.Low)
            {
                SharedMaterial.shader.maximumLOD = 301;
            }
            else
            {
                SharedMaterial.shader.maximumLOD = 201;
            }

            // If the system does not support depth textures (ie. NaCl), turn off edge bleeding,
            // as the shader will render everything as transparent if the depth texture is not valid.
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
            {
                EdgeBlend = false;
            }

            if (EdgeBlend)
            {
                Shader.EnableKeyword("WATER_EDGEBLEND_ON");
                Shader.DisableKeyword("WATER_EDGEBLEND_OFF");
                // just to make sure (some peeps might forget to add a water tile to the patches)
                if (Camera.main)
                {
                    Camera.main.depthTextureMode |= DepthTextureMode.Depth;
                }
            }
            else
            {
                Shader.EnableKeyword("WATER_EDGEBLEND_OFF");
                Shader.DisableKeyword("WATER_EDGEBLEND_ON");
            }
        }


        public void WaterTileBeingRendered(Transform tr, Camera currentCam)
        {
            if (currentCam && EdgeBlend)
            {
                currentCam.depthTextureMode |= DepthTextureMode.Depth;
            }
        }


        public void Update()
        {
            if (SharedMaterial)
            {
                UpdateShader();
            }
        }
    }
}