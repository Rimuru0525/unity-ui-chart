using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ChartDebugger : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private int maxDataPoints = 50;
    
    [Header("Dataset 1")]
    [SerializeField] private bool enableDataset1 = true;
    [SerializeField] private Color dataset1Color = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private float dataset1Amplitude = 10f;
    [SerializeField] private float dataset1Frequency = 1f;
    
    [Header("Dataset 2")]
    [SerializeField] private bool enableDataset2 = true;
    [SerializeField] private Color dataset2Color = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private float dataset2Amplitude = 15f;
    [SerializeField] private float dataset2Frequency = 0.5f;
    
    private ChartElement _chartElement;
    private ChartData _dataset1;
    private ChartData _dataset2;
    private float _startTime;
    private float _lastUpdateTime;
    
    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is not assigned!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        _chartElement = root.Q<ChartElement>("chart");
        
        if (_chartElement == null)
        {
            Debug.LogError("ChartElement with name 'chart' not found in UIDocument!");
            return;
        }
        
        _startTime = Time.time;
        
        // Initialize datasets
        _dataset1 = new ChartData
        {
            name = "Dataset 1",
            color = dataset1Color,
            points = new List<DataPoint>()
        };
        
        _dataset2 = new ChartData
        {
            name = "Dataset 2",
            color = dataset2Color,
            points = new List<DataPoint>()
        };
        
        if (enableDataset1) _chartElement.AddDataset(_dataset1);
        if (enableDataset2) _chartElement.AddDataset(_dataset2);
    }
    
    void Update()
    {
        if (_chartElement == null) return;
        
        if (Time.time - _lastUpdateTime >= updateInterval)
        {
            _lastUpdateTime = Time.time;
            float currentTime = Time.time - _startTime;
            
            // Generate new data points
            if (enableDataset1)
            {
                float value1 = Mathf.Sin(currentTime * dataset1Frequency) * dataset1Amplitude + 
                              Random.Range(-1f, 1f) * 2f;
                _dataset1.points.Add(new DataPoint(currentTime, value1));
                
                // Remove old points
                while (_dataset1.points.Count > maxDataPoints)
                {
                    _dataset1.points.RemoveAt(0);
                }
            }
            
            if (enableDataset2)
            {
                float value2 = Mathf.Cos(currentTime * dataset2Frequency) * dataset2Amplitude + 
                              Random.Range(-1f, 1f) * 3f;
                _dataset2.points.Add(new DataPoint(currentTime, value2));
                
                // Remove old points
                while (_dataset2.points.Count > maxDataPoints)
                {
                    _dataset2.points.RemoveAt(0);
                }
            }
            
            // Update chart
            _chartElement.ClearDatasets();
            if (enableDataset1) _chartElement.AddDataset(_dataset1);
            if (enableDataset2) _chartElement.AddDataset(_dataset2);
        }
    }
    
    void OnValidate()
    {
        if (_chartElement != null && Application.isPlaying)
        {
            // Update colors
            if (_dataset1 != null) _dataset1.color = dataset1Color;
            if (_dataset2 != null) _dataset2.color = dataset2Color;
            
            // Refresh chart
            _chartElement.ClearDatasets();
            if (enableDataset1 && _dataset1 != null) _chartElement.AddDataset(_dataset1);
            if (enableDataset2 && _dataset2 != null) _chartElement.AddDataset(_dataset2);
        }
    }
}