using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab2
{
    public partial class MainWindow : Window
    {
        // Результати двох потоків (для графіка)
        private readonly CalcResult[] results = new CalcResult[2];

        // Кольори ліній: синій і помаранчевий
        private static readonly Brush[] LineColors = { Hex("#2563EB"), Hex("#F97316") };

        public MainWindow()
        {
            InitializeComponent();
        }

        private static Brush Hex(string color)
        {
            return (Brush)new BrushConverter().ConvertFrom(color);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Приймаємо і "0,01", і "0.01" незалежно від мови Windows
            double eps;
            string text = EpsilonBox.Text.Trim().Replace(',', '.');
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out eps)
                || eps < 1e-10)
            {
                MessageBox.Show("Введіть додатне число для похибки (не менше 0,0000000001).");
                return;
            }

            // Скидаємо попередній результат
            results[0] = null;
            results[1] = null;
            Root1Text.Text = "X = ...";
            Root2Text.Text = "X = ...";
            Info1Text.Text = "обчислення";
            Info2Text.Text = "обчислення";
            OutputBox.Clear();
            DrawChart();
            StartButton.IsEnabled = false;

            // Налаштування коду з методички
            FixedPointCalculator.e = eps;
            FixedPointCalculator.Log = Log;
            FixedPointCalculator.OnResult = OnResult;

            // Запускаємо в окремому потоці, щоб вікно не "зависало" (Sleep і Join блокують)
            Thread worker = new Thread(delegate ()
            {
                FixedPointCalculator.RunAll();
                Dispatcher.Invoke(new Action(delegate () { StartButton.IsEnabled = true; }));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        // Потоки не можуть напряму змінювати елементи вікна - тому Dispatcher
        private void Log(string text)
        {
            Dispatcher.Invoke(new Action(delegate ()
            {
                OutputBox.AppendText(text + Environment.NewLine);
                OutputBox.ScrollToEnd();
            }));
        }

        // Викликається з потоку, коли він завершив обчислення
        private void OnResult(CalcResult r)
        {
            Dispatcher.Invoke(new Action(delegate ()
            {
                results[r.Number - 1] = r;

                string info = string.Format("Ітерацій: {0},  потік №{1},  похибка {2}",
                                            r.Steps.Count - 1, r.ThreadId, FixedPointCalculator.e);
                string root = string.Format("X = {0:F6}", r.Root);

                if (r.Number == 1) { Root1Text.Text = root; Info1Text.Text = info; }
                else { Root2Text.Text = root; Info2Text.Text = info; }

                DrawChart();
            }));
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart();
        }

        // Малюємо графік: по горизонталі - номер ітерації, по вертикалі - значення x
        private void DrawChart()
        {
            ChartCanvas.Children.Clear();

            double w = ChartCanvas.ActualWidth;
            double h = ChartCanvas.ActualHeight;
            if (w < 100 || h < 80) return;

            double left = 45, right = 15, top = 10, bottom = 28;
            double pw = w - left - right;   // ширина області графіка
            double ph = h - top - bottom;   // висота області графіка

            // Горизонтальні лінії сітки і підписи осі Y (0 ... 1)
            for (int i = 0; i <= 4; i++)
            {
                double value = i * 0.25;
                double y = top + ph * (1 - value);

                ChartCanvas.Children.Add(new Line
                {
                    X1 = left,
                    X2 = left + pw,
                    Y1 = y,
                    Y2 = y,
                    Stroke = Hex("#E5E7EB"),
                    StrokeThickness = 1
                });
                AddLabel(value.ToString("0.00"), 6, y - 9);
            }

            // Скільки ітерацій на графіку (за найдовшим потоком)
            int maxSteps = 1;
            foreach (CalcResult r in results)
                if (r != null) maxSteps = Math.Max(maxSteps, r.Steps.Count - 1);

            AddLabel("0", left - 3, top + ph + 4);
            AddLabel(maxSteps.ToString(), left + pw - 6, top + ph + 4);
            AddLabel("ітерація", left + pw / 2 - 25, top + ph + 4);

            // Лінії обох потоків
            for (int n = 0; n < 2; n++)
            {
                CalcResult r = results[n];
                if (r == null) continue;

                Polyline line = new Polyline { Stroke = LineColors[n], StrokeThickness = 2 };

                for (int k = 0; k < r.Steps.Count; k++)
                {
                    double x = left + pw * k / maxSteps;
                    double v = Math.Max(0, Math.Min(1, r.Steps[k]));
                    double y = top + ph * (1 - v);

                    line.Points.Add(new Point(x, y));

                    Ellipse dot = new Ellipse { Width = 7, Height = 7, Fill = LineColors[n] };
                    Canvas.SetLeft(dot, x - 3.5);
                    Canvas.SetTop(dot, y - 3.5);
                    ChartCanvas.Children.Add(dot);
                }
                ChartCanvas.Children.Insert(0, line);   // лінія під точками
            }
        }

        private void AddLabel(string text, double x, double y)
        {
            TextBlock label = new TextBlock { Text = text, FontSize = 11, Foreground = Hex("#6B7280") };
            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);
            ChartCanvas.Children.Add(label);
        }
    }
}