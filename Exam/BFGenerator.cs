using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace C_FinalTask
{

    // Generates all possible password combinations
    public class BFGenerator
    {
        // Characters that can be used in generated passwords
        public const string CHARSET = "abcdefghijklmnopqrstuvwxyz0123456789";

        private readonly int _maxLength;

        public BFGenerator(int maxLength = 6)
        {
            _maxLength = maxLength;
        }

        // Total number of combinations for a given length(CHARSET.Length ^ length)
        public long CombinationsForLength(int length)
        {
            long count = 1;
            for (int i = 0; i < length; i++)
                count *= CHARSET.Length;
            return count; 
        }

        public string GetCandidateAtIndex(int length, long index)
        {
            var chars = new char[length];
            long baseN = CHARSET.Length;

            for (int pos = length - 1; pos >= 0; pos--)
            {
                int digit = (int)(index % baseN);
                chars[pos] = CHARSET[digit];
                index /= baseN;
            }

            return new string(chars);
        }

        public IEnumerable<string> GetCombinations(int length)
        {
            long total = CombinationsForLength(length);
            for (long i = 0; i < total; i++)
                yield return GetCandidateAtIndex(length, i);
        }

        // Calculates the total number of combinations, used for the progress bar.
        public long TotalCombinations()
        {
            long total = 0;
            long count = CHARSET.Length;
            for (int len = 1; len <= _maxLength; len++)
            {
                total += count;
                count *= CHARSET.Length;
            }
            return total;
        }
    }
}
