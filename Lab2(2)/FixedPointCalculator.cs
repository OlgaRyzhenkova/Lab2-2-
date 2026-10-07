using System;
using System.Collections.Generic;
using System.Threading;

namespace Lab2
{
    /// <summary>Результат роботи одного потоку (потрібен лише для графіка у вікні).</summary>
    public class CalcResult
    {
        public int Number { get; set; }            // номер рівняння: 1 або 2
        public string Equation { get; set; }       // текст рівняння
        public double Root { get; set; }           // знайдений корінь
        public List<double> Steps { get; set; }    // усі наближення (для графіка)
        public int ThreadId { get; set; }          // номер потоку, який рахував
    }

    /// <summary>
    /// Код з методичних вказівок (лабораторна робота 2).
    /// Клас названо FixedPointCalculator, а не Thread, як у методичці, бо ім'я Thread
    /// збігається з System.Threading.Thread і код не компілюється.
    /// Сумісний з C# 4 / Visual Studio 2010.
    /// </summary>
    public static class FixedPointCalculator
    {
        // Похибка у розрахунках (методичка: public static double e)
        public static double e;

        // Делегат для передачі необхідного методу (g1 та g2) до методу calc()
        public delegate double MyFunc(double x);

        // Замість Console.WriteLine: у WPF-вікні консолі немає, тому вивід іде через ці дії.
        // За замовчуванням (без вікна) пише в консоль, як у методичці.
        public static Action<string> Log = delegate (string s) { Console.WriteLine(s); };
        public static Action<CalcResult> OnResult = null;

        // --- Go(): потокова безпека (методичка, п. 2) ---
        static bool done;                          // статичний тип
        static object locker = new object();       // статичний об'єкт

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

        // --- Функції ітерації (методичка, п. 3) ---

        // Реалізація функції f(x) = 2x - cos (x)
        public static double g1(double x)
        {
            double y = 0.5 * Math.Cos(x);
            return y;
        }

        // Реалізація функції f(x) = x + ln x
        public static double g2(double x)
        {
            double y = (2 * x - Math.Log(x)) / 3;
            return y;
        }

        // Основний метод з розрахунками - точно як у методичці
        public static double calc(double x, MyFunc g)
        {
            return calc(x, g, null);
        }

        // Те саме, але ще й записує всі наближення (steps) для графіка
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

        // Організація потоків для функцій
        static void ThreadFunction1()
        {
            double x = 0.5;
            MyFunc g = g1;
            List<double> steps = new List<double>();
            x = calc(x, g, steps);
            // одне повідомлення на потік - рядки різних потоків не перемішуються
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

        // Передача результату у вікно (для графіка)
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

        // --- Те, що в методичці виконує Main() (п. 1) ---
        public static void RunAll()
        {
            done = false;

            // Створюємо перший потік
            Thread thread1 = new Thread(ThreadFunction1);
            thread1.Start();

            // Створюємо другий потік
            Thread thread2 = new Thread(ThreadFunction2);
            thread2.Start();

            // Go() - статичний метод
            Thread t = new Thread(Go);
            // Виконати Go() в новому потоці
            t.Start();
            // Одночасно запустити Go() в головному потоці
            Go();

            Thread.Sleep(TimeSpan.FromSeconds(3));   // блокування на 3 сек.
            t.Join();                                // очікуємо завершення потоку

            // Додатково: дочекатися і потоків обчислення (на практиці вони завершуються набагато раніше за 3 с)
            thread1.Join();
            thread2.Join();
        }
    }
}