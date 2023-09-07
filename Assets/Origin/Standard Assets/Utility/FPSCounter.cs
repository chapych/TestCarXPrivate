using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Utility
{
    [RequireComponent(typeof (Text))]
    public class FPSCounter : MonoBehaviour
    {
        const float FPS_MEASURE_PERIOD = 0.5f;
        private int m_fpsAccumulator = 0;
        private float m_fpsNextPeriod = 0;
        private int m_currentFps;
        const string DISPLAY = "{0} FPS";
        private Text m_text;


        private void Start()
        {
            m_fpsNextPeriod = Time.realtimeSinceStartup + FPS_MEASURE_PERIOD;
            m_text = GetComponent<Text>();
        }


        private void Update()
        {
            // measure average frames per second
            m_fpsAccumulator++;
            if (Time.realtimeSinceStartup > m_fpsNextPeriod)
            {
                m_currentFps = (int) (m_fpsAccumulator/FPS_MEASURE_PERIOD);
                m_fpsAccumulator = 0;
                m_fpsNextPeriod += FPS_MEASURE_PERIOD;
                m_text.text = string.Format(DISPLAY, m_currentFps);
            }
        }
    }
}
