using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Image Effects/Camera/Camera Motion Blur") ]
    public class CameraMotionBlur : PostEffectsBase
    {
        // make sure to match this to MAX_RADIUS in shader ('k' in paper)
        static float maxRadius = 10.0f;

        public enum MotionBlurFilter {
            CameraMotion = 0,			// global screen blur based on cam motion
            LocalBlur = 1,				// cheap blur, no dilation or scattering
            Reconstruction = 2,			// advanced filter (simulates scattering) as in plausible motion blur paper
            ReconstructionDx11 = 3,		// advanced filter (simulates scattering) as in plausible motion blur paper
            ReconstructionDisc = 4,		// advanced filter using scaled poisson disc sampling
        }

        // settings
        public MotionBlurFilter FilterType = MotionBlurFilter.Reconstruction;
        public bool  Preview = false;				// show how blur would look like in action ...
        public Vector3 PreviewScale = Vector3.one;	// ... given this movement vector

        // params
        public float MovementScale = 0.0f;
        public float RotationScale = 1.0f;
        public float MAXVelocity = 8.0f;	// maximum velocity in pixels
        public float MINVelocity = 0.1f;	// minimum velocity in pixels
        public float VelocityScale = 0.375f;	// global velocity scale
        public float SoftZDistance = 0.005f;	// for z overlap check softness (reconstruction filter only)
        public int VelocityDownsample = 1;	// low resolution velocity buffer? (optimization)
        public LayerMask ExcludeLayers = 0;
        private GameObject m_tmpCam = null;

        // resources
        public Shader Shader;
        public Shader Dx11MotionBlurShader;
        public Shader ReplacementClear;

        private Material m_motionBlurMaterial = null;
        private Material m_dx11MotionBlurMaterial = null;

        public Texture2D NoiseTexture = null;
        public float Jitter = 0.05f;

        // (internal) debug
        public bool  ShowVelocity = false;
        public float ShowVelocityScale = 1.0f;

        // camera transforms
        private Matrix4x4 m_currentViewProjMat;
        private Matrix4x4 m_prevViewProjMat;
        private int m_prevFrameCount;
        private bool  m_wasActive;
        // shortcuts to calculate global blur direction when using 'CameraMotion'
        private Vector3 m_prevFrameForward = Vector3.forward;
        private Vector3 m_prevFrameUp = Vector3.up;
        private Vector3 m_prevFramePos = Vector3.zero;
        private Camera m_camera;


        private void CalculateViewProjection () {
            Matrix4x4 viewMat = m_camera.worldToCameraMatrix;
            Matrix4x4 projMat = GL.GetGPUProjectionMatrix (m_camera.projectionMatrix, true);
            m_currentViewProjMat = projMat * viewMat;
        }


        new void Start () {
            CheckResources ();

            if (m_camera == null)
                m_camera = GetComponent<Camera>();

            m_wasActive = gameObject.activeInHierarchy;
            CalculateViewProjection ();
            Remember ();
            m_wasActive = false; // hack to fake position/rotation update and prevent bad blurs
        }

        void OnEnable () {

            if (m_camera == null)
                m_camera = GetComponent<Camera>();

            m_camera.depthTextureMode |= DepthTextureMode.Depth;
        }

        void OnDisable () {
            if (null != m_motionBlurMaterial) {
                DestroyImmediate (m_motionBlurMaterial);
                m_motionBlurMaterial = null;
            }
            if (null != m_dx11MotionBlurMaterial) {
                DestroyImmediate (m_dx11MotionBlurMaterial);
                m_dx11MotionBlurMaterial = null;
            }
            if (null != m_tmpCam) {
                DestroyImmediate (m_tmpCam);
                m_tmpCam = null;
            }
        }


        public override bool CheckResources () {
            CheckSupport (true, true); // depth & hdr needed
            m_motionBlurMaterial = CheckShaderAndCreateMaterial (Shader, m_motionBlurMaterial);

            if (SupportDx11 && FilterType == MotionBlurFilter.ReconstructionDx11) {
                m_dx11MotionBlurMaterial = CheckShaderAndCreateMaterial (Dx11MotionBlurShader, m_dx11MotionBlurMaterial);
            }

            if (!IsSupported)
                ReportAutoDisable ();

            return IsSupported;
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination) {
            if (false == CheckResources ()) {
                Graphics.Blit (source, destination);
                return;
            }

            if (FilterType == MotionBlurFilter.CameraMotion)
                StartFrame ();

            // use if possible new RG format ... fallback to half otherwise
            var rtFormat= SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGBHalf;

            // get temp textures
            RenderTexture velBuffer = RenderTexture.GetTemporary (DivRoundUp (source.width, VelocityDownsample), DivRoundUp (source.height, VelocityDownsample), 0, rtFormat);
            int tileWidth = 1;
            int tileHeight = 1;
            MAXVelocity = Mathf.Max (2.0f, MAXVelocity);

            float maxVelocity = MAXVelocity; // calculate 'k'
            // note: 's' is hardcoded in shaders except for DX11 path

            // auto DX11 fallback!
            bool fallbackFromDx11 = FilterType == MotionBlurFilter.ReconstructionDx11 && m_dx11MotionBlurMaterial == null;

            if (FilterType == MotionBlurFilter.Reconstruction || fallbackFromDx11 || FilterType == MotionBlurFilter.ReconstructionDisc) {
                MAXVelocity = Mathf.Min (MAXVelocity, maxRadius);
                tileWidth = DivRoundUp (velBuffer.width, (int) MAXVelocity);
                tileHeight = DivRoundUp (velBuffer.height, (int) MAXVelocity);
                maxVelocity = velBuffer.width/tileWidth;
            }
            else {
                tileWidth = DivRoundUp (velBuffer.width, (int) MAXVelocity);
                tileHeight = DivRoundUp (velBuffer.height, (int) MAXVelocity);
                maxVelocity = velBuffer.width/tileWidth;
            }

            RenderTexture tileMax  = RenderTexture.GetTemporary (tileWidth, tileHeight, 0, rtFormat);
            RenderTexture neighbourMax  = RenderTexture.GetTemporary (tileWidth, tileHeight, 0, rtFormat);
            velBuffer.filterMode = FilterMode.Point;
            tileMax.filterMode = FilterMode.Point;
            neighbourMax.filterMode = FilterMode.Point;
            if (NoiseTexture) NoiseTexture.filterMode = FilterMode.Point;
            source.wrapMode = TextureWrapMode.Clamp;
            velBuffer.wrapMode = TextureWrapMode.Clamp;
            neighbourMax.wrapMode = TextureWrapMode.Clamp;
            tileMax.wrapMode = TextureWrapMode.Clamp;

            // calc correct viewprj matrix
            CalculateViewProjection ();

            // just started up?
            if (gameObject.activeInHierarchy && !m_wasActive) {
                Remember ();
            }
            m_wasActive = gameObject.activeInHierarchy;

            // matrices
            Matrix4x4 invViewPrj = Matrix4x4.Inverse (m_currentViewProjMat);
            m_motionBlurMaterial.SetMatrix ("_InvViewProj", invViewPrj);
            m_motionBlurMaterial.SetMatrix ("_PrevViewProj", m_prevViewProjMat);
            m_motionBlurMaterial.SetMatrix ("_ToPrevViewProjCombined", m_prevViewProjMat * invViewPrj);

            m_motionBlurMaterial.SetFloat ("_MaxVelocity", maxVelocity);
            m_motionBlurMaterial.SetFloat ("_MaxRadiusOrKInPaper", maxVelocity);
            m_motionBlurMaterial.SetFloat ("_MinVelocity", MINVelocity);
            m_motionBlurMaterial.SetFloat ("_VelocityScale", VelocityScale);
            m_motionBlurMaterial.SetFloat ("_Jitter", Jitter);

            // texture samplers
            m_motionBlurMaterial.SetTexture ("_NoiseTex", NoiseTexture);
            m_motionBlurMaterial.SetTexture ("_VelTex", velBuffer);
            m_motionBlurMaterial.SetTexture ("_NeighbourMaxTex", neighbourMax);
            m_motionBlurMaterial.SetTexture ("_TileTexDebug", tileMax);

            if (Preview) {
                // generate an artifical 'previous' matrix to simulate blur look
                Matrix4x4 viewMat = m_camera.worldToCameraMatrix;
                Matrix4x4 offset = Matrix4x4.identity;
                offset.SetTRS(PreviewScale * 0.3333f, Quaternion.identity, Vector3.one); // using only translation
                Matrix4x4 projMat = GL.GetGPUProjectionMatrix (m_camera.projectionMatrix, true);
                m_prevViewProjMat = projMat * offset * viewMat;
                m_motionBlurMaterial.SetMatrix ("_PrevViewProj", m_prevViewProjMat);
                m_motionBlurMaterial.SetMatrix ("_ToPrevViewProjCombined", m_prevViewProjMat * invViewPrj);
            }

            if (FilterType == MotionBlurFilter.CameraMotion)
            {
                // build blur vector to be used in shader to create a global blur direction
                Vector4 blurVector = Vector4.zero;

                float lookUpDown = Vector3.Dot (transform.up, Vector3.up);
                Vector3 distanceVector = m_prevFramePos-transform.position;

                float distMag = distanceVector.magnitude;

                float farHeur = 1.0f;

                // pitch (vertical)
                farHeur = (Vector3.Angle (transform.up, m_prevFrameUp) / m_camera.fieldOfView) * (source.width * 0.75f);
                blurVector.x =  RotationScale * farHeur;//Mathf.Clamp01((1.0ff-Vector3.Dot(transform.up, prevFrameUp)));

                // yaw #1 (horizontal, faded by pitch)
                farHeur = (Vector3.Angle (transform.forward, m_prevFrameForward) / m_camera.fieldOfView) * (source.width * 0.75f);
                blurVector.y = RotationScale * lookUpDown * farHeur;//Mathf.Clamp01((1.0ff-Vector3.Dot(transform.forward, prevFrameForward)));

                // yaw #2 (when looking down, faded by 1-pitch)
                farHeur = (Vector3.Angle (transform.forward, m_prevFrameForward) / m_camera.fieldOfView) * (source.width * 0.75f);
                blurVector.z = RotationScale * (1.0f- lookUpDown) * farHeur;//Mathf.Clamp01((1.0ff-Vector3.Dot(transform.forward, prevFrameForward)));

                if (distMag > Mathf.Epsilon && MovementScale > Mathf.Epsilon) {
                    // forward (probably most important)
                    blurVector.w = MovementScale * (Vector3.Dot (transform.forward, distanceVector) ) * (source.width * 0.5f);
                    // jump (maybe scale down further)
                    blurVector.x += MovementScale * (Vector3.Dot (transform.up, distanceVector) ) * (source.width * 0.5f);
                    // strafe (maybe scale down further)
                    blurVector.y += MovementScale * (Vector3.Dot (transform.right, distanceVector) ) * (source.width * 0.5f);
                }

                if (Preview) // crude approximation
                    m_motionBlurMaterial.SetVector ("_BlurDirectionPacked", new Vector4 (PreviewScale.y, PreviewScale.x, 0.0f, PreviewScale.z) * 0.5f * m_camera.fieldOfView);
                else
                    m_motionBlurMaterial.SetVector ("_BlurDirectionPacked", blurVector);
            }
            else {
                // generate velocity buffer
                Graphics.Blit (source, velBuffer, m_motionBlurMaterial, 0);

                // patch up velocity buffer:

                // exclude certain layers (e.g. skinned objects as we cant really support that atm)

                Camera cam = null;
                if (ExcludeLayers.value != 0)// || dynamicLayers.value)
                    cam = GetTmpCam ();

                if (cam && ExcludeLayers.value != 0 && ReplacementClear && ReplacementClear.isSupported) {
                    cam.targetTexture = velBuffer;
                    cam.cullingMask = ExcludeLayers;
                    cam.RenderWithShader (ReplacementClear, "");
                }
            }

            if (!Preview && Time.frameCount != m_prevFrameCount) {
                // remember current transformation data for next frame
                m_prevFrameCount = Time.frameCount;
                Remember ();
            }

            source.filterMode = FilterMode.Bilinear;

            // debug vel buffer:
            if (ShowVelocity) {
                // generate tile max and neighbour max
                //Graphics.Blit (velBuffer, tileMax, motionBlurMaterial, 2);
                //Graphics.Blit (tileMax, neighbourMax, motionBlurMaterial, 3);
                m_motionBlurMaterial.SetFloat ("_DisplayVelocityScale", ShowVelocityScale);
                Graphics.Blit (velBuffer, destination, m_motionBlurMaterial, 1);
            }
            else {
                if (FilterType == MotionBlurFilter.ReconstructionDx11 && !fallbackFromDx11) {
                    // need to reset some parameters for dx11 shader
                    m_dx11MotionBlurMaterial.SetFloat ("_MinVelocity", MINVelocity);
                    m_dx11MotionBlurMaterial.SetFloat ("_VelocityScale", VelocityScale);
                    m_dx11MotionBlurMaterial.SetFloat ("_Jitter", Jitter);

                    // texture samplers
                    m_dx11MotionBlurMaterial.SetTexture ("_NoiseTex", NoiseTexture);
                    m_dx11MotionBlurMaterial.SetTexture ("_VelTex", velBuffer);
                    m_dx11MotionBlurMaterial.SetTexture ("_NeighbourMaxTex", neighbourMax);

                    m_dx11MotionBlurMaterial.SetFloat ("_SoftZDistance", Mathf.Max(0.00025f, SoftZDistance) );
                    m_dx11MotionBlurMaterial.SetFloat ("_MaxRadiusOrKInPaper", maxVelocity);

                    // generate tile max and neighbour max
                    Graphics.Blit (velBuffer, tileMax, m_dx11MotionBlurMaterial, 0);
                    Graphics.Blit (tileMax, neighbourMax, m_dx11MotionBlurMaterial, 1);

                    // final blur
                    Graphics.Blit (source, destination, m_dx11MotionBlurMaterial, 2);
                }
                else if (FilterType == MotionBlurFilter.Reconstruction || fallbackFromDx11) {
                    // 'reconstructing' properly integrated color
                    m_motionBlurMaterial.SetFloat ("_SoftZDistance", Mathf.Max(0.00025f, SoftZDistance) );

                    // generate tile max and neighbour max
                    Graphics.Blit (velBuffer, tileMax, m_motionBlurMaterial, 2);
                    Graphics.Blit (tileMax, neighbourMax, m_motionBlurMaterial, 3);

                    // final blur
                    Graphics.Blit (source, destination, m_motionBlurMaterial, 4);
                }
                else if (FilterType == MotionBlurFilter.CameraMotion) {
                    // orange box style motion blur
                    Graphics.Blit (source, destination, m_motionBlurMaterial, 6);
                }
                else if (FilterType == MotionBlurFilter.ReconstructionDisc) {
                    // dof style motion blur defocuing and ellipse around the princical blur direction
                    // 'reconstructing' properly integrated color
                    m_motionBlurMaterial.SetFloat ("_SoftZDistance", Mathf.Max(0.00025f, SoftZDistance) );

                    // generate tile max and neighbour max
                    Graphics.Blit (velBuffer, tileMax, m_motionBlurMaterial, 2);
                    Graphics.Blit (tileMax, neighbourMax, m_motionBlurMaterial, 3);

                    Graphics.Blit (source, destination, m_motionBlurMaterial, 7);
                }
                else {
                    // simple & fast blur (low quality): just blurring along velocity
                    Graphics.Blit (source, destination, m_motionBlurMaterial, 5);
                }
            }

            // cleanup
            RenderTexture.ReleaseTemporary (velBuffer);
            RenderTexture.ReleaseTemporary (tileMax);
            RenderTexture.ReleaseTemporary (neighbourMax);
        }

        void Remember () {
            m_prevViewProjMat = m_currentViewProjMat;
            m_prevFrameForward = transform.forward;
            m_prevFrameUp = transform.up;
            m_prevFramePos = transform.position;
        }

        Camera GetTmpCam () {
            if (m_tmpCam == null) {
                string name = "_" + m_camera.name + "_MotionBlurTmpCam";
                GameObject go = GameObject.Find (name);
                if (null == go) // couldn't find, recreate
                    m_tmpCam = new GameObject (name, typeof (Camera));
                else
                    m_tmpCam = go;
            }

            m_tmpCam.hideFlags = HideFlags.DontSave;
            m_tmpCam.transform.position = m_camera.transform.position;
            m_tmpCam.transform.rotation = m_camera.transform.rotation;
            m_tmpCam.transform.localScale = m_camera.transform.localScale;
            m_tmpCam.GetComponent<Camera>().CopyFrom(m_camera);

            m_tmpCam.GetComponent<Camera>().enabled = false;
            m_tmpCam.GetComponent<Camera>().depthTextureMode = DepthTextureMode.None;
            m_tmpCam.GetComponent<Camera>().clearFlags = CameraClearFlags.Nothing;

            return m_tmpCam.GetComponent<Camera>();
        }

        void StartFrame () {
            // take only x% of positional changes into account (camera motion)
            // TODO: possibly do the same for rotational part
            m_prevFramePos = Vector3.Slerp(m_prevFramePos, transform.position, 0.75f);
        }

        static int DivRoundUp (int x, int d)
        {
            return (x + d - 1) / d;
        }
    }
}
