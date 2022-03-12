using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmmoLocker
{
    public interface IProgress
    {
        public Task<T> Step<T>(Task<T> step, double weight = 1);
    }

    public class Progress : IProgress
    {
        private double weightExpected;
        private double weightCompleted;

        public delegate void ProgressHandler(double progress);

        public event ProgressHandler OnProgress;

        public Progress()
        {
            this.weightExpected = 0d;
            this.weightCompleted = 0d;
        }

        private void Expect(double weight)
        {
            Interlocking.Add(ref weightExpected, weight);
        }

        private void Complete(double weight)
        {
            var completed = Interlocking.Add(ref weightCompleted, weight);
            if (weightExpected > 0f)
            {
                OnProgress(completed / weightExpected);
            }
        }

        public async Task<T> Step<T>(Task<T> step, double weight = 1)
        {
            Expect(weight);
            var result = await step;
            Complete(weight);
            return result;
        }
    }

    public static class ProgressExtensions
    {
        public static async Task<T> SelectMany<T,W>(this Task<W> first, Func<W, Task<T>> then)
        {
            var w = await first;
            var t = await then(w);
            return t;
        }

        public static Task<T> After<W, T>(this IProgress progress, Task<W> first, Func<W, Task<T>> then, double weight=1)
        {
            return progress.Step<T>(first.SelectMany(then), weight);
        }
    }

    public static class Interlocking
    {
        public delegate T CompareExchanger<T>(ref T location, T value, T comparand);

        public static T AtomicModify<T>(CompareExchanger<T> compareExchange, ref T location, Func<T, T> update) where T : IEquatable<T>
        {
            var previous = location;
            while (true)
            {
                var updated = update(previous);
                var observed = compareExchange(ref location, updated, previous);
                if (observed.Equals(previous))
                {
                    return updated;
                }
                previous = observed;
            }
        }

        public static double Add(ref double location, double value)
        {
            return AtomicModify(Interlocked.CompareExchange, ref location, x => x + value);
        }
    }
}