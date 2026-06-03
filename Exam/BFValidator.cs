namespace C_FinalTask
{
    // Validates generated passwords

    public class BFValidator
    {
        // The hash we are trying to crack (set once at the start of the attack)
        private readonly string _targetHash;

        public BFValidator(string targetHash)
        {
            _targetHash = targetHash;
        }

        // Returns true if the password matches the target hash
        public bool IsMatch(string candidate)
        {
            string candidateHash = PasswordManager.HashPassword(candidate);
            return candidateHash == _targetHash;
        }
    }
}
