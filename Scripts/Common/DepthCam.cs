using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.IO;


namespace TankDemoML {
    /// <summary>
    /// Component to process image segmentation on a camera, and output to a rendertexture
    /// </summary>
    [RequireComponent (typeof(Camera))]
    public class DepthCam : MonoBehaviour
    {
        public Camera sourceCamera;
        public RenderTexture output;
        public Camera depthCam;
        public Shader uberReplacementShader;


        void Start()
        {
            depthCam.hideFlags = HideFlags.HideAndDontSave;

            OnCameraChange();
            OnSceneChange();
        }

        void LateUpdate()
        {
            OnSceneChange();
            OnCameraChange();
        }

        static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode)
        {
            SetupCameraWithReplacementShader(cam, shader, mode, Color.black);
        }

        static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode, Color clearColor)
        {
            var cb = new CommandBuffer();
            cb.SetGlobalFloat("_OutputMode", (int)mode); // @TODO: CommandBuffer is missing SetGlobalInt() method
            cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
            cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
            cam.SetReplacementShader(shader, "");
            cam.backgroundColor = clearColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        enum ReplacelementModes {
            ObjectId 			= 0,
            CatergoryId			= 1,
            DepthCompressed		= 2,
            DepthMultichannel	= 3,
            Normals				= 4
        };

        public void OnCameraChange()
        {
            depthCam.RemoveAllCommandBuffers();
            depthCam.CopyFrom(sourceCamera);
            depthCam.targetTexture = output;
            SetupCameraWithReplacementShader(depthCam, uberReplacementShader, ReplacelementModes.DepthCompressed, Color.white);
        }

        public void OnSceneChange()
        {
            var renderers = Object.FindObjectsOfType<Renderer>();
            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                var id = r.gameObject.GetInstanceID();
                var layer = r.gameObject.layer;
                var tag = r.gameObject.tag;

                mpb.SetColor("_ObjectColor", ColorEncoding.EncodeIDAsColor(id));
                mpb.SetColor("_CategoryColor", ColorEncoding.EncodeLayerAsColor(layer));
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
