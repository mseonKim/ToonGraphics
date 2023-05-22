using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;

namespace ToonGraphics
{
    public static class CharacterShadowUtils
    {
        private static List<VisibleLight> s_vSpotLights = new List<VisibleLight>(256);
        private static List<int> s_vSpotLightIndices = new List<int>(256);
        private static List<KeyValuePair<float, int>> s_SortedSpotLights = new List<KeyValuePair<float, int>>(256);
        private static int s_CharShadowLocalLightIndices = Shader.PropertyToID("_CharShadowLocalLightIndices");

        public static void SetShadowmapLightData(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var spotLightIndices = CalculateMostIntensiveLightIndices(ref renderingData);
            if (spotLightIndices == null)
            {
                return;
            }

            var visibleLights = renderingData.lightData.visibleLights;
            var lightCount = renderingData.lightData.visibleLights.Length;
            var lightOffset = 0;
            while (lightOffset < lightCount && visibleLights[lightOffset].lightType == LightType.Directional)
            {
                lightOffset++;
            }
            var hasMainLight = 0;
            // Update Main Light Camera transform
            if (renderingData.lightData.mainLightIndex != -1 && lightOffset != 0)
            {
                CharShadowCamera.Instance.SetLightCameraTransform(0, visibleLights[renderingData.lightData.mainLightIndex].light);
                hasMainLight = 1;
            }

            if (spotLightIndices.Count == 0)
                return;

            var localLightIndices = new float[3] { -1, -1, -1 };
            lightCount = (int)Mathf.Min(spotLightIndices.Count, 3);
            for (int i = 0; i < lightCount; i++)
            {
                // Update local light camera transform
                int lightIndex = spotLightIndices[i] + hasMainLight;
                CharShadowCamera.Instance.SetLightCameraTransform(i + 1, visibleLights[lightIndex].light);
                // Update local light index table
                localLightIndices[i] = spotLightIndices[i];
            }

            // Send to GPU
            cmd.SetGlobalFloatArray(s_CharShadowLocalLightIndices, localLightIndices);
        }

        private static List<int> CalculateMostIntensiveLightIndices(ref RenderingData renderingData)
        {
            if (CharShadowCamera.Instance == null)
            {
                return null;
            }

            var lightCount = renderingData.lightData.visibleLights.Length;
            var lightOffset = 0;
            while (lightOffset < lightCount && renderingData.lightData.visibleLights[lightOffset].lightType == LightType.Directional)
            {
                lightOffset++;
            }
            lightCount -= lightOffset;
            var directionalLightCount = lightOffset;
            if (renderingData.lightData.mainLightIndex != -1 && directionalLightCount != 0) directionalLightCount -= 1;
            var visibleLights = renderingData.lightData.visibleLights.GetSubArray(lightOffset, lightCount);

            s_vSpotLights.Clear();
            s_vSpotLightIndices.Clear();
            s_SortedSpotLights.Clear();
            // Extract spot lights
            for (int i = 0; i < visibleLights.Length; i++)
            {
                if (visibleLights[i].lightType == LightType.Spot)
                {
                    s_vSpotLightIndices.Add(i + directionalLightCount);
                    s_vSpotLights.Add(visibleLights[i]);
                }
            }

            // Calculate light intensity
            var target = CharShadowCamera.Instance.target.transform;
            for (int i = 0; i < s_vSpotLights.Count; i++)
            {
                var light = s_vSpotLights[i].light;
                var diff = target.position - light.transform.position;
                var dirToTarget = Vector3.Normalize(diff);
                var L = light.transform.rotation * Vector3.forward;
                var dotL = Vector3.Dot(dirToTarget, L);
                var distance = diff.magnitude;
                if (dotL <= Mathf.Cos(light.spotAngle) || distance > light.range)
                {
                    continue;
                }

                var finalColor = s_vSpotLights[i].finalColor;
                var atten = 1f - distance / light.range;
                var strength = (finalColor.r * 0.229f + finalColor.g * 0.587f + finalColor.b * 0.114f) * atten;
                s_SortedSpotLights.Add(new KeyValuePair<float, int>(strength, s_vSpotLightIndices[i]));
            }
            // Sort
            s_SortedSpotLights.Sort((x, y) => y.Key.CompareTo(x.Key));

            var charSpotLightIndices = new List<int>(s_SortedSpotLights.Count);
            for (int i = 0; i < charSpotLightIndices.Capacity; i++)
            {
                charSpotLightIndices.Add(s_SortedSpotLights[i].Value);
            }
            return charSpotLightIndices;
        }
    }
}