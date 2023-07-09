using UnityEngine;

namespace ToonGraphics
{
    public enum CustomShadowMapSize
    {
        X1 = 1,
        X2 = 2,
        X4 = 4
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
        public bool enableTransparentShadow = false;
        public bool enableAdditionalShadow = false;
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
