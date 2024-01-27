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
        [Tooltip("[Forward+ Only] If enabled, use the stronger light among MainLight & the brighest spot light.")]
        public bool useBrightestLight = true;
        public LayerMask followLayerMask;
        public float bias = 0.001f;
        public float normalBias = 0.001f;
        public float stepOffset = 0.99f;    // Currently unused
        public CustomShadowMapSize textureScale = CustomShadowMapSize.X4;
        public CustomShadowMapSize transparentTextureScale = CustomShadowMapSize.X2;
        public CustomShadowMapPrecision precision = CustomShadowMapPrecision.RFloat;
        public CharSoftShadowMode softShadowMode = CharSoftShadowMode.Normal;
        public Vector4 cascadeSplit = new Vector4(3.5f, 7f, 11f, 20f);
    }
}
