using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [RequireComponent(typeof (Camera))]
    [AddComponentMenu("")]
    public class ImageEffectBase : MonoBehaviour
    {
        /// Provides a shader property that is set in the inspector
        /// and a material instantiated from the shader
        public Shader Shader;

        private Material m_material;


        protected virtual void Start()
        {
            // Disable if we don't support image effects
            if (!SystemInfo.supportsImageEffects)
            {
                enabled = false;
                return;
            }

            // Disable the image effect if the shader can't
            // run on the users graphics card
            if (!Shader || !Shader.isSupported)
                enabled = false;
        }


        protected Material material
        {
            get
            {
                if (m_material == null)
                {
                    m_material = new Material(Shader);
                    m_material.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_material;
            }
        }


        protected virtual void OnDisable()
        {
            if (m_material)
            {
                DestroyImmediate(m_material);
            }
        }
    }
}
