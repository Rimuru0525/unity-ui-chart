using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class ChartData
{
    public string name;
    public Color color;
    public List<DataPoint> points = new List<DataPoint>();
}

[System.Serializable]
public class DataPoint
{
    public float timestamp;
    public float value;
    
    public DataPoint(float timestamp, float value)
    {
        this.timestamp = timestamp;
        this.value = value;
    }
}

[UxmlElement]
public partial class ChartElement : VisualElement
{
    // Legacy UXML factory for compatibility
    public new class UxmlFactory : UxmlFactory<ChartElement> { }
    
    private List<ChartData> _datasets = new List<ChartData>();
    private Label _tooltipLabel;
    private VisualElement _labelsContainer;
    private Vector2 _mousePosition;
    private bool _showCrosshair;
    
    // Inspector properties with UXML attributes
    [UxmlAttribute]
    private int _yAxisDivisions = 5;
    
    [UxmlAttribute]
    private int _xAxisDivisions = 10;
    
    [UxmlAttribute]
    private float _lineWidth = 2f;
    
    [UxmlAttribute]
    private bool _showGrid = true;
    
    [UxmlAttribute]
    private Color _gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    
    private bool _showZeroLine = true;
    private Color _zeroLineColor = new Color(1f, 1f, 1f, 0.5f);
    
    [UxmlAttribute]
    private Color _textColor = Color.white;
    
    private int _textSize = 11;
    private bool _useNiceYAxisSteps = true;
    private bool _useManualYAxisRange = false;
    private float _manualYAxisMin = 0f;
    private float _manualYAxisMax = 100f;
    private bool _useManualYAxisStep = false;
    private float _manualYAxisStep = 1f;
    
    public int yAxisDivisions 
    { 
        get => _yAxisDivisions;
        set { _yAxisDivisions = value; UpdateLabels(); MarkDirtyRepaint(); }
    }
    
    public int xAxisDivisions 
    { 
        get => _xAxisDivisions;
        set { _xAxisDivisions = value; UpdateLabels(); MarkDirtyRepaint(); }
    }
    
    public float lineWidth 
    { 
        get => _lineWidth;
        set { _lineWidth = value; MarkDirtyRepaint(); }
    }
    
    public bool showGrid 
    { 
        get => _showGrid;
        set { _showGrid = value; MarkDirtyRepaint(); }
    }
    
    public Color gridColor 
    { 
        get => _gridColor;
        set { _gridColor = value; MarkDirtyRepaint(); }
    }
    
    public bool showZeroLine
    {
        get => _showZeroLine;
        set { _showZeroLine = value; MarkDirtyRepaint(); }
    }
    
    public Color zeroLineColor
    {
        get => _zeroLineColor;
        set { _zeroLineColor = value; MarkDirtyRepaint(); }
    }
    
    public Color textColor 
    { 
        get => _textColor;
        set { _textColor = value; UpdateLabelsColor(); MarkDirtyRepaint(); }
    }
    
    public int textSize
    {
        get => _textSize;
        set { _textSize = value; UpdateLabels(); }
    }
    
    public bool useNiceYAxisSteps
    {
        get => _useNiceYAxisSteps;
        set { _useNiceYAxisSteps = value; UpdateBounds(); UpdateLabels(); MarkDirtyRepaint(); }
    }
    
    public bool useManualYAxisRange
    {
        get => _useManualYAxisRange;
        set { _useManualYAxisRange = value; UpdateBounds(); UpdateLabels(); MarkDirtyRepaint(); }
    }
    
    public float manualYAxisMin
    {
        get => _manualYAxisMin;
        set { _manualYAxisMin = value; if (_useManualYAxisRange) { UpdateBounds(); UpdateLabels(); MarkDirtyRepaint(); } }
    }
    
    public float manualYAxisMax
    {
        get => _manualYAxisMax;
        set { _manualYAxisMax = value; if (_useManualYAxisRange) { UpdateBounds(); UpdateLabels(); MarkDirtyRepaint(); } }
    }
    
    public bool useManualYAxisStep
    {
        get => _useManualYAxisStep;
        set { _useManualYAxisStep = value; UpdateBounds(); UpdateLabels(); MarkDirtyRepaint(); }
    }
    
