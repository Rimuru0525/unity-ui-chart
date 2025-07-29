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

public class ChartElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<ChartElement, UxmlTraits> { }
    
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlIntAttributeDescription m_YAxisDivisions = new UxmlIntAttributeDescription { name = "y-axis-divisions", defaultValue = 5 };
        UxmlIntAttributeDescription m_XAxisDivisions = new UxmlIntAttributeDescription { name = "x-axis-divisions", defaultValue = 10 };
        UxmlFloatAttributeDescription m_LineWidth = new UxmlFloatAttributeDescription { name = "line-width", defaultValue = 2f };
        UxmlBoolAttributeDescription m_ShowGrid = new UxmlBoolAttributeDescription { name = "show-grid", defaultValue = true };
        UxmlColorAttributeDescription m_GridColor = new UxmlColorAttributeDescription { name = "grid-color", defaultValue = new Color(0.3f, 0.3f, 0.3f, 0.3f) };
        UxmlColorAttributeDescription m_TextColor = new UxmlColorAttributeDescription { name = "text-color", defaultValue = Color.white };
        
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var chart = ve as ChartElement;
            chart.yAxisDivisions = m_YAxisDivisions.GetValueFromBag(bag, cc);
            chart.xAxisDivisions = m_XAxisDivisions.GetValueFromBag(bag, cc);
            chart.lineWidth = m_LineWidth.GetValueFromBag(bag, cc);
            chart.showGrid = m_ShowGrid.GetValueFromBag(bag, cc);
            chart.gridColor = m_GridColor.GetValueFromBag(bag, cc);
            chart.textColor = m_TextColor.GetValueFromBag(bag, cc);
        }
    }
    
    private List<ChartData> _datasets = new List<ChartData>();
    private Label _tooltipLabel;
    private VisualElement _labelsContainer;
    private Vector2 _mousePosition;
    private bool _showTooltip;
    
    // Inspector properties
    private int _yAxisDivisions = 5;
    private int _xAxisDivisions = 10;
    private float _lineWidth = 2f;
    private bool _showGrid = true;
    private Color _gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    private Color _textColor = Color.white;
    private int _textSize = 11;
    
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
    
    private float _minValue;
    private float _maxValue;
    private float _minTime;
    private float _maxTime;
    private float _padding = 50f;
    
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
        Add(_tooltipLabel);
        
        // Register mouse events
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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
            for (int i = 0; i <= _yAxisDivisions; i++)
            {
                float y = chartTop + (i / (float)_yAxisDivisions) * chartHeight;
                painter.BeginPath();
                painter.MoveTo(new Vector2(chartLeft, y));
                painter.LineTo(new Vector2(chartRight, y));
                painter.Stroke();
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
        
        // Draw datasets
        foreach (var dataset in _datasets)
        {
            if (dataset.points.Count < 2) continue;
            
            painter.strokeColor = dataset.color;
            painter.lineWidth = _lineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            
            painter.BeginPath();
            
            for (int i = 0; i < dataset.points.Count; i++)
            {
                var point = dataset.points[i];
                float x = chartLeft + ((point.timestamp - _minTime) / (_maxTime - _minTime)) * chartWidth;
                float y = chartTop + ((_maxValue - point.value) / (_maxValue - _minValue)) * chartHeight;
                
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
                float y = chartTop + ((_maxValue - point.value) / (_maxValue - _minValue)) * chartHeight;
                
                painter.BeginPath();
                painter.Arc(new Vector2(x, y), 3f, 0, 360);
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
            _showTooltip = true;
        }
        else
        {
            _showTooltip = false;
            _tooltipLabel.style.display = DisplayStyle.None;
        }
    }
    
    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        _showTooltip = false;
        _tooltipLabel.style.display = DisplayStyle.None;
    }
    
    private void UpdateTooltip()
    {
        if (_datasets.Count == 0) return;
        
        float chartLeft = _padding;
        float chartRight = contentRect.width - 20f;
        float chartTop = 20f;
        float chartBottom = contentRect.height - _padding;
        float chartWidth = chartRight - chartLeft;
        
        float normalizedX = (_mousePosition.x - chartLeft) / chartWidth;
        float timeAtMouse = _minTime + normalizedX * (_maxTime - _minTime);
        
        string tooltipText = $"Time: {timeAtMouse:F2}\n";
        
        foreach (var dataset in _datasets)
        {
            if (dataset.points.Count == 0) continue;
            
            // Find closest point
            var closestPoint = dataset.points.OrderBy(p => Mathf.Abs(p.timestamp - timeAtMouse)).First();
            tooltipText += $"{dataset.name}: {closestPoint.value:F2}\n";
        }
        
        _tooltipLabel.text = tooltipText.TrimEnd('\n');
        _tooltipLabel.style.display = DisplayStyle.Flex;
        _tooltipLabel.style.left = _mousePosition.x + 10;
        _tooltipLabel.style.top = _mousePosition.y - 20;
    }
}