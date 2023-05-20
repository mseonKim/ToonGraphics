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
        public static Camera[] lightCameras = new Camera[4];  // 0 : Main, 1~3 : Spot

        public Transform target;    // Character Transform
        public float charBoundOffset = 1;
        public float charHalfHeight = 0.75f; // Character half height
        public float cameraDistance = 4f;

        [SerializeField]
        private Camera[] m_LightCameras = new Camera[4];

        private Light _mainLight;
        private Light[] _spotLights = new Light[3];
        private UnityEngine.Quaternion _originQuaternion;
        private List<Light> _sceneLights;

        private void OnValidate()
        {
        }

        void Awake()
        {
            _sceneLights = new List<Light>(256);
            SetLightCameras();
        }

        // Start is called before the first frame update
        void Start()
        {
            RefreshSceneLights();
        }

        void LateUpdate()
        {
            CalculateMostIntensiveLights(_spotLights);
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

        private void SetLightCameras()
        {
            for (int i = 0; i < 4; i ++)
                lightCameras[i] = m_LightCameras[i];
        }

        private void RefreshLightCameraTransforms()
        {
            // Update Main Light Camera Transform
            SetLightCameraTransform(lightCameras[0], _mainLight);

            // Update Additional Light Camera Transforms
            for (int i = 0; i < 3; i++)
            {
                SetLightCameraTransform(lightCameras[i + 1], _spotLights[i]);
            }
        }

        private void SetLightCameraTransform(Camera lightCamera, Light light)
        {
            if (lightCamera == null || light == null)
                return;

            lightCamera.transform.rotation = light.transform.rotation;
            var dir = light.transform.rotation * Vector3.forward;
            lightCamera.transform.position = target.position + (Vector3.up * charHalfHeight) + (-dir * cameraDistance);
        }


        private void CalculateMostIntensiveLights(Light[] outLights)
        {
            int length = outLights.Length;
            int count = 0;

            for (int i = 0; i < length; i++)
            {
                outLights[i] = null;
            }

            foreach (var light in _sceneLights)
            {
                if (!light.isActiveAndEnabled)
                    continue;

                // TODO:
                if (light.type == LightType.Spot)
                {
                    outLights[count++] = light;
                    if (count >= length)
                        break;
                }
            }
        }
    }
}
