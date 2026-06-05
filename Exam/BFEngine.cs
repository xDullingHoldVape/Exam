using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace C_FinalTask
{
    // This class handles the brute-force attack, using several threads
    public class BFEngine
    {
        // For using field instead of local variable
        private long _checkedCount = 0;
        public long CheckedCount => _checkedCount;
        public long TotalCombinations() => _generator.TotalCombinations();


        // Use all processor cores except one, so the application interface stays responsive.
        public static readonly int ThreadCount = Math.Max(1, Environment.ProcessorCount - 1);

        private readonly BFValidator _validator;
        private readonly BFGenerator _generator;

        // Used to stop all running threads when needed.
        private CancellationTokenSource _cts;

        // Events used to send information back to the UI.
        public Action<long, long> OnProgress;   // Checked passwords, total passwords
        public Action<string, double> OnFound; // Found password, elapsed time

        public BFEngine(string targetHash)
        {
            _validator = new BFValidator(targetHash);
            _generator = new BFGenerator(maxLength: 6);
        }

        // Starts the brute-force attack using multiple threads.
        public void StartMultiThread()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            long totalCombinations = _generator.TotalCombinations();
            _checkedCount = 0;

            // Stores the password once it is found.
            string foundPassword = null;

            var stopwatch = Stopwatch.StartNew();

            // Try every length from 1 up to 6
            for (int length = 1; length <= 6 && foundPassword == null && !token.IsCancellationRequested; length++)
            {
                // Generate all possible passwords of the current length.
                var allCombinations = new List<string>(_generator.GetCombinations(length));

                // Divide the work between threads.
                var chunks = SplitIntoChunks(allCombinations, ThreadCount);

                // Create one Task per chunk (tasks run on thread-pool threads)
                var tasks = new Task[chunks.Count];

                for (int t = 0; t < chunks.Count; t++)
                {
                    var chunk = chunks[t]; // Capture loop variable for the closure

                    tasks[t] = Task.Run(() =>
                    {
                        long localCount = 0;

                        foreach (var candidate in chunk)
                        {
                            if (token.IsCancellationRequested || foundPassword != null)
                                return;

                            if (_validator.IsMatch(candidate))
                            {
                                foundPassword = candidate;
                                _cts.Cancel();
                                return;
                            }

                            localCount++;

                            if (localCount % 500 == 0)
                            {
                                Interlocked.Add(ref _checkedCount, localCount);
                                OnProgress?.Invoke(_checkedCount, totalCombinations);
                                localCount = 0;
                            }
                        }
                        if (localCount > 0)
                            Interlocked.Add(ref _checkedCount, localCount);

                    }, token);
                }

                // Wait until all threads finish their work
                try { Task.WaitAll(tasks); }
                catch (AggregateException) { }
            }

            stopwatch.Stop();

            // Send the result back to the UI
            OnFound?.Invoke(foundPassword, stopwatch.Elapsed.TotalSeconds);
        }

        // Runs the attack using only one thread, used for comparing performance.
        public double RunSingleThread(string targetHash)
        {
            var validator = new BFValidator(targetHash);
            var generator = new BFGenerator(maxLength: 6);
            var stopwatch = Stopwatch.StartNew();

            for (int length = 1; length <= 6; length++)
            {
                foreach (var candidate in generator.GetCombinations(length))
                {
                    if (validator.IsMatch(candidate))
                    {
                        stopwatch.Stop();
                        return stopwatch.Elapsed.TotalSeconds;
                    }
                }
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalSeconds;
        }

        // Stops the brute-force attack
        public void Stop()
        {
            _cts?.Cancel();
        }

        // Splits a list into smaller parts for each thread
        private List<List<string>> SplitIntoChunks(List<string> list, int chunkCount)
        {
            var chunks = new List<List<string>>();
            int chunkSize = (int)Math.Ceiling(list.Count / (double)chunkCount);

            for (int i = 0; i < list.Count; i += chunkSize)
            {
                int end = Math.Min(i + chunkSize, list.Count);
                chunks.Add(list.GetRange(i, end - i));
            }

            return chunks;
        }
    }
}
