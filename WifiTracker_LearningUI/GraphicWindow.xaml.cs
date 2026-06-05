using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace WifiRadar
{
    public partial class GraphWindow : Window
    {
        // ---------------- DATA ----------------
        List<(double v, double t)> data = new();
        List<(double t, double v)> peaks = new();
        List<(double t, double v)> spikes = new();

        double threshold = 0;

        // ---------------- PAPER ROLL CONFIG ----------------
        double startTime;
        double pixelsPerSecond = 20;   // scrolling speed
        double timeWindow = 60;         // visible seconds window

        // ---------------- SMOOTH SCALE ----------------
        double lastScale = 10;

        public GraphWindow()
        {
            InitializeComponent();
            startTime = Environment.TickCount;
        }

        // ---------------- API ----------------
        public void AddValue(double v)
        {
            double t = (Environment.TickCount - startTime) / 1000.0;

            data.Add((v, t));

            if (data.Count > 500)
                data.RemoveAt(0);

            DetectPeaks();
            DetectSpikes();
            Draw();
        }

        public void SetThreshold(double t)
        {
            threshold = t;
        }

        // ---------------- ANALYSIS ----------------
        void DetectPeaks()
        {
            peaks.Clear();

            for (int i = 1; i < data.Count - 1; i++)
            {
                double prev = data[i - 1].v;
                double curr = data[i].v;
                double next = data[i + 1].v;

                if (curr > prev && curr > next && curr > threshold)
                    peaks.Add((data[i].t, curr));
            }
        }

        void DetectSpikes()
        {
            spikes.Clear();

            for (int i = 1; i < data.Count; i++)
            {
                double diff = Math.Abs(data[i].v - data[i - 1].v);

                if (diff > 3.0)
                    spikes.Add((data[i].t, data[i].v));
            }
        }

        // ---------------- STABLE AUTO SCALE ----------------
        double GetAutoScale()
        {
            if (data.Count < 2) return 10;

            double max = 1;

            foreach (var p in data)
                if (p.v > max)
                    max = p.v;

            return Math.Max(8, max * 1.1);
        }

        double SmoothScale(double newScale)
        {
            lastScale = lastScale * 0.85 + newScale * 0.15;
            return lastScale;
        }

        // ---------------- DRAW ----------------
        void Draw()
        {
            GraphCanvas.Children.Clear();

            double w = GraphCanvas.ActualWidth;
            double h = GraphCanvas.ActualHeight;

            if (w == 0 || h == 0) return;

            double zoom = SmoothScale(GetAutoScale());

            double paddingLeft = 40;
            double paddingBottom = 25;

            double now = data.Count > 0 ? data[^1].t : 0;

            DrawAxesPaper(w, h, zoom, now);

            // ---------------- THRESHOLD ----------------
            double ty = h - paddingBottom - (threshold / zoom) * (h * 0.6);

            GraphCanvas.Children.Add(new Line
            {
                X1 = paddingLeft,
                X2 = w,
                Y1 = ty,
                Y2 = ty,
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            });

            // ---------------- SIGNAL ----------------
            Polyline line = new Polyline
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 2
            };

            foreach (var p in data)
            {
                double age = now - p.t;

                if (age > timeWindow)
                    continue;

                double x = w - (age * pixelsPerSecond);
                double y = h - paddingBottom - (p.v / zoom) * (h * 0.6);

                line.Points.Add(new Point(x, y));
            }

            GraphCanvas.Children.Add(line);

            // ---------------- PEAKS ----------------
            foreach (var p in peaks)
            {
                double age = now - p.t;
                if (age > timeWindow) continue;

                DrawMarker(w, h, zoom, p.t, p.v, now, Brushes.Orange, "▲");
            }

            // ---------------- SPIKES ----------------
            foreach (var s in spikes)
            {
                double age = now - s.t;
                if (age > timeWindow) continue;

                DrawMarker(w, h, zoom, s.t, s.v, now, Brushes.Red, "⚡");
            }
        }

        // ---------------- PAPER AXIS ----------------
        void DrawAxesPaper(double w, double h, double zoom, double now)
        {
            double paddingLeft = 40;
            double paddingBottom = 25;

            int bars = 6;
            double stepSeconds = timeWindow / bars; // 60 / 6 = 10s per bar

            for (int i = 0; i <= bars; i++)
            {
                double secondsAgo = i * stepSeconds;

                double x = w - (secondsAgo * pixelsPerSecond);

                GraphCanvas.Children.Add(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = h - 25,
                    Stroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255))
                });

                GraphCanvas.Children.Add(new TextBlock
                {
                    Text = $"{secondsAgo:0}s",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(x, h - 20, 0, 0)
                });
            }
            // Y axis grid
            for (int i = 0; i <= 5; i++)
            {
                double val = i * 5;
                double y = h - paddingBottom - (val / zoom) * (h * 0.6);

                GraphCanvas.Children.Add(new Line
                {
                    X1 = paddingLeft,
                    X2 = w,
                    Y1 = y,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
                });

                GraphCanvas.Children.Add(new TextBlock
                {
                    Text = val.ToString("0"),
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(5, y - 8, 0, 0)
                });
            }

            // X axis (time)
            for (int i = 0; i <= 6; i++)
            {
                double age = i;
                double x = w - (age * pixelsPerSecond);

                GraphCanvas.Children.Add(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = h - paddingBottom,
                    Stroke = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255))
                });

                GraphCanvas.Children.Add(new TextBlock
                {
                    Text = $"{i}s",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(x, h - 20, 0, 0)
                });
            }

            // base axes
            GraphCanvas.Children.Add(new Line
            {
                X1 = paddingLeft,
                X2 = paddingLeft,
                Y1 = 0,
                Y2 = h - paddingBottom,
                Stroke = Brushes.White,
                StrokeThickness = 1
            });

            GraphCanvas.Children.Add(new Line
            {
                X1 = paddingLeft,
                X2 = w,
                Y1 = h - paddingBottom,
                Y2 = h - paddingBottom,
                Stroke = Brushes.White,
                StrokeThickness = 1
            });
        }

        // ---------------- MARKERS ----------------
        void DrawMarker(double w, double h, double zoom, double t, double v, double now, Brush color, string symbol)
        {
            double paddingBottom = 25;

            double age = now - t;

            double x = w - (age * pixelsPerSecond);
            double y = h - paddingBottom - (v / zoom) * (h * 0.6);

            TextBlock marker = new TextBlock
            {
                Text = symbol,
                Foreground = color,
                FontSize = 14
            };

            Canvas.SetLeft(marker, x - 5);
            Canvas.SetTop(marker, y - 10);

            GraphCanvas.Children.Add(marker);
        }
    }
}