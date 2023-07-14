using System.Runtime.InteropServices.ComTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Note:
// We assume that there's only 1 camera except for render texture cameras in a scene.
namespace ToonGraphics
{
    [ExecuteAlways]
    public class CharShadowCamera : MonoBehaviour
    {
        public static CharShadowCamera Instance => s_Instance;
        private static CharShadowCamera s_Instance;

        public Transform target;    // Character Transform
        public float charBoundOffset = 1;
        public float charHalfHeight = 0.75f; // Character half height
        public float cameraDistance = 4f;
        public Camera[] lightCameras = new Camera[4];

        private Light _mainLight;
        private Light[] _spotLights = new Light[3];
        private UnityEngine.Quaternion _originQuaternion;
        private List<Light> _sceneLights;

        private void OnValidate()
        {
        }

        void Awake()
        {
            s_Instance = this;
            _sceneLights = new List<Light>(256);
        }

        // Start is called before the first frame update
        void Start()
        {
            RefreshSceneLights();
        }

        void LateUpdate()
        {
            RefreshLightCameraTransforms();
        }

        ///<summary>
        /// NOTE) Make sure run this API in external script when dynamic lights are spawned at runtime. But should be care before due to performance.
        ///</summary>
        public void RefreshSceneLights()
        {
            _sceneLights.Clear();
            foreach (var light in GameObject.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.gameObject.isStatic)
                    continue;

                // Set Main Light as the first found directional light.
                if (light.type == LightType.Directional && light.isActiveAndEnabled)
                {
                    if (_mainLight == null)
                        _mainLight = light;
                }
                
                if (light.type == LightType.Spot)
                {
                    _sceneLights.Add(light);
                }
            }
        }

        public void RefreshLightCameraTransforms()
        {
            // Update Main Light Camera Transform
            SetLightCameraTransform(lightCameras[0], _mainLight);
        }

        public void SetLightCameraTransform(Camera lightCamera, Light light)
        {
            if (lightCamera == null || light == null)
                return;

            lightCamera.transform.rotation = light.transform.rotation;
            var dir = light.transform.rotation * Vector3.forward;
            lightCamera.transform.position = target.position + (Vector3.up * charHalfHeight) + (-dir * cameraDistance);
        }

        public void SetLightCameraTransform(int camIndex, Light light)
        {
            lightCameras[camIndex].transform.rotation = light.transform.rotation;
            var dir = light.transform.rotation * Vector3.forward;
            lightCameras[camIndex].transform.position = target.position + (Vector3.up * charHalfHeight) + (-dir * cameraDistance);
        }
    }
}
