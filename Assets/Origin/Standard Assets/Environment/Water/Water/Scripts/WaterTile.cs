using System;
using UnityEngine;

namespace UnityStandardAssets.Water
{
    [ExecuteInEditMode]
    public class WaterTile : MonoBehaviour
    {
        public PlanarReflection Reflection;
        public WaterBase WaterBase;


        public void Start()
        {
            AcquireComponents();
        }


        void AcquireComponents()
        {
            if (!Reflection)
            {
                if (transform.parent)
                {
                    Reflection = transform.parent.GetComponent<PlanarReflection>();
                }
                else
                {
                    Reflection = transform.GetComponent<PlanarReflection>();
                }
            }

            if (!WaterBase)
            {
                if (transform.parent)
                {
                    WaterBase = transform.parent.GetComponent<WaterBase>();
                }
                else
                {
                    WaterBase = transform.GetComponent<WaterBase>();
                }
            }
        }


#if UNITY_EDITOR
        public void Update()
        {
            AcquireComponents();
        }
#endif


        public void OnWillRenderObject()
        {
            if (Reflection)
            {
                Reflection.WaterTileBeingRendered(transform, Camera.current);
            }
            if (WaterBase)
            {
                WaterBase.WaterTileBeingRendered(transform, Camera.current);
            }
        }
    }
}