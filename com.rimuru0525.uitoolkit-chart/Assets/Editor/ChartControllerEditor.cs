using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChartController))]
public class ChartControllerEditor : Editor
{
    private SerializedProperty chartSettings;
    
    // ChartSettings properties
    private SerializedProperty yAxisDivisions;
    private SerializedProperty xAxisDivisions;
    private SerializedProperty useNiceYAxisSteps;
    private SerializedProperty useManualYAxisRange;
    private SerializedProperty manualYAxisMin;
    private SerializedProperty manualYAxisMax;
    private SerializedProperty useManualYAxisStep;
    private SerializedProperty manualYAxisStep;
    private SerializedProperty lineWidth;
    private SerializedProperty showGrid;
    private SerializedProperty gridColor;
    private SerializedProperty showZeroLine;
    private SerializedProperty zeroLineColor;
    private SerializedProperty textColor;
    private SerializedProperty textSize;
    
    private void OnEnable()
    {
        chartSettings = serializedObject.FindProperty("chartSettings");
        
        // Find all properties
        yAxisDivisions = chartSettings.FindPropertyRelative("yAxisDivisions");
        xAxisDivisions = chartSettings.FindPropertyRelative("xAxisDivisions");
        useNiceYAxisSteps = chartSettings.FindPropertyRelative("useNiceYAxisSteps");
        useManualYAxisRange = chartSettings.FindPropertyRelative("useManualYAxisRange");
        manualYAxisMin = chartSettings.FindPropertyRelative("manualYAxisMin");
        manualYAxisMax = chartSettings.FindPropertyRelative("manualYAxisMax");
        useManualYAxisStep = chartSettings.FindPropertyRelative("useManualYAxisStep");
        manualYAxisStep = chartSettings.FindPropertyRelative("manualYAxisStep");
        lineWidth = chartSettings.FindPropertyRelative("lineWidth");
        showGrid = chartSettings.FindPropertyRelative("showGrid");
        gridColor = chartSettings.FindPropertyRelative("gridColor");
        showZeroLine = chartSettings.FindPropertyRelative("showZeroLine");
        zeroLineColor = chartSettings.FindPropertyRelative("zeroLineColor");
        textColor = chartSettings.FindPropertyRelative("textColor");
        textSize = chartSettings.FindPropertyRelative("textSize");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Chart Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        // Axis Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axis Settings", EditorStyles.boldLabel);
        
        // Y Axis Divisions - disable if manual step is used
        EditorGUI.BeginDisabledGroup(useManualYAxisStep.boolValue);
        EditorGUILayout.PropertyField(yAxisDivisions);
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.PropertyField(xAxisDivisions);
        
        // Use Nice Y Axis Steps - disable if manual step is used
        EditorGUI.BeginDisabledGroup(useManualYAxisStep.boolValue);
        EditorGUILayout.PropertyField(useNiceYAxisSteps, new GUIContent("Use Nice Y Axis Steps"));
        EditorGUI.EndDisabledGroup();
        
        if (useManualYAxisStep.boolValue)
        {
            EditorGUILayout.HelpBox("Y Axis Divisions and Nice Y Axis Steps are ignored when using Manual Y Axis Step", MessageType.Info);
        }
        
        // Y Axis Manual Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Y Axis Manual Settings", EditorStyles.boldLabel);
        
        // Manual Range
        EditorGUILayout.PropertyField(useManualYAxisRange, new GUIContent("Use Manual Y Axis Range"));
        
        EditorGUI.BeginDisabledGroup(!useManualYAxisRange.boolValue);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(manualYAxisMin, new GUIContent("Min"));
        EditorGUILayout.PropertyField(manualYAxisMax, new GUIContent("Max"));
        
        // Validate min < max
        if (useManualYAxisRange.boolValue && manualYAxisMin.floatValue >= manualYAxisMax.floatValue)
        {
            EditorGUILayout.HelpBox("Min value must be less than Max value", MessageType.Warning);
        }
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(5);
        
        // Manual Step
        EditorGUILayout.PropertyField(useManualYAxisStep, new GUIContent("Use Manual Y Axis Step"));
        
        EditorGUI.BeginDisabledGroup(!useManualYAxisStep.boolValue);
        EditorGUI.indentLevel++;
        
        // Calculate minimum allowed step
        float minStep = 0.001f;
        if (useManualYAxisRange.boolValue)
        {
            float range = Mathf.Abs(manualYAxisMax.floatValue - manualYAxisMin.floatValue);
            minStep = range / 100f; // Maximum 100 divisions
        }
        
        EditorGUILayout.PropertyField(manualYAxisStep, new GUIContent("Step Size"));
        
        // Ensure step is within reasonable bounds
        if (manualYAxisStep.floatValue < minStep)
        {
            manualYAxisStep.floatValue = minStep;
        }
        
        // Show warning if step is too small
        if (useManualYAxisRange.boolValue)
        {
            float range = Mathf.Abs(manualYAxisMax.floatValue - manualYAxisMin.floatValue);
            int estimatedSteps = Mathf.CeilToInt(range / manualYAxisStep.floatValue);
            
            if (estimatedSteps > 50)
            {
                EditorGUILayout.HelpBox($"This will create {estimatedSteps} divisions. Consider using a larger step size.", MessageType.Warning);
            }
        }
        
        // Show Excel-like step suggestions
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Quick Set:", GUILayout.Width(70));
        if (GUILayout.Button("0.1")) manualYAxisStep.floatValue = 0.1f;
        if (GUILayout.Button("0.25")) manualYAxisStep.floatValue = 0.25f;
        if (GUILayout.Button("0.5")) manualYAxisStep.floatValue = 0.5f;
        if (GUILayout.Button("1")) manualYAxisStep.floatValue = 1f;
        if (GUILayout.Button("2.5")) manualYAxisStep.floatValue = 2.5f;
        if (GUILayout.Button("5")) manualYAxisStep.floatValue = 5f;
        if (GUILayout.Button("10")) manualYAxisStep.floatValue = 10f;
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
        
        // Line Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Line Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(lineWidth);
        
        // Grid Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showGrid);
        
        EditorGUI.BeginDisabledGroup(!showGrid.boolValue);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(gridColor);
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.PropertyField(showZeroLine);
        
        EditorGUI.BeginDisabledGroup(!showZeroLine.boolValue);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(zeroLineColor);
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
        
        // Text Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Text Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(textColor);
        EditorGUILayout.PropertyField(textSize);
        
        EditorGUI.indentLevel--;
        
        serializedObject.ApplyModifiedProperties();
    }
}