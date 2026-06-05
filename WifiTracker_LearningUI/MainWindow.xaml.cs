using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;

namespace WifiRadar
{
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        GraphWindow graph;

        double lastA = -50;
        double lastB = -50;

        double baseline = 0;
        double sum = 0;
        int count = 0;

        const int CALIBRATION = 40;
        bool calibrated = false;

        public MainWindow()
        {
            InitializeComponent();

            graph = new GraphWindow();
            graph.Show();

            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += Update;
            timer.Start();
        }

        void AutoCalibrate_Click(object sender, RoutedEventArgs e)
        {
            calibrated = false;
            sum = 0;
            count = 0;
        }

        void Update(object sender, EventArgs e)
        {
            double A = GetRssi();
            double B = GetRssi();

            double motion = (Math.Abs(A - lastA) + Math.Abs(B - lastB)) * 2;

            lastA = A;
            lastB = B;

            // CALIBRATION
            if (!calibrated)
            {
                sum += motion;
                count++;

                StatusText.Text = $"CALIBRATING {count}/{CALIBRATION}";
                StatusText.Foreground = Brushes.Orange;

                if (count >= CALIBRATION)
                {
                    baseline = (sum / count) * 0.85;
                    calibrated = true;
                }

                graph.AddValue(motion);
                return;
            }

            // CONTROLS
            double sensitivity = SensitivitySlider.Value;
            double fine = FineTuneSlider.Value;

            double threshold = baseline + sensitivity + fine;

            graph.SetThreshold(threshold);
            graph.AddValue(motion);

            // PRESENCE %
            double presence = Math.Max(0, Math.Min(100, (motion - baseline) * 18));

            StatusText.Text = $"Presence: {presence:F0}%";

            StatusText.Foreground =
                presence > 70 ? Brushes.Lime :
                presence > 30 ? Brushes.Orange :
                Brushes.Gray;

            ParamsText.Text =
                $"Baseline: {baseline:F2}\n" +
                $"Sensitivity: {sensitivity:F2}\n" +
                $"Fine: {fine:F2}\n" +
                $"Threshold: {threshold:F2}\n" +
                $"Motion: {motion:F2}\n" +
                $"Presence: {presence:F0}%";
        }

        double GetRssi()
        {
            try
            {
                var p = new Process();
                p.StartInfo.FileName = "netsh";
                p.StartInfo.Arguments = "wlan show interfaces";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;

                p.Start();

                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var m = Regex.Match(output, @"Rssi\s*:\s*(-?\d+)");

                if (m.Success)
                    return double.Parse(m.Groups[1].Value);
            }
            catch { }

            return -50;
        }
    }
}