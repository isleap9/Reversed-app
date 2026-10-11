using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;

namespace AkariTools.App.Controls;

public class StarField : Canvas
{
    private readonly List<Star> _stars = new();
    private readonly DispatcherTimer _timer;

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

        var rnd = new Random();
        double w = ActualWidth > 0 ? ActualWidth : 800;
        double h = ActualHeight > 0 ? ActualHeight : 600;

        for (int i = 0; i < StarCount; i++)
        {
            var ellipse = new Ellipse
            {
                Width = StarSize,
                Height = StarSize,
                Fill = StarColor,
                Opacity = rnd.NextDouble() * 0.5 + 0.1
            };

            double x = rnd.NextDouble() * w;
            double y = rnd.NextDouble() * h;
            SetLeft(ellipse, x);
            SetTop(ellipse, y);

            _stars.Add(new Star { Element = ellipse, X = x, Y = y, BaseOpacity = ellipse.Opacity });
            Children.Add(ellipse);
        }
    }

    private void Timer_Tick(object? sender, object e)
    {
        double w = ActualWidth > 0 ? ActualWidth : 800;
        double h = ActualHeight > 0 ? ActualHeight : 600;
        var rnd = new Random();

        foreach (var star in _stars)
        {
            star.Y += 0.3;
            if (star.Y > h)
            {
                star.Y = 0;
                star.X = rnd.NextDouble() * w;
            }

            SetLeft(star.Element, star.X);
            SetTop(star.Element, star.Y);

            // Subtle twinkle
            double opacity = star.BaseOpacity + Math.Sin(DateTime.Now.Ticks / 1000.0 + star.X) * 0.1;
            star.Element.Opacity = Math.Clamp(opacity, 0.05, 0.7);
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
