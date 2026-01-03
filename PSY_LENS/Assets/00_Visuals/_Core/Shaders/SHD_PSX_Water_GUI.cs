using UnityEngine;
using UnityEditor;

public class SHD_PSX_Water_GUI : ShaderGUI
{
    private bool showShapeSettings   = true;
    private bool showGlobalSettings  = true;
    private bool showSpecSettings    = true;
    private bool showSparkleSettings = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        // Base
        MaterialProperty mainTex = FindProperty("_MainTex", props, false);
        MaterialProperty color   = FindProperty("_Color",   props, false);
        MaterialProperty alpha   = FindProperty("_Alpha",   props, false);

        // Mode
        MaterialProperty waveModeProp = FindProperty("_WaveMode", props, false);

        // Shape
        MaterialProperty ampMin    = FindProperty("_AmplitudeMin", props, false);
        MaterialProperty ampMax    = FindProperty("_AmplitudeMax", props, false);
        MaterialProperty freqMin   = FindProperty("_FrequencyMin", props, false);
        MaterialProperty freqMax   = FindProperty("_FrequencyMax", props, false);
        MaterialProperty shapeSize = FindProperty("_ShapeSize",    props, false);

        // Global
        MaterialProperty speed        = FindProperty("_Speed",                 props, false);
        MaterialProperty distortStr   = FindProperty("_DistortStrength",       props, false);
        MaterialProperty waterMoveStr = FindProperty("_WaterMovementStrength", props, false);
        MaterialProperty timeChoppy   = FindProperty("_TimeChoppiness",        props, false);
        MaterialProperty waterPix     = FindProperty("_WaterPixelation",       props, false);
        MaterialProperty depthFade    = FindProperty("_DepthFadeDistance",     props, false);

        // Toggles
        MaterialProperty useDistort = FindProperty("_UseDistortion", props, false);
        MaterialProperty useSpec    = FindProperty("_UseSpecular",   props, false);
        MaterialProperty useSpark   = FindProperty("_UseSparkle",    props, false);

        // Specular
        MaterialProperty specColor           = FindProperty("_WaterSpecColor",           props, false);
        MaterialProperty specIntensity       = FindProperty("_WaterSpecIntensity",       props, false);
        MaterialProperty specHeightThreshold = FindProperty("_WaterSpecHeightThreshold", props, false);
        MaterialProperty specFadeRange       = FindProperty("_WaterSpecFadeRange",       props, false);

        // Sparkle
        MaterialProperty sparkleIntensity = FindProperty("_WaterSparkleIntensity", props, false);
        MaterialProperty sparkleScale     = FindProperty("_WaterSparkleScale",     props, false);
        MaterialProperty sparkleSpeed     = FindProperty("_WaterSparkleSpeed",     props, false);
        MaterialProperty sparkleThreshold = FindProperty("_WaterSparkleThreshold", props, false);

        // Base texture / tint / alpha
        if (mainTex != null)
            materialEditor.TexturePropertySingleLine(new GUIContent("Main Tex"), mainTex, color);

        if (alpha != null)
            materialEditor.ShaderProperty(alpha, alpha.displayName);

        EditorGUILayout.Space();

        // Mode
        if (waveModeProp != null)
        {
            int mode = (waveModeProp.floatValue < 0.5f) ? 0 : 1;

            EditorGUILayout.LabelField("Water Mode", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.IntPopup(
                "Mode",
                mode,
                new[] { "Waves", "Ripples" },
                new[] { 0, 1 }
            );
            if (EditorGUI.EndChangeCheck())
            {
                waveModeProp.floatValue = mode;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Shape
        showShapeSettings = EditorGUILayout.Foldout(showShapeSettings, "Water Shape", true);
        if (showShapeSettings)
        {
            EditorGUI.indentLevel++;

            if (ampMin != null)    materialEditor.ShaderProperty(ampMin,    ampMin.displayName);
            if (ampMax != null)    materialEditor.ShaderProperty(ampMax,    ampMax.displayName);
            if (freqMin != null)   materialEditor.ShaderProperty(freqMin,   freqMin.displayName);
            if (freqMax != null)   materialEditor.ShaderProperty(freqMax,   freqMax.displayName);
            if (shapeSize != null) materialEditor.ShaderProperty(shapeSize, shapeSize.displayName);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Global
        showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "Global Settings", true);
        if (showGlobalSettings)
        {
            EditorGUI.indentLevel++;

            if (speed != null)
                materialEditor.ShaderProperty(speed, speed.displayName);

            if (distortStr != null)
                materialEditor.ShaderProperty(distortStr, distortStr.displayName);

            // Distortion toggle
            if (useDistort != null)
            {
                bool d = useDistort.floatValue > 0.5f;
                EditorGUI.BeginChangeCheck();
                d = EditorGUILayout.Toggle("Use Distortion", d);
                if (EditorGUI.EndChangeCheck())
                    useDistort.floatValue = d ? 1f : 0f;
            }

            if (waterMoveStr != null)
                materialEditor.ShaderProperty(waterMoveStr, waterMoveStr.displayName);

            if (timeChoppy != null)
                materialEditor.ShaderProperty(timeChoppy, timeChoppy.displayName);

            if (waterPix != null)
                materialEditor.ShaderProperty(waterPix, waterPix.displayName);

            if (depthFade != null)
                materialEditor.ShaderProperty(depthFade, depthFade.displayName);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Specular
        showSpecSettings = EditorGUILayout.Foldout(showSpecSettings, "Specular", true);
        if (showSpecSettings)
        {
            EditorGUI.indentLevel++;

            // Spec toggle
            if (useSpec != null)
            {
                bool s = useSpec.floatValue > 0.5f;
                EditorGUI.BeginChangeCheck();
                s = EditorGUILayout.Toggle("Use Specular", s);
                if (EditorGUI.EndChangeCheck())
                    useSpec.floatValue = s ? 1f : 0f;
            }

            if (specColor != null)
                materialEditor.ShaderProperty(specColor, specColor.displayName);
            if (specIntensity != null)
                materialEditor.ShaderProperty(specIntensity, specIntensity.displayName);
            if (specHeightThreshold != null)
                materialEditor.ShaderProperty(specHeightThreshold, specHeightThreshold.displayName);
            if (specFadeRange != null)
                materialEditor.ShaderProperty(specFadeRange, specFadeRange.displayName);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Sparkle
        showSparkleSettings = EditorGUILayout.Foldout(showSparkleSettings, "Sparkle", true);
        if (showSparkleSettings)
        {
            EditorGUI.indentLevel++;

            // Sparkle toggle
            if (useSpark != null)
            {
                bool sp = useSpark.floatValue > 0.5f;
                EditorGUI.BeginChangeCheck();
                sp = EditorGUILayout.Toggle("Use Sparkle", sp);
                if (EditorGUI.EndChangeCheck())
                    useSpark.floatValue = sp ? 1f : 0f;
            }

            if (sparkleIntensity != null)
                materialEditor.ShaderProperty(sparkleIntensity, sparkleIntensity.displayName);
            if (sparkleScale != null)
                materialEditor.ShaderProperty(sparkleScale, sparkleScale.displayName);
            if (sparkleSpeed != null)
                materialEditor.ShaderProperty(sparkleSpeed, sparkleSpeed.displayName);
            if (sparkleThreshold != null)
                materialEditor.ShaderProperty(sparkleThreshold, sparkleThreshold.displayName);

            EditorGUI.indentLevel--;
        }
    }
}
