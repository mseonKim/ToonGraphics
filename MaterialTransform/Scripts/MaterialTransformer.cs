using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonGraphics.MaterialTransformer
{
    public enum MaterialTransformPivot
    {
        VerticalTop,
        VerticalBottom,
        VerticalCenter,
        HorizontalLeft,
        HorizontalRight,
        HorizontalCenter,
        Front,
        Back,
        Center
    }

    [System.Serializable]
    public struct MaterialTransformData
    {
        public Material material;
        public int channel;
        public MaterialTransformPivot pivot;
        
        private readonly static Vector4[] s_PivotToVectorTable = new Vector4[9] {
            new Vector4(0, 1, 0, 0),  new Vector4(0, 1, 0, 1), new Vector4(0, 1, 0, 0.5f),
            new Vector4(1, 0, 0, 0), new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 0.5f),
            new Vector4(0, 0, 1, 0), new Vector4(0, 0, 1, 1), new Vector4(0, 0, 1, 0.5f)
        };
        public Vector4 pivotVector => s_PivotToVectorTable[(int)pivot]; // xyz: using axis, w: pivot value
    }

    [ExecuteAlways]
    public class MaterialTransformer : MonoBehaviour
    {
        private static class ShaderID
        {
            public readonly static int _TransformerMaskPivot = Shader.PropertyToID("_TransformerMaskPivot");
            public readonly static int _TransformerMaskChannel = Shader.PropertyToID("_TransformerMaskChannel");
            public readonly static int _MeshTransformScale = Shader.PropertyToID("_MeshTransformScale");
            public readonly static int _MeshTransformOffset = Shader.PropertyToID("_MeshTransformOffset");
            public readonly static int _UseTransformerMask = Shader.PropertyToID("_UseTransformerMask");
            public readonly static int _gTransformerMasks = Shader.PropertyToID("_TransformerMasks");
            public readonly static int _gTransformerColor = Shader.PropertyToID("_TransformerColor");
            public readonly static int _gTransformerDissolveTex = Shader.PropertyToID("_TransformerDissolveTex");
            public readonly static int _gInvTransformerDissolveWidth = Shader.PropertyToID("_InvTransformerDissolveWidth");
        }

        [Header("Settings")]
        public bool enable = true;
        public Vector3 meshTransformScale = Vector3.one;
        public Vector3 meshTransformOffset = Vector3.zero;
        public GameObject targetA;
        public GameObject targetB;
        public bool targetAEnabled = true;
        public bool targetBEnabled = true;
        [ColorUsage(true, true)] public Color meshTransformColor = Color.white;
        public Texture2D noiseTex;
        [Range(0, 0.05f)] public float dissolveWidth = 0.02f;

        [Header("Channels")]
        [Range(0, 1)] public float channel0;
        [Range(0, 1)] public float channel1;
        [Range(0, 1)] public float channel2;
        [Range(0, 1)] public float channel3;
        [Range(0, 1)] public float channel4;
        [Range(0, 1)] public float channel5;
        [Range(0, 1)] public float channel6;
        [Range(0, 1)] public float channel7;
        private float[] _channels = new float[8];
        
        [Header("Material List")]
        [SerializeField]
        public List<MaterialTransformData> materialTransformLists = new List<MaterialTransformData>();

        void OnEnable()
        {
            UpdateMaterialData();
        }

        void Update()
        {
            if (targetA != null)
                targetA.SetActive(targetAEnabled);
            if (targetB != null)
                targetB.SetActive(targetBEnabled);
            UpdateMaterialTransformer();
        }

        private void UpdateMaterialTransformer()
        {
            if (enable)
            {
                Shader.EnableKeyword("_MATERIAL_TRANSFORM");
                UpdateGlobalData();
                UpdateMaterialData();
            }
            else
            {
                Shader.DisableKeyword("_MATERIAL_TRANSFORM");
            }
        }

        private void UpdateGlobalData()
        {
            _channels[0] = channel0;
            _channels[1] = channel1;
            _channels[2] = channel2;
            _channels[3] = channel3;
            _channels[4] = channel4;
            _channels[5] = channel5;
            _channels[6] = channel6;
            _channels[7] = channel7;
            Shader.SetGlobalFloatArray(ShaderID._gTransformerMasks, _channels);
            Shader.SetGlobalColor(ShaderID._gTransformerColor, meshTransformColor);
            Shader.SetGlobalTexture(ShaderID._gTransformerDissolveTex, noiseTex);
            Shader.SetGlobalFloat(ShaderID._gInvTransformerDissolveWidth, 1.0f / Mathf.Max(dissolveWidth, 0.0001f));
        }

        private void UpdateMaterialData()
        {
            foreach (var transformData in materialTransformLists)
            {
                var targetMaterial = transformData.material;
                if (targetMaterial != null)
                {
                    targetMaterial.SetVector(ShaderID._TransformerMaskPivot, transformData.pivotVector);
                    targetMaterial.SetVector(ShaderID._MeshTransformScale, meshTransformScale);
                    targetMaterial.SetVector(ShaderID._MeshTransformOffset, meshTransformOffset);
                    targetMaterial.SetInt(ShaderID._TransformerMaskChannel, transformData.channel);
                    targetMaterial.SetInt(ShaderID._UseTransformerMask, 1);
                }
            }
        }
    }
}
