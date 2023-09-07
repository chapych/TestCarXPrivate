using System;
using UnityEngine;

namespace UnityStandardAssets.Water
{
    [RequireComponent(typeof(WaterBase))]
    [ExecuteInEditMode]
    public class SpecularLighting : MonoBehaviour
    {
        public Transform SpecularLight;
        private WaterBase m_waterBase;


        public void Start()
        {
            m_waterBase = (WaterBase)gameObject.GetComponent(typeof(WaterBase));
        }


        public void Update()
        {
            if (!m_waterBase)
            {
                m_waterBase = (WaterBase)gameObject.GetComponent(typeof(WaterBase));
            }

            if (SpecularLight && m_waterBase.SharedMaterial)
            {
                m_waterBase.SharedMaterial.SetVector("_WorldLightDir", SpecularLight.transform.forward);
            }
        }
    }
}