using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal static class StylizedProperty
    {
        public static readonly string SpecularWorkflowMode = "_WorkflowMode";
        public static readonly string SurfaceType = "_Surface";
        public static readonly string BlendMode = "_Blend";
        public static readonly string AlphaClip = "_AlphaClip";
        public static readonly string AlphaToMask = "_AlphaToMask";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string SrcBlendAlpha = "_SrcBlendAlpha";
        public static readonly string DstBlendAlpha = "_DstBlendAlpha";
        public static readonly string BlendModePreserveSpecular = "_BlendModePreserveSpecular";
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string CullMode = "_Cull";
        public static readonly string CastShadows = "_CastShadows";
        public static readonly string ReceiveShadows = "_ReceiveShadows";
        public static readonly string QueueOffset = "_QueueOffset";

        // for ShaderGraph shaders only
        public static readonly string ZTest = "_ZTest";
        public static readonly string ZWriteControl = "_ZWriteControl";
        public static readonly string QueueControl = "_QueueControl";

        // Global Illumination requires some properties to be named specifically:
        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";
    }

    /// <summary>
    /// Editor script for the Lit material inspector.
    /// </summary>
    internal static class StylizedLitGUI
    {
        /// <summary>
        /// Workflow modes for the shader.
        /// </summary>
        public enum WorkflowMode
        {
            /// <summary>
            /// Use this for specular workflow.
            /// </summary>
            Specular = 0,

            /// <summary>
            /// Use this for metallic workflow.
            /// </summary>
            Metallic
        }

        /// <summary>
        /// Options to select the texture channel where the smoothness value is stored.
        /// </summary>
        public enum SmoothnessMapChannel
        {
            /// <summary>
            /// Use this when smoothness is stored in the alpha channel of the Specular/Metallic Map.
            /// </summary>
            SpecularMetallicAlpha,

            /// <summary>
            /// Use this when smoothness is stored in the alpha channel of the Albedo Map.
            /// </summary>
            AlbedoAlpha,
        }

        /// <summary>
        /// Container for the text and tooltips used to display the shader.
        /// </summary>
        public static class Styles
        {
            /// <summary>
            /// The text and tooltip for the workflow Mode GUI.
            /// </summary>
            public static GUIContent workflowModeText = EditorGUIUtility.TrTextContent("Workflow Mode",
                "Select a workflow that fits your textures. Choose between Metallic or Specular.");

            /// <summary>
            /// Custom stylized GUI
            /// </summary>
            public static GUIContent baseColorStepText =
                EditorGUIUtility.TrTextContent("Color Threshold", "Sets the boundary between the Base Color and the Shadow Color.");
            public static GUIContent baseColorStepFeatherText =
                EditorGUIUtility.TrTextContent("Color Smooth", "Feathers the boundary between the Base Color and the Shadow Color.");
            public static GUIContent specIntensityText =
                EditorGUIUtility.TrTextContent("Specular Intensity", "Sets the intensity of the Specular.");
            public static GUIContent specStepText =
                EditorGUIUtility.TrTextContent("Specular Threshold", "Sets the boundary of the Specular.");
            public static GUIContent specStepFeatherText =
                EditorGUIUtility.TrTextContent("Specular Smooth", "Feathers the boundary of the Specular.");

            /// <summary>
            /// The text and tooltip for the specular Map GUI.
            /// </summary>
            public static GUIContent specularMapText =
                EditorGUIUtility.TrTextContent("Specular Map", "Designates a Specular Map and specular color determining the apperance of reflections on this Material's surface.");

            /// <summary>
            /// The text and tooltip for the metallic Map GUI.
            /// </summary>
            public static GUIContent metallicMapText =
                EditorGUIUtility.TrTextContent("Metallic Map", "Sets and configures the map for the Metallic workflow.");

            /// <summary>
            /// The text and tooltip for the smoothness GUI.
            /// </summary>
            public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness",
                "Controls the spread of highlights and reflections on the surface.");

            /// <summary>
            /// The text and tooltip for the smoothness source GUI.
            /// </summary>
            public static GUIContent smoothnessMapChannelText =
                EditorGUIUtility.TrTextContent("Source",
                    "Specifies where to sample a smoothness map from. By default, uses the alpha channel for your map.");

            /// <summary>
            /// The text and tooltip for the specular Highlights GUI.
            /// </summary>
            public static GUIContent highlightsText = EditorGUIUtility.TrTextContent("Specular Highlights",
                "When enabled, the Material reflects the shine from direct lighting.");

            /// <summary>
            /// The text and tooltip for the environment Reflections GUI.
            /// </summary>
            public static GUIContent reflectionsText =
                EditorGUIUtility.TrTextContent("Environment Reflections",
                    "When enabled, the Material samples reflections from the nearest Reflection Probes or Lighting Probe.");

            /// <summary>
            /// The text and tooltip for the height map GUI.
            /// </summary>
            public static GUIContent heightMapText = EditorGUIUtility.TrTextContent("Height Map",
                "Defines a Height Map that will drive a parallax effect in the shader making the surface seem displaced.");

            /// <summary>
            /// The text and tooltip for the occlusion map GUI.
            /// </summary>
            public static GUIContent occlusionText = EditorGUIUtility.TrTextContent("Occlusion Map",
                "Sets an occlusion map to simulate shadowing from ambient lighting.");

            /// <summary>
            /// The names for smoothness alpha options available for metallic workflow.
            /// </summary>
            public static readonly string[] metallicSmoothnessChannelNames = { "Metallic Alpha", "Albedo Alpha" };

            /// <summary>
            /// The names for smoothness alpha options available for specular workflow.
            /// </summary>
            public static readonly string[] specularSmoothnessChannelNames = { "Specular Alpha", "Albedo Alpha" };

            /// <summary>
            /// The text and tooltip for the enabling/disabling clear coat GUI.
            /// </summary>
            public static GUIContent clearCoatText = EditorGUIUtility.TrTextContent("Clear Coat",
                "A multi-layer material feature which simulates a thin layer of coating on top of the surface material." +
                "\nPerformance cost is considerable as the specular component is evaluated twice, once per layer.");

            /// <summary>
            /// The text and tooltip for the clear coat Mask GUI.
            /// </summary>
            public static GUIContent clearCoatMaskText = EditorGUIUtility.TrTextContent("Mask",
                "Specifies the amount of the coat blending." +
                "\nActs as a multiplier of the clear coat map mask value or as a direct mask value if no map is specified." +
                "\nThe map specifies clear coat mask in the red channel and clear coat smoothness in the green channel.");

            /// <summary>
            /// The text and tooltip for the clear coat smoothness GUI.
            /// </summary>
            public static GUIContent clearCoatSmoothnessText = EditorGUIUtility.TrTextContent("Smoothness",
                "Specifies the smoothness of the coating." +
                "\nActs as a multiplier of the clear coat map smoothness value or as a direct smoothness value if no map is specified.");
        }

        /// <summary>
        /// Container for the properties used in the <c>LitGUI</c> editor script.
        /// </summary>
        public struct LitProperties
        {
            // Surface Option Props

            /// <summary>
            /// The MaterialProperty for workflow mode.
            /// </summary>
            public MaterialProperty workflowMode;


            // Surface Input Props

            /// <summary>
            /// Custom Stylized Properties
            /// </summary>
            public MaterialProperty baseColorStep;
            public MaterialProperty baseColorStepFeather;
            public MaterialProperty specIntensity;
            public MaterialProperty specStep;
            public MaterialProperty specStepFeather;

            /// <summary>
            /// The MaterialProperty for metallic value.
            /// </summary>
            public MaterialProperty metallic;

            /// <summary>
            /// The MaterialProperty for specular color.
            /// </summary>
            public MaterialProperty specColor;

            /// <summary>
            /// The MaterialProperty for metallic Smoothness map.
            /// </summary>
            public MaterialProperty metallicGlossMap;

            /// <summary>
            /// The MaterialProperty for specular smoothness map.
            /// </summary>
            public MaterialProperty specGlossMap;

            /// <summary>
            /// The MaterialProperty for smoothness value.
            /// </summary>
            public MaterialProperty smoothness;

            /// <summary>
            /// The MaterialProperty for smoothness alpha channel.
            /// </summary>
            public MaterialProperty smoothnessMapChannel;

            /// <summary>
            /// The MaterialProperty for normal map.
            /// </summary>
            public MaterialProperty bumpMapProp;

            /// <summary>
            /// The MaterialProperty for normal map scale.
            /// </summary>
            public MaterialProperty bumpScaleProp;

            /// <summary>
            /// The MaterialProperty for height map.
            /// </summary>
            public MaterialProperty parallaxMapProp;

            /// <summary>
            /// The MaterialProperty for height map scale.
            /// </summary>
            public MaterialProperty parallaxScaleProp;

            /// <summary>
            /// The MaterialProperty for occlusion strength.
            /// </summary>
            public MaterialProperty occlusionStrength;

            /// <summary>
            /// The MaterialProperty for occlusion map.
            /// </summary>
            public MaterialProperty occlusionMap;


            // Advanced Props

            /// <summary>
            /// The MaterialProperty for specular highlights.
            /// </summary>
            public MaterialProperty highlights;

            /// <summary>
            /// The MaterialProperty for environment reflections.
            /// </summary>
            public MaterialProperty reflections;

            /// <summary>
            /// The MaterialProperty for enabling/disabling clear coat.
            /// </summary>
            public MaterialProperty clearCoat;  // Enable/Disable dummy property

            /// <summary>
            /// The MaterialProperty for clear coat map.
            /// </summary>
            public MaterialProperty clearCoatMap;

            /// <summary>
            /// The MaterialProperty for clear coat mask.
            /// </summary>
            public MaterialProperty clearCoatMask;

            /// <summary>
            /// The MaterialProperty for clear coat smoothness.
            /// </summary>
            public MaterialProperty clearCoatSmoothness;

            /// <summary>
            /// Constructor for the <c>LitProperties</c> container struct.
            /// </summary>
            /// <param name="properties"></param>
            public LitProperties(MaterialProperty[] properties)
            {
                // Surface Option Props
                workflowMode = BaseShaderGUI.FindProperty("_WorkflowMode", properties, false);
                // Surface Input Props
                baseColorStep = BaseShaderGUI.FindProperty("_BaseColorStep", properties);
                baseColorStepFeather = BaseShaderGUI.FindProperty("_BaseColorStepFeather", properties);
                specIntensity = BaseShaderGUI.FindProperty("_SpecIntensity", properties);
                specStep = BaseShaderGUI.FindProperty("_SpecStep", properties);
                specStepFeather = BaseShaderGUI.FindProperty("_SpecStepFeather", properties);

                metallic = BaseShaderGUI.FindProperty("_Metallic", properties);
                specColor = BaseShaderGUI.FindProperty("_SpecColor", properties, false);
                metallicGlossMap = BaseShaderGUI.FindProperty("_MetallicGlossMap", properties);
                specGlossMap = BaseShaderGUI.FindProperty("_SpecGlossMap", properties, false);
                smoothness = BaseShaderGUI.FindProperty("_Smoothness", properties, false);
                smoothnessMapChannel = BaseShaderGUI.FindProperty("_SmoothnessTextureChannel", properties, false);
                bumpMapProp = BaseShaderGUI.FindProperty("_BumpMap", properties, false);
                bumpScaleProp = BaseShaderGUI.FindProperty("_BumpScale", properties, false);
                parallaxMapProp = BaseShaderGUI.FindProperty("_ParallaxMap", properties, false);
                parallaxScaleProp = BaseShaderGUI.FindProperty("_Parallax", properties, false);
                occlusionStrength = BaseShaderGUI.FindProperty("_OcclusionStrength", properties, false);
                occlusionMap = BaseShaderGUI.FindProperty("_OcclusionMap", properties, false);
                // Advanced Props
                highlights = BaseShaderGUI.FindProperty("_SpecularHighlights", properties, false);
                reflections = BaseShaderGUI.FindProperty("_EnvironmentReflections", properties, false);

                clearCoat = BaseShaderGUI.FindProperty("_ClearCoat", properties, false);
                clearCoatMap = BaseShaderGUI.FindProperty("_ClearCoatMap", properties, false);
                clearCoatMask = BaseShaderGUI.FindProperty("_ClearCoatMask", properties, false);
                clearCoatSmoothness = BaseShaderGUI.FindProperty("_ClearCoatSmoothness", properties, false);
            }
        }

        /// <summary>
        /// Draws the surface inputs GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        public static void Inputs(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            DoStylizedArea(properties, materialEditor, material);
            DoMetallicSpecularArea(properties, materialEditor, material);
            BaseShaderGUI.DrawNormalArea(materialEditor, properties.bumpMapProp, properties.bumpScaleProp);

            if (HeightmapAvailable(material))
                DoHeightmapArea(properties, materialEditor);

            if (properties.occlusionMap != null)
            {
                materialEditor.TexturePropertySingleLine(Styles.occlusionText, properties.occlusionMap,
                    properties.occlusionMap.textureValue != null ? properties.occlusionStrength : null);
            }

            // Check that we have all the required properties for clear coat,
            // otherwise we will get null ref exception from MaterialEditor GUI helpers.
            if (ClearCoatAvailable(material))
                DoClearCoat(properties, materialEditor, material);
        }

        private static bool ClearCoatAvailable(Material material)
        {
            return material.HasProperty("_ClearCoat")
                && material.HasProperty("_ClearCoatMap")
                && material.HasProperty("_ClearCoatMask")
                && material.HasProperty("_ClearCoatSmoothness");
        }

        private static bool HeightmapAvailable(Material material)
        {
            return material.HasProperty("_Parallax")
                && material.HasProperty("_ParallaxMap");
        }

        private static void DoHeightmapArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(Styles.heightMapText, properties.parallaxMapProp,
                properties.parallaxMapProp.textureValue != null ? properties.parallaxScaleProp : null);
        }

        private static bool ClearCoatEnabled(Material material)
        {
            return material.HasProperty("_ClearCoat") && material.GetFloat("_ClearCoat") > 0.0;
        }

        /// <summary>
        /// Draws the clear coat GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        public static void DoClearCoat(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.ShaderProperty(properties.clearCoat, Styles.clearCoatText);
            var coatEnabled = material.GetFloat("_ClearCoat") > 0.0;

            EditorGUI.BeginDisabledGroup(!coatEnabled);
            {
                EditorGUI.indentLevel += 2;
                materialEditor.TexturePropertySingleLine(Styles.clearCoatMaskText, properties.clearCoatMap, properties.clearCoatMask);

                // Texture and HDR color controls
                materialEditor.ShaderProperty(properties.clearCoatSmoothness, Styles.clearCoatSmoothnessText);

                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Draws the custom stylized area GUI.
        /// </summary>
        public static void DoStylizedArea(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            materialEditor.ShaderProperty(properties.baseColorStep, Styles.baseColorStepText);
            materialEditor.ShaderProperty(properties.baseColorStepFeather, Styles.baseColorStepFeatherText);
            materialEditor.ShaderProperty(properties.specIntensity, Styles.specIntensityText);
            materialEditor.ShaderProperty(properties.specStep, Styles.specStepText);
            materialEditor.ShaderProperty(properties.specStepFeather, Styles.specStepFeatherText);
        }

        /// <summary>
        /// Draws the metallic/specular area GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        public static void DoMetallicSpecularArea(LitProperties properties, MaterialEditor materialEditor, Material material)
        {
            string[] smoothnessChannelNames;
            bool hasGlossMap = false;
            if (properties.workflowMode == null ||
                (WorkflowMode)properties.workflowMode.floatValue == WorkflowMode.Metallic)
            {
                hasGlossMap = properties.metallicGlossMap.textureValue != null;
                smoothnessChannelNames = Styles.metallicSmoothnessChannelNames;
                materialEditor.TexturePropertySingleLine(Styles.metallicMapText, properties.metallicGlossMap,
                    hasGlossMap ? null : properties.metallic);
            }
            else
            {
                hasGlossMap = properties.specGlossMap.textureValue != null;
                smoothnessChannelNames = Styles.specularSmoothnessChannelNames;
                BaseShaderGUI.TextureColorProps(materialEditor, Styles.specularMapText, properties.specGlossMap,
                    hasGlossMap ? null : properties.specColor);
            }
            DoSmoothness(materialEditor, material, properties.smoothness, properties.smoothnessMapChannel, smoothnessChannelNames);
        }

        internal static bool IsOpaque(Material material)
        {
            bool opaque = true;
            if (material.HasProperty(StylizedProperty.SurfaceType))
                opaque = ((BaseShaderGUI.SurfaceType)material.GetFloat(StylizedProperty.SurfaceType) == BaseShaderGUI.SurfaceType.Opaque);
            return opaque;
        }

        /// <summary>
        /// Draws the smoothness GUI.
        /// </summary>
        /// <param name="materialEditor"></param>
        /// <param name="material"></param>
        /// <param name="smoothness"></param>
        /// <param name="smoothnessMapChannel"></param>
        /// <param name="smoothnessChannelNames"></param>
        public static void DoSmoothness(MaterialEditor materialEditor, Material material, MaterialProperty smoothness, MaterialProperty smoothnessMapChannel, string[] smoothnessChannelNames)
        {
            EditorGUI.indentLevel += 2;

            materialEditor.ShaderProperty(smoothness, Styles.smoothnessText);

            if (smoothnessMapChannel != null) // smoothness channel
            {
                var opaque = IsOpaque(material);
                EditorGUI.indentLevel++;
                EditorGUI.showMixedValue = smoothnessMapChannel.hasMixedValue;
                if (opaque)
                {
                    MaterialEditor.BeginProperty(smoothnessMapChannel);
                    EditorGUI.BeginChangeCheck();
                    var smoothnessSource = (int)smoothnessMapChannel.floatValue;
                    smoothnessSource = EditorGUILayout.Popup(Styles.smoothnessMapChannelText, smoothnessSource, smoothnessChannelNames);
                    if (EditorGUI.EndChangeCheck())
                        smoothnessMapChannel.floatValue = smoothnessSource;
                    MaterialEditor.EndProperty();
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Popup(Styles.smoothnessMapChannelText, 0, smoothnessChannelNames);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.showMixedValue = false;
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel -= 2;
        }

        /// <summary>
        /// Retrieves the alpha channel used for smoothness.
        /// </summary>
        /// <param name="material"></param>
        /// <returns>The Alpha channel used for Smoothness.</returns>
        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;

            return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        // (shared by all lit shaders, including shadergraph Lit Target and Lit.shader)
        internal static void SetupSpecularWorkflowKeyword(Material material, out bool isSpecularWorkflow)
        {
            isSpecularWorkflow = false;     // default is metallic workflow
            if (material.HasProperty(StylizedProperty.SpecularWorkflowMode))
                isSpecularWorkflow = ((WorkflowMode)material.GetFloat(StylizedProperty.SpecularWorkflowMode)) == WorkflowMode.Specular;
            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", isSpecularWorkflow);
        }


        /// <summary>
        /// Sets up the keywords for the Lit shader and material.
        /// </summary>
        /// <param name="material"></param>
        public static void SetMaterialKeywords(Material material)
        {
            SetupSpecularWorkflowKeyword(material, out bool isSpecularWorkFlow);

            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            var specularGlossMap = isSpecularWorkFlow ? "_SpecGlossMap" : "_MetallicGlossMap";
            var hasGlossMap = material.GetTexture(specularGlossMap) != null;

            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", hasGlossMap);

            if (material.HasProperty("_SpecularHighlights"))
                CoreUtils.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF",
                    material.GetFloat("_SpecularHighlights") == 0.0f);
            if (material.HasProperty("_EnvironmentReflections"))
                CoreUtils.SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF",
                    material.GetFloat("_EnvironmentReflections") == 0.0f);
            if (material.HasProperty("_OcclusionMap"))
                CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));

            if (material.HasProperty("_ParallaxMap"))
                CoreUtils.SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                var opaque = IsOpaque(material);
                CoreUtils.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                    GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha && opaque);
            }

            // Clear coat keywords are independent to remove possiblity of invalid combinations.
            if (ClearCoatEnabled(material))
            {
                var hasMap = material.HasProperty("_ClearCoatMap") && material.GetTexture("_ClearCoatMap") != null;
                if (hasMap)
                {
                    CoreUtils.SetKeyword(material, "_CLEARCOAT", false);
                    CoreUtils.SetKeyword(material, "_CLEARCOATMAP", true);
                }
                else
                {
                    CoreUtils.SetKeyword(material, "_CLEARCOAT", true);
                    CoreUtils.SetKeyword(material, "_CLEARCOATMAP", false);
                }
            }
            else
            {
                CoreUtils.SetKeyword(material, "_CLEARCOAT", false);
                CoreUtils.SetKeyword(material, "_CLEARCOATMAP", false);
            }
        }
    }

    internal class StylizedLitDetailGUI
    {
        internal static class Styles
        {
            public static readonly GUIContent detailInputs = EditorGUIUtility.TrTextContent("Detail Inputs",
                "These settings define the surface details by tiling and overlaying additional maps on the surface.");

            public static readonly GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Mask",
                "Select a mask for the Detail map. The mask uses the alpha channel of the selected texture. The Tiling and Offset settings have no effect on the mask.");

            public static readonly GUIContent detailAlbedoMapText = EditorGUIUtility.TrTextContent("Base Map",
                "Select the surface detail texture.The alpha of your texture determines surface hue and intensity.");

            public static readonly GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map",
                "Designates a Normal Map to create the illusion of bumps and dents in the details of this Material's surface.");

            public static readonly GUIContent detailAlbedoMapScaleInfo = EditorGUIUtility.TrTextContent("Setting the scaling factor to a value other than 1 results in a less performant shader variant.");
            public static readonly GUIContent detailAlbedoMapFormatError = EditorGUIUtility.TrTextContent("This texture is not in linear space.");
        }

        public struct LitProperties
        {
            public MaterialProperty detailMask;
            public MaterialProperty detailAlbedoMapScale;
            public MaterialProperty detailAlbedoMap;
            public MaterialProperty detailNormalMapScale;
            public MaterialProperty detailNormalMap;

            public LitProperties(MaterialProperty[] properties)
            {
                detailMask = BaseShaderGUI.FindProperty("_DetailMask", properties, false);
                detailAlbedoMapScale = BaseShaderGUI.FindProperty("_DetailAlbedoMapScale", properties, false);
                detailAlbedoMap = BaseShaderGUI.FindProperty("_DetailAlbedoMap", properties, false);
                detailNormalMapScale = BaseShaderGUI.FindProperty("_DetailNormalMapScale", properties, false);
                detailNormalMap = BaseShaderGUI.FindProperty("_DetailNormalMap", properties, false);
            }
        }

        public static void DoDetailArea(LitProperties properties, MaterialEditor materialEditor)
        {
            materialEditor.TexturePropertySingleLine(Styles.detailMaskText, properties.detailMask);
            materialEditor.TexturePropertySingleLine(Styles.detailAlbedoMapText, properties.detailAlbedoMap,
                properties.detailAlbedoMap.textureValue != null ? properties.detailAlbedoMapScale : null);
            if (properties.detailAlbedoMapScale.floatValue != 1.0f)
            {
                EditorGUILayout.HelpBox(Styles.detailAlbedoMapScaleInfo.text, MessageType.Info, true);
            }
            var detailAlbedoTexture = properties.detailAlbedoMap.textureValue as Texture2D;
            if (detailAlbedoTexture != null && GraphicsFormatUtility.IsSRGBFormat(detailAlbedoTexture.graphicsFormat))
            {
                EditorGUILayout.HelpBox(Styles.detailAlbedoMapFormatError.text, MessageType.Warning, true);
            }
            materialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, properties.detailNormalMap,
                properties.detailNormalMap.textureValue != null ? properties.detailNormalMapScale : null);
            materialEditor.TextureScaleOffsetProperty(properties.detailAlbedoMap);
        }

        public static void SetMaterialKeywords(Material material)
        {
            if (material.HasProperty("_DetailAlbedoMap") && material.HasProperty("_DetailNormalMap") && material.HasProperty("_DetailAlbedoMapScale"))
            {
                bool isScaled = material.GetFloat("_DetailAlbedoMapScale") != 1.0f;
                bool hasDetailMap = material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap");
                CoreUtils.SetKeyword(material, "_DETAIL_MULX2", !isScaled && hasDetailMap);
                CoreUtils.SetKeyword(material, "_DETAIL_SCALED", isScaled && hasDetailMap);
            }
        }
    }
}
