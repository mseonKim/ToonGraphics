using UnityEngine;

namespace ToonGraphics
{
    public enum CustomShadowMapSize
    {
        X1 = 1,
        X2 = 2,
        X4 = 4,
        X8 = 8,
    }

    public enum CustomShadowMapPrecision
    {
        RFloat = 14,
        RHalf = 15,
    }

    public enum CharSoftShadowMode
    {
        Normal,
        High,
    }

    [CreateAssetMenu(menuName = "ToonGraphics/CharacterShadowConfig")]
    public class CharacterShadowConfig : ScriptableObject
    {
        [Tooltip("If enabled, Transparent shadowmap will be rendered.")]
        public bool enableTransparentShadow = false;
        [Tooltip("If enabled, use 4 Shadowmaps. (MainLight & 3 additional spot lights)")]
        public bool enableAdditionalShadow = false;
        [Tooltip("If enabled, use the stronger light among MainLight & the brighest spot light for the character shadow map. It will ignore 3 other additional shadows even though 'enableAdditionalShadow' is enabled.")]
        public bool useBrightestLightOnly = false;
        public float bias;
        public float normalBias;
        public float additionalBias;
        public float additionalNormalBias;
        public float stepOffset = 0.99f;
        public float additionalStepOffset = 0.99f;
        public CustomShadowMapSize textureScale = CustomShadowMapSize.X2;
        public CustomShadowMapSize transparentTextureScale = CustomShadowMapSize.X2;
        public CustomShadowMapPrecision precision = CustomShadowMapPrecision.RFloat;
        public CharSoftShadowMode softShadowMode = CharSoftShadowMode.Normal;
        public Vector4 cascadeSplit = new Vector4(2, 6, 14, 20);
    }
}
