using System;
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

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = ThreadCount,
                CancellationToken = token
            };

            // Try every length from 1 up to 6
            for (int length = 1; length <= 6 && foundPassword == null && !token.IsCancellationRequested; length++)
            {
                long total = _generator.CombinationsForLength(length);
                int currentLength = length;


                try
                {
                    Parallel.For(0, total, parallelOptions,
                        () => 0L, // thread-local seed: local counter
                        (i, loopState, localCount) =>
                        {
                            if (foundPassword != null)
                            {
                                loopState.Stop();
                                return localCount;
                            }

                            string candidate = _generator.GetCandidateAtIndex(currentLength, i);

                            if (_validator.IsMatch(candidate))
                            {
                                foundPassword = candidate;
                                loopState.Stop();
                                return localCount;
                            }

                            localCount++;

                            if (localCount >= 1000)
                            {
                                long total2 = Interlocked.Add(ref _checkedCount, localCount);
                                OnProgress?.Invoke(total2, totalCombinations);
                                localCount = 0;
                            }

                            return localCount;
                        },
                        (localCount) =>
                        {
                            if (localCount > 0)
                                Interlocked.Add(ref _checkedCount, localCount);
                        });

                }

                catch (OperationCanceledException) { }

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
                long total = generator.CombinationsForLength(length);
                for (long i = 0; i < total; i++)

                {
                    string candidate = generator.GetCandidateAtIndex(length, i);

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
    }
}
