using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class ChartSettings
{
    [Header("Axis Settings")]
    [Range(2, 20)]
    public int yAxisDivisions = 5;
    [Range(2, 20)]
    public int xAxisDivisions = 10;
    public bool useNiceYAxisSteps = true;
    
    [Header("Y Axis Manual Settings")]
    public bool useManualYAxisRange = false;
    public float manualYAxisMin = 0f;
    public float manualYAxisMax = 100f;
    public bool useManualYAxisStep = false;
    [Min(0.001f)]
    public float manualYAxisStep = 1f;
    
    [Header("Line Settings")]
    [Range(0.5f, 10f)]
    public float lineWidth = 2f;
    
    [Header("Grid Settings")]
    public bool showGrid = true;
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    public bool showZeroLine = true;
    public Color zeroLineColor = new Color(1f, 1f, 1f, 0.5f);
    
    [Header("Text Settings")]
    public Color textColor = Color.white;
    [Range(8, 20)]
    public int textSize = 11;
}

[RequireComponent(typeof(UIDocument))]
public class ChartController : MonoBehaviour
{
    [SerializeField] private ChartSettings chartSettings = new ChartSettings();
    
    private ChartElement _chartElement;
    private ChartSettings _previousSettings;
    
    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        _chartElement = root.Q<ChartElement>("chart");
        
        if (_chartElement == null)
        {
            Debug.LogError("ChartElement with name 'chart' not found!");
            return;
        }
        
        ApplySettings();
        _previousSettings = CloneSettings(chartSettings);
    }
    
    void Update()
    {
        // Check if settings have changed
        if (!SettingsEqual(_previousSettings, chartSettings))
        {
            ApplySettings();
            _previousSettings = CloneSettings(chartSettings);
        }
    }
    
    private void ApplySettings()
    {
        if (_chartElement == null) return;
        
        _chartElement.yAxisDivisions = chartSettings.yAxisDivisions;
        _chartElement.xAxisDivisions = chartSettings.xAxisDivisions;
        _chartElement.useNiceYAxisSteps = chartSettings.useNiceYAxisSteps;
        _chartElement.useManualYAxisRange = chartSettings.useManualYAxisRange;
        _chartElement.manualYAxisMin = chartSettings.manualYAxisMin;
        _chartElement.manualYAxisMax = chartSettings.manualYAxisMax;
        _chartElement.useManualYAxisStep = chartSettings.useManualYAxisStep;
        _chartElement.manualYAxisStep = chartSettings.manualYAxisStep;
        _chartElement.lineWidth = chartSettings.lineWidth;
        _chartElement.showGrid = chartSettings.showGrid;
        _chartElement.gridColor = chartSettings.gridColor;
        _chartElement.showZeroLine = chartSettings.showZeroLine;
        _chartElement.zeroLineColor = chartSettings.zeroLineColor;
        _chartElement.textColor = chartSettings.textColor;
        _chartElement.textSize = chartSettings.textSize;
    }
    
    private ChartSettings CloneSettings(ChartSettings settings)
    {
        return new ChartSettings
        {
            yAxisDivisions = settings.yAxisDivisions,
            xAxisDivisions = settings.xAxisDivisions,
            useNiceYAxisSteps = settings.useNiceYAxisSteps,
            useManualYAxisRange = settings.useManualYAxisRange,
            manualYAxisMin = settings.manualYAxisMin,
            manualYAxisMax = settings.manualYAxisMax,
            useManualYAxisStep = settings.useManualYAxisStep,
            manualYAxisStep = settings.manualYAxisStep,
            lineWidth = settings.lineWidth,
            showGrid = settings.showGrid,
            gridColor = settings.gridColor,
            showZeroLine = settings.showZeroLine,
            zeroLineColor = settings.zeroLineColor,
            textColor = settings.textColor,
            textSize = settings.textSize
        };
    }
    
    private bool SettingsEqual(ChartSettings a, ChartSettings b)
    {
        if (a == null || b == null) return false;
        
        return a.yAxisDivisions == b.yAxisDivisions &&
               a.xAxisDivisions == b.xAxisDivisions &&
               a.useNiceYAxisSteps == b.useNiceYAxisSteps &&
               a.useManualYAxisRange == b.useManualYAxisRange &&
               Mathf.Approximately(a.manualYAxisMin, b.manualYAxisMin) &&
               Mathf.Approximately(a.manualYAxisMax, b.manualYAxisMax) &&
               a.useManualYAxisStep == b.useManualYAxisStep &&
               Mathf.Approximately(a.manualYAxisStep, b.manualYAxisStep) &&
               Mathf.Approximately(a.lineWidth, b.lineWidth) &&
               a.showGrid == b.showGrid &&
               a.gridColor == b.gridColor &&
               a.showZeroLine == b.showZeroLine &&
               a.zeroLineColor == b.zeroLineColor &&
               a.textColor == b.textColor &&
               a.textSize == b.textSize;
    }
    
    void OnValidate()
    {
        if (_chartElement != null && Application.isPlaying)
        {
            ApplySettings();
        }
    }
}