    public float manualYAxisStep
    {
        get => _manualYAxisStep;
        set { _manualYAxisStep = Mathf.Max(0.001f, value); if (_useManualYAxisStep) { UpdateBounds(); UpdateLabels(); MarkDirtyRepaint(); } }
    }
    
    private float _minValue;
    private float _maxValue;
    private float _minTime;
    private float _maxTime;
    private float _padding = 50f;
    
    // Nice Y axis values
    private float _niceMinValue;
    private float _niceMaxValue;
    private float _niceStepSize;
    private int _niceStepCount;
    
    public ChartElement()
    {
        generateVisualContent += OnGenerateVisualContent;
        AddToClassList("chart-element");
        
        // Create labels container
        _labelsContainer = new VisualElement();
        _labelsContainer.style.position = Position.Absolute;
        _labelsContainer.style.left = 0;
        _labelsContainer.style.top = 0;
        _labelsContainer.style.right = 0;
        _labelsContainer.style.bottom = 0;
        _labelsContainer.pickingMode = PickingMode.Ignore;
        Add(_labelsContainer);
        
        // Create tooltip
        _tooltipLabel = new Label();
        _tooltipLabel.AddToClassList("chart-tooltip");
        _tooltipLabel.style.position = Position.Absolute;
        _tooltipLabel.style.display = DisplayStyle.None;
        _tooltipLabel.pickingMode = PickingMode.Ignore;
        Add(_tooltipLabel);
        
        // Register mouse events
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        
        // Initialize bounds
        UpdateBounds();
    }
    
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateLabels();
    }
    
    public void AddDataset(ChartData dataset)
    {
        _datasets.Add(dataset);
        UpdateBounds();
        UpdateLabels();
        MarkDirtyRepaint();
    }
    
    public void ClearDatasets()
    {
        _datasets.Clear();
        UpdateLabels();
        MarkDirtyRepaint();
    }
    
    public void UpdateDataset(int index, ChartData dataset)
    {
        if (index >= 0 && index < _datasets.Count)
        {
            _datasets[index] = dataset;
            UpdateBounds();
            UpdateLabels();
            MarkDirtyRepaint();
        }
    }
    
    private void UpdateBounds()
    {
        if (_datasets.Count == 0 || _datasets.All(d => d.points.Count == 0))
        {
            _minValue = 0;
            _maxValue = 1;
            _minTime = 0;
            _maxTime = 1;
            CalculateNiceYAxis();
            return;
        }
        
        _minValue = float.MaxValue;
        _maxValue = float.MinValue;
        _minTime = float.MaxValue;
        _maxTime = float.MinValue;
        
        foreach (var dataset in _datasets)
        {
            foreach (var point in dataset.points)
            {
                _minValue = Mathf.Min(_minValue, point.value);
                _maxValue = Mathf.Max(_maxValue, point.value);
                _minTime = Mathf.Min(_minTime, point.timestamp);
                _maxTime = Mathf.Max(_maxTime, point.timestamp);
            }
        }
        
        // Add some padding to the value range
        float valueRange = _maxValue - _minValue;
        _minValue -= valueRange * 0.1f;
        _maxValue += valueRange * 0.1f;
        
        // Calculate nice Y axis values
        CalculateNiceYAxis();
    }
    
    private void CalculateNiceYAxis()
    {
        // Apply manual range if enabled
        if (_useManualYAxisRange)
        {
            _niceMinValue = _manualYAxisMin;
            _niceMaxValue = _manualYAxisMax;
            
            // Ensure min < max
            if (_niceMinValue >= _niceMaxValue)
            {
                _niceMaxValue = _niceMinValue + 1f;
            }
        }
        else
        {
            _niceMinValue = _minValue;
            _niceMaxValue = _maxValue;
        }
        
        // Apply manual step if enabled
        if (_useManualYAxisStep)
        {
            _niceStepSize = _manualYAxisStep;
            
            // Calculate step count and prevent too many steps
            float currentRange = _niceMaxValue - _niceMinValue;
            int estimatedSteps = Mathf.CeilToInt(currentRange / _niceStepSize);
            
            // Limit to reasonable number of steps (Excel-like behavior)
            if (estimatedSteps > 100)
            {
                // Adjust step size to have maximum 100 steps
                _niceStepSize = currentRange / 100f;
                Debug.LogWarning($"Manual Y axis step too small. Adjusted to {_niceStepSize:F4} to prevent excessive divisions.");
            }
            
            // If not using manual range, adjust the range to fit the step size nicely
            if (!_useManualYAxisRange)
            {
                _niceMinValue = Mathf.Floor(_niceMinValue / _niceStepSize) * _niceStepSize;
                _niceMaxValue = Mathf.Ceil(_niceMaxValue / _niceStepSize) * _niceStepSize;
            }
            
            _niceStepCount = Mathf.RoundToInt((_niceMaxValue - _niceMinValue) / _niceStepSize);
            return;
        }
        
        // If not using manual step, calculate nice steps
        if (!_useNiceYAxisSteps)
        {
            return;
        }
        
        float range = _niceMaxValue - _niceMinValue;
        float targetSteps = _yAxisDivisions;
        
        // Calculate the rough step size
        float roughStep = range / targetSteps;
        
        // Prevent zero or very small steps
        if (roughStep <= 0)
        {
            roughStep = 1f;
        }
        
        // Find the order of magnitude
        float magnitude = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(roughStep)));
        
        // Normalize the step to be between 1 and 10
        float normalizedStep = roughStep / magnitude;
        
        // Choose a nice step value
        float niceStep;
        if (normalizedStep <= 1f)
            niceStep = 1f;
        else if (normalizedStep <= 2f)
            niceStep = 2f;
        else if (normalizedStep <= 2.5f)
            niceStep = 2.5f;
        else if (normalizedStep <= 5f)
            niceStep = 5f;
        else
            niceStep = 10f;
        
        _niceStepSize = niceStep * magnitude;
        
        // Calculate nice min and max values if not using manual range
        if (!_useManualYAxisRange)
        {
            _niceMinValue = Mathf.Floor(_minValue / _niceStepSize) * _niceStepSize;
            _niceMaxValue = Mathf.Ceil(_maxValue / _niceStepSize) * _niceStepSize;
            
            // Ensure we include zero if it's close to the range
            if (_showZeroLine && _minValue <= 0 && _maxValue >= 0)
            {
                if (_niceMinValue > 0) _niceMinValue = 0;
                if (_niceMaxValue < 0) _niceMaxValue = 0;
            }
        }
        
        // Calculate actual step count
        _niceStepCount = Mathf.RoundToInt((_niceMaxValue - _niceMinValue) / _niceStepSize);
    }
    
    private void UpdateLabels()
    {
        // Clear existing labels
        _labelsContainer.Clear();
        
        var rect = contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;
        
        float chartLeft = _padding;
        float chartRight = rect.width - 20f;
        float chartTop = 20f;
        float chartBottom = rect.height - _padding;
        float chartHeight = chartBottom - chartTop;
        float chartWidth = chartRight - chartLeft;
        
        // Create Y-axis labels
        if ((_useNiceYAxisSteps || _useManualYAxisStep) && _niceStepSize > 0)
        {
            // Use nice or manual step values
            float currentValue = _niceMinValue;
            int stepIndex = 0;
            
            while (currentValue <= _niceMaxValue + _niceStepSize * 0.01f && stepIndex <= 100) // Safety limit increased
            {
                float y = chartTop + ((_niceMaxValue - currentValue) / (_niceMaxValue - _niceMinValue)) * chartHeight;
                
                // Format value based on magnitude
                string format;
                if (_useManualYAxisStep)
                {
                    // Format based on manual step size
                    if (_manualYAxisStep >= 1f)
                        format = "F0";
                    else if (_manualYAxisStep >= 0.1f)
                        format = "F1";
                    else if (_manualYAxisStep >= 0.01f)
                        format = "F2";
                    else if (_manualYAxisStep >= 0.001f)
                        format = "F3";
                    else
                        format = "F4";
                }
                else
                {
                    // Format based on calculated nice step
                    format = _niceStepSize >= 1f ? "F0" : 
                            _niceStepSize >= 0.1f ? "F1" : 
                            _niceStepSize >= 0.01f ? "F2" : "F3";
                }
                
                var label = new Label(currentValue.ToString(format));
                label.AddToClassList("axis-label");
                label.style.position = Position.Absolute;
                label.style.color = _textColor;
                label.style.fontSize = _textSize;
                label.style.left = chartLeft - 45;
                label.style.top = y - 8;
                label.style.unityTextAlign = TextAnchor.MiddleRight;
                label.style.width = 40;
                
                // Highlight zero if it's exactly on a step
                if (Mathf.Abs(currentValue) < 0.0001f && _showZeroLine)
                {
                    label.style.color = _zeroLineColor;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                
                _labelsContainer.Add(label);
                
                currentValue += _niceStepSize;
                stepIndex++;
            }
        }
        else
        {
            // Use regular divisions
            for (int i = 0; i <= _yAxisDivisions; i++)
            {
                float y = chartTop + (i / (float)_yAxisDivisions) * chartHeight;
                float value = _maxValue - (i / (float)_yAxisDivisions) * (_maxValue - _minValue);
                
                var label = new Label(value.ToString("F1"));
                label.AddToClassList("axis-label");
                label.style.position = Position.Absolute;
                label.style.color = _textColor;
                label.style.fontSize = _textSize;
                label.style.left = chartLeft - 45;
                label.style.top = y - 8;
                label.style.unityTextAlign = TextAnchor.MiddleRight;
                label.style.width = 40;
                _labelsContainer.Add(label);
            }
            
            // Add zero label if needed (for non-nice steps mode)
            if (_showZeroLine && _minValue <= 0 && _maxValue >= 0)
            {
                bool zeroAlreadyLabeled = false;
                
                for (int i = 0; i <= _yAxisDivisions; i++)
                {
                    float value = _maxValue - (i / (float)_yAxisDivisions) * (_maxValue - _minValue);
                    if (Mathf.Abs(value) < 0.01f)
                    {
                        zeroAlreadyLabeled = true;
                        break;
                    }
                }
                
                if (!zeroAlreadyLabeled)
                {
                    float zeroY = chartTop + ((_maxValue - 0) / (_maxValue - _minValue)) * chartHeight;
                    var zeroLabel = new Label("0.0");
                    zeroLabel.AddToClassList("axis-label");
                    zeroLabel.style.position = Position.Absolute;
                    zeroLabel.style.color = _zeroLineColor;
                    zeroLabel.style.fontSize = _textSize;
                    zeroLabel.style.left = chartLeft - 45;
                    zeroLabel.style.top = zeroY - 8;
                    zeroLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                    zeroLabel.style.width = 40;
                    zeroLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    _labelsContainer.Add(zeroLabel);
                }
            }
        }
        
        // Create X-axis labels
        for (int i = 0; i <= _xAxisDivisions; i++)
        {
            float x = chartLeft + (i / (float)_xAxisDivisions) * chartWidth;
            float time = _minTime + (i / (float)_xAxisDivisions) * (_maxTime - _minTime);
            
            var label = new Label(time.ToString("F1"));
            label.AddToClassList("axis-label");
            label.style.position = Position.Absolute;
            label.style.color = _textColor;
            label.style.fontSize = _textSize;
            label.style.left = x - 25;
            label.style.top = chartBottom + 5;
            label.style.unityTextAlign = TextAnchor.UpperCenter;
            label.style.width = 50;
            _labelsContainer.Add(label);
        }
    }
    
    private void UpdateLabelsColor()
    {
        foreach (var child in _labelsContainer.Children())
        {
            if (child is Label label)
            {
                label.style.color = _textColor;
                label.style.fontSize = _textSize;
            }
        }
    }
    
    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        var rect = contentRect;
        
        if (rect.width <= 0 || rect.height <= 0) return;
        
        // Draw background
        painter.fillColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, 0));
        painter.LineTo(new Vector2(rect.width, 0));
        painter.LineTo(new Vector2(rect.width, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.ClosePath();
        painter.Fill();
        
        // Calculate chart area
        float chartLeft = _padding;
        float chartRight = rect.width - 20f;
        float chartTop = 20f;
        float chartBottom = rect.height - _padding;
        float chartWidth = chartRight - chartLeft;
        float chartHeight = chartBottom - chartTop;
        
        if (chartWidth <= 0 || chartHeight <= 0) return;
        
        // Draw grid
        if (_showGrid)
        {
            painter.strokeColor = _gridColor;
            painter.lineWidth = 1f;
            
            // Y-axis grid lines
            if ((_useNiceYAxisSteps || _useManualYAxisStep) && _niceStepSize > 0)
            {
                // Use nice or manual step values for grid
                float currentValue = _niceMinValue;
                int stepIndex = 0;
                
                while (currentValue <= _niceMaxValue + _niceStepSize * 0.01f && stepIndex <= 100)
                {
                    float y = chartTop + ((_niceMaxValue - currentValue) / (_niceMaxValue - _niceMinValue)) * chartHeight;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(chartLeft, y));
                    painter.LineTo(new Vector2(chartRight, y));
                    painter.Stroke();
                    
                    currentValue += _niceStepSize;
                    stepIndex++;
                }
            }
            else
            {
                // Regular grid lines
                for (int i = 0; i <= _yAxisDivisions; i++)
                {
                    float y = chartTop + (i / (float)_yAxisDivisions) * chartHeight;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(chartLeft, y));
                    painter.LineTo(new Vector2(chartRight, y));
                    painter.Stroke();
                }
            }
            
            // X-axis grid lines
            for (int i = 0; i <= _xAxisDivisions; i++)
            {
                float x = chartLeft + (i / (float)_xAxisDivisions) * chartWidth;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, chartTop));
                painter.LineTo(new Vector2(x, chartBottom));
                painter.Stroke();
            }
        }
        
        // Draw axes
        painter.strokeColor = _textColor;
        painter.lineWidth = 2f;
        painter.BeginPath();
        painter.MoveTo(new Vector2(chartLeft, chartTop));
        painter.LineTo(new Vector2(chartLeft, chartBottom));
        painter.LineTo(new Vector2(chartRight, chartBottom));
        painter.Stroke();
        
        // Draw zero line if enabled and zero is in range
        if (_showZeroLine)
        {
            float minVal = _useNiceYAxisSteps ? _niceMinValue : _minValue;
            float maxVal = _useNiceYAxisSteps ? _niceMaxValue : _maxValue;
            
            if (minVal <= 0 && maxVal >= 0)
            {
                float zeroY = chartTop + ((maxVal - 0) / (maxVal - minVal)) * chartHeight;
                painter.strokeColor = _zeroLineColor;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(chartLeft, zeroY));
                painter.LineTo(new Vector2(chartRight, zeroY));
                painter.Stroke();
            }
        }
        
        // Draw datasets
        foreach (var dataset in _datasets)
        {
            if (dataset.points.Count < 2) continue;
            
            painter.strokeColor = dataset.color;
            painter.lineWidth = _lineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            
            painter.BeginPath();
            
            float minVal = _useNiceYAxisSteps ? _niceMinValue : _minValue;
            float maxVal = _useNiceYAxisSteps ? _niceMaxValue : _maxValue;
            
            for (int i = 0; i < dataset.points.Count; i++)
            {
                var point = dataset.points[i];
                float x = chartLeft + ((point.timestamp - _minTime) / (_maxTime - _minTime)) * chartWidth;
                float y = chartTop + ((maxVal - point.value) / (maxVal - minVal)) * chartHeight;
                
                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }
            
            painter.Stroke();
            
            // Draw points
            painter.fillColor = dataset.color;
            foreach (var point in dataset.points)
            {
                float x = chartLeft + ((point.timestamp - _minTime) / (_maxTime - _minTime)) * chartWidth;
                float y = chartTop + ((maxVal - point.value) / (maxVal - minVal)) * chartHeight;
                
                painter.BeginPath();
                painter.Arc(new Vector2(x, y), 3f, 0, 360);
                painter.Fill();
            }
        }
        
        // Draw crosshair
        if (_showCrosshair && _mousePosition.x >= chartLeft && _mousePosition.x <= chartRight)
        {
            // Vertical line
            painter.strokeColor = new Color(1f, 1f, 1f, 0.3f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(_mousePosition.x, chartTop));
            painter.LineTo(new Vector2(_mousePosition.x, chartBottom));
            painter.Stroke();
            
            // Highlight points at mouse position
            float normalizedX = (_mousePosition.x - chartLeft) / chartWidth;
            float timeAtMouse = _minTime + normalizedX * (_maxTime - _minTime);
            
            float minVal = _useNiceYAxisSteps ? _niceMinValue : _minValue;
            float maxVal = _useNiceYAxisSteps ? _niceMaxValue : _maxValue;
            
            foreach (var dataset in _datasets)
            {
                if (dataset.points.Count == 0) continue;
                
                var closestPoint = dataset.points.OrderBy(p => Mathf.Abs(p.timestamp - timeAtMouse)).First();
                float pointX = chartLeft + ((closestPoint.timestamp - _minTime) / (_maxTime - _minTime)) * chartWidth;
                float pointY = chartTop + ((maxVal - closestPoint.value) / (maxVal - minVal)) * chartHeight;
                
                // Outer circle
                painter.fillColor = new Color(1f, 1f, 1f, 0.2f);
                painter.BeginPath();
                painter.Arc(new Vector2(pointX, pointY), 8f, 0, 360);
                painter.Fill();
                
                // Inner circle
                painter.fillColor = dataset.color;
                painter.BeginPath();
                painter.Arc(new Vector2(pointX, pointY), 5f, 0, 360);
                painter.Fill();
            }
        }
    }
    
    private void OnMouseMove(MouseMoveEvent evt)
    {
        _mousePosition = evt.localMousePosition;
        
        float chartLeft = _padding;
        float chartRight = contentRect.width - 20f;
        float chartTop = 20f;
        float chartBottom = contentRect.height - _padding;
        
        if (_mousePosition.x >= chartLeft && _mousePosition.x <= chartRight &&
            _mousePosition.y >= chartTop && _mousePosition.y <= chartBottom)
        {
            UpdateTooltip();
            _showCrosshair = true;
            MarkDirtyRepaint();
        }
        else
        {
            _showCrosshair = false;
            _tooltipLabel.style.display = DisplayStyle.None;
            MarkDirtyRepaint();
        }
    }
    
    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        _showCrosshair = false;
        _tooltipLabel.style.display = DisplayStyle.None;
        MarkDirtyRepaint();
    }
    
    private void UpdateTooltip()
    {
        if (_datasets.Count == 0) return;
        
        float chartLeft = _padding;
        float chartRight = contentRect.width - 20f;
        float chartBottom = contentRect.height - _padding;
        float chartWidth = chartRight - chartLeft;
        
        float normalizedX = (_mousePosition.x - chartLeft) / chartWidth;
        float timeAtMouse = _minTime + normalizedX * (_maxTime - _minTime);
        
        // Create tooltip content with HTML-like styling
        string tooltipText = $"<b>Time: {timeAtMouse:F2}</b>\n";
        
        foreach (var dataset in _datasets)
        {
            if (dataset.points.Count == 0) continue;
            
            // Find closest point
            var closestPoint = dataset.points.OrderBy(p => Mathf.Abs(p.timestamp - timeAtMouse)).First();
            
            // Create colored indicator
            string colorHex = ColorUtility.ToHtmlStringRGB(dataset.color);
            tooltipText += $"<color=#{colorHex}>●</color> {dataset.name}: <b>{closestPoint.value:F2}</b>\n";
        }
        
        _tooltipLabel.text = tooltipText.TrimEnd('\n');
        _tooltipLabel.style.display = DisplayStyle.Flex;
        
        // Position tooltip to avoid edge clipping
        float tooltipX = _mousePosition.x + 15;
        float tooltipY = _mousePosition.y - 20;
        
        // Adjust if too close to right edge
        if (tooltipX + 150 > contentRect.width)
        {
            tooltipX = _mousePosition.x - 150;
        }
        
        // Adjust if too close to top edge
        if (tooltipY < 20)
        {
            tooltipY = _mousePosition.y + 20;
        }
        
        _tooltipLabel.style.left = tooltipX;
        _tooltipLabel.style.top = tooltipY;
    }
}