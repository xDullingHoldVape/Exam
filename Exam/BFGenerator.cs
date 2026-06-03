using System.Collections.Generic;

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

        // Generates all combinations of a given lengthб example: aa, ab, ac, ..., 99
        public IEnumerable<string> GetCombinations(int length)
        {
            // Stores the current position for each character
            int[] indices = new int[length];

            while (true)
            {
                // Build the current combination
                var chars = new char[length];
                for (int i = 0; i < length; i++)
                    chars[i] = CHARSET[indices[i]];

                yield return new string(chars);

                // Move to the next combination
                int position = length - 1;
                while (position >= 0)
                {
                    indices[position]++;
                    if (indices[position] < CHARSET.Length)
                        break; // No carry needed, stop

                    // Reset current position and move left
                    indices[position] = 0;
                    position--;
                }

                // No more combinations left
                if (position < 0)
                    yield break;
            }
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
