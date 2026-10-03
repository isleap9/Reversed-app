using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using VainTools.Models;

namespace VainTools.App.Controls;

/// <summary>
/// Editor control for GPU fan curve points.
/// Provides visual curve representation and inline editing of temperature/speed pairs.
/// </summary>
public sealed partial class FanCurveEditor : UserControl
{
    /// <summary>
    /// The collection of fan curve points being edited.
    /// </summary>
    public ObservableCollection<NvFanCurvePoint> FanCurvePoints
    {
        get => (ObservableCollection<NvFanCurvePoint>)GetValue(FanCurvePointsProperty);
        set => SetValue(FanCurvePointsProperty, value);
    }

    public static readonly DependencyProperty FanCurvePointsProperty =
        DependencyProperty.Register(
            nameof(FanCurvePoints),
            typeof(ObservableCollection<NvFanCurvePoint>),
            typeof(FanCurveEditor),
            new PropertyMetadata(null, OnFanCurvePointsChanged));

    private const double CanvasPadding = 30;
    private const double MinTemp = 0;
    private const double MaxTemp = 100;
    private const double MinSpeed = 0;
    private const double MaxSpeed = 100;

    public FanCurveEditor()
    {
        this.InitializeComponent();
        FanCurvePoints = new ObservableCollection<NvFanCurvePoint>();
        PointsItemsControl.ItemsSource = FanCurvePoints;
    }

    private static void OnFanCurvePointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (FanCurveEditor)d;
        if (e.OldValue is ObservableCollection<NvFanCurvePoint> oldColl)
        {
            oldColl.CollectionChanged -= editor.OnCollectionChanged;
        }
        if (e.NewValue is ObservableCollection<NvFanCurvePoint> newColl)
        {
            newColl.CollectionChanged += editor.OnCollectionChanged;
        }
        editor.RefreshCurve();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshCurve();
    }

    /// <summary>
    /// Adds a new fan curve point with default values.
    /// </summary>
    private void AddPointButton_Click(object sender, RoutedEventArgs e)
    {
        int temp = FanCurvePoints.Count > 0
            ? Math.Min(FanCurvePoints.Max(p => p.Temperature) + 10, 100)
            : 30;
        int speed = FanCurvePoints.Count > 0
            ? Math.Min(FanCurvePoints.Max(p => p.SpeedPercent) + 10, 100)
            : 50;

        FanCurvePoints.Add(new NvFanCurvePoint
        {
            Temperature = temp,
            SpeedPercent = speed
        });
    }

    /// <summary>
    /// Removes the specified fan curve point.
    /// </summary>
    private void RemovePointButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is NvFanCurvePoint point)
        {
            FanCurvePoints.Remove(point);
        }
    }

    /// <summary>
    /// Redraws the curve visualization when the canvas size changes.
    /// </summary>
    private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RefreshCurve();
    }

    /// <summary>
    /// Redraws the fan curve visualization on the canvas.
    /// </summary>
    private void RefreshCurve()
    {
        CurveCanvas.Children.Clear();

        if (FanCurvePoints == null || FanCurvePoints.Count == 0)
        {
            NoPointsTextBlock.Visibility = Visibility.Visible;
            return;
        }

        NoPointsTextBlock.Visibility = Visibility.Collapsed;

        double width = CurveCanvas.ActualWidth;
        double height = CurveCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        double drawWidth = width - 2 * CanvasPadding;
        double drawHeight = height - 2 * CanvasPadding;

        // Draw axes
        var xAxis = new Line
        {
            X1 = CanvasPadding,
            Y1 = height - CanvasPadding,
            X2 = width - CanvasPadding,
            Y2 = height - CanvasPadding,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            StrokeThickness = 1
        };
        CurveCanvas.Children.Add(xAxis);

        var yAxis = new Line
        {
            X1 = CanvasPadding,
            Y1 = CanvasPadding,
            X2 = CanvasPadding,
            Y2 = height - CanvasPadding,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            StrokeThickness = 1
        };
        CurveCanvas.Children.Add(yAxis);

        // Sort points by temperature for drawing
        var sortedPoints = FanCurvePoints.OrderBy(p => p.Temperature).ToList();

        // Draw the curve as connected line segments
        for (int i = 0; i < sortedPoints.Count - 1; i++)
        {
            var p1 = sortedPoints[i];
            var p2 = sortedPoints[i + 1];

            double x1 = CanvasPadding + ((p1.Temperature - MinTemp) / (MaxTemp - MinTemp)) * drawWidth;
            double y1 = height - CanvasPadding - ((p1.SpeedPercent - MinSpeed) / (MaxSpeed - MinSpeed)) * drawHeight;
            double x2 = CanvasPadding + ((p2.Temperature - MinTemp) / (MaxTemp - MinTemp)) * drawWidth;
            double y2 = height - CanvasPadding - ((p2.SpeedPercent - MinSpeed) / (MaxSpeed - MinSpeed)) * drawHeight;

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.Orange),
                StrokeThickness = 2
            };
            CurveCanvas.Children.Add(line);
        }

        // Draw points as circles
        foreach (var point in sortedPoints)
        {
            double x = CanvasPadding + ((point.Temperature - MinTemp) / (MaxTemp - MinTemp)) * drawWidth;
            double y = height - CanvasPadding - ((point.SpeedPercent - MinSpeed) / (MaxSpeed - MinSpeed)) * drawHeight;

            var ellipse = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange),
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, x - 4);
            Canvas.SetTop(ellipse, y - 4);
            CurveCanvas.Children.Add(ellipse);
        }
    }
}
