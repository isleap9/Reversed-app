using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;

namespace AkariTools.App.Controls;

public class StarField : Canvas
{
    private static readonly SolidColorBrush DimmedStarBrush =
        new(Microsoft.UI.Colors.Gray);

    private readonly List<Star> _stars = new();
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private double _elapsedSeconds;
    private bool _dimForLight;

    public StarField()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += Timer_Tick;
        Loaded += (_, _) => Start();
        Unloaded += (_, _) => Stop();
    }

    public int StarCount { get; set; } = 80;
    public double StarSize { get; set; } = 2.0;
    public Brush StarColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.White);

    /// <summary>
    /// Dims the white stars for light themes so they stay subtle on bright
    /// backgrounds; restores full brightness for dark themes.
    /// </summary>
    public void ApplyLightDim(bool isLight)
    {
        _dimForLight = isLight;
        foreach (var star in _stars)
        {
            star.Element.Fill = _dimForLight ? DimmedStarBrush : StarColor;
            star.Element.Opacity = star.BaseOpacity * (_dimForLight ? 0.4 : 1.0);
        }
    }

    private void Start()
    {
        InitializeStars();
        _timer.Start();
    }

    private void Stop() => _timer.Stop();

    private void InitializeStars()
    {
        Children.Clear();
        _stars.Clear();

        double w = ActualWidth > 0 ? ActualWidth : 800;
        double h = ActualHeight > 0 ? ActualHeight : 600;

        for (int i = 0; i < StarCount; i++)
        {
            var ellipse = new Ellipse
            {
                Width = StarSize,
                Height = StarSize,
                Fill = _dimForLight ? DimmedStarBrush : StarColor,
                Opacity = _random.NextDouble() * 0.5 + 0.1
            };

            double x = _random.NextDouble() * w;
            double y = _random.NextDouble() * h;
            SetLeft(ellipse, x);
            SetTop(ellipse, y);

            _stars.Add(new Star { Element = ellipse, X = x, Y = y, BaseOpacity = ellipse.Opacity });
            Children.Add(ellipse);
        }

        if (_dimForLight)
            ApplyLightDim(true);
    }

    private void Timer_Tick(object? sender, object e)
    {
        _elapsedSeconds += _timer.Interval.TotalSeconds;

        double w = ActualWidth > 0 ? ActualWidth : 800;
        double h = ActualHeight > 0 ? ActualHeight : 600;

        double maxOpacity = _dimForLight ? 0.25 : 0.7;

        foreach (var star in _stars)
        {
            star.Y += 0.3;
            if (star.Y > h)
            {
                star.Y = 0;
                star.X = _random.NextDouble() * w;
            }

            SetLeft(star.Element, star.X);
            SetTop(star.Element, star.Y);

            // Subtle twinkle driven by a monotonic clock.
            double opacity = star.BaseOpacity + Math.Sin(_elapsedSeconds * 2.0 + star.X + star.Y) * 0.1;
            star.Element.Opacity = Math.Clamp(opacity, 0.05, maxOpacity);
        }
    }

    private sealed class Star
    {
        public required Ellipse Element { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double BaseOpacity { get; init; }
    }
}
