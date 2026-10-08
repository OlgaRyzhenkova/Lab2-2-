using System;
using System.Collections.Generic;
using System.Threading;

namespace Lab2
{
    // Результат потоку для графіка
    public class CalcResult
    {
        public int Number { get; set; }
        public string Equation { get; set; }
        public double Root { get; set; }
        public List<double> Steps { get; set; }
        public int ThreadId { get; set; }
    }

    // Названо не Thread, щоб не збігалось з System.Threading.Thread
    public static class FixedPointCalculator
    {
        public static double e;                    // похибка

        public delegate double MyFunc(double x);

        // Вивід у вікно (за замовчуванням - консоль)
        public static Action<string> Log = delegate (string s) { Console.WriteLine(s); };
        public static Action<CalcResult> OnResult = null;

        static bool done;
        static object locker = new object();

        static void Go()
        {
            lock (locker)
            {
                if (!done)
                {
                    Log("Done");
                    done = true;
                }
            }
        }

        public static double g1(double x)
        {
            double y = 0.5 * Math.Cos(x);
            return y;
        }

        public static double g2(double x)
        {
            double y = (2 * x - Math.Log(x)) / 3;
            return y;
        }

        public static double calc(double x, MyFunc g)
        {
            return calc(x, g, null);
        }

        // steps - наближення для графіка
        public static double calc(double x, MyFunc g, List<double> steps)
        {
            double temp, d;
            if (steps != null) steps.Add(x);
            do
            {
                temp = g(x);
                d = Math.Abs(temp - x);
                x = temp;
                if (steps != null) steps.Add(x);
            }
            while (d >= e);
            return (x);
        }

        static void ThreadFunction1()
        {
            double x = 0.5;
            MyFunc g = g1;
            List<double> steps = new List<double>();
            x = calc(x, g, steps);
            // Одне повідомлення, щоб рядки потоків не змішувались
            Log("Education 2x - cos (x) = 0" + Environment.NewLine +
                string.Format("Answer: X= {0}, iquel {1}", x, e));
            Report(1, "2x - cos(x) = 0", x, steps);
        }

        static void ThreadFunction2()
        {
            double x = 0.75;
            MyFunc g = g2;
            List<double> steps = new List<double>();
            x = calc(x, g, steps);
            Log("Education x + ln (x) = 0" + Environment.NewLine +
                string.Format("Answer: X= {0}, iquel {1}", x, e));
            Report(2, "x + ln(x) = 0", x, steps);
        }

        static void Report(int number, string equation, double root, List<double> steps)
        {
            if (OnResult == null) return;
            CalcResult r = new CalcResult();
            r.Number = number;
            r.Equation = equation;
            r.Root = root;
            r.Steps = steps;
            r.ThreadId = Thread.CurrentThread.ManagedThreadId;
            OnResult(r);
        }

        // Аналог Main() з методички
        public static void RunAll()
        {
            done = false;

            Thread thread1 = new Thread(ThreadFunction1);
            thread1.Start();

            Thread thread2 = new Thread(ThreadFunction2);
            thread2.Start();

            Thread t = new Thread(Go);
            t.Start();
            Go();                                    // Go() і в поточному потоці

            Thread.Sleep(TimeSpan.FromSeconds(3));
            t.Join();
            thread1.Join();
            thread2.Join();
        }
    }
}