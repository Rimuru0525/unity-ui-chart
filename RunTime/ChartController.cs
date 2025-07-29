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
    
    [Header("Line Settings")]
    [Range(0.5f, 10f)]
    public float lineWidth = 2f;
    
    [Header("Grid Settings")]
    public bool showGrid = true;
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    
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
        _chartElement.lineWidth = chartSettings.lineWidth;
        _chartElement.showGrid = chartSettings.showGrid;
        _chartElement.gridColor = chartSettings.gridColor;
        _chartElement.textColor = chartSettings.textColor;
        _chartElement.textSize = chartSettings.textSize;
    }
    
    private ChartSettings CloneSettings(ChartSettings settings)
    {
        return new ChartSettings
        {
            yAxisDivisions = settings.yAxisDivisions,
            xAxisDivisions = settings.xAxisDivisions,
            lineWidth = settings.lineWidth,
            showGrid = settings.showGrid,
            gridColor = settings.gridColor,
            textColor = settings.textColor,
            textSize = settings.textSize
        };
    }
    
    private bool SettingsEqual(ChartSettings a, ChartSettings b)
    {
        if (a == null || b == null) return false;
        
        return a.yAxisDivisions == b.yAxisDivisions &&
               a.xAxisDivisions == b.xAxisDivisions &&
               Mathf.Approximately(a.lineWidth, b.lineWidth) &&
               a.showGrid == b.showGrid &&
               a.gridColor == b.gridColor &&
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