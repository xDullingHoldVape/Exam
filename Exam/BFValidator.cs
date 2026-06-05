namespace C_FinalTask
{
    public class BFValidator
    {
        private readonly byte[] _targetHashBytes;

        public BFValidator(string targetHash)
        {
            _targetHashBytes = ConvertHexToBytes(targetHash);
        }

        public bool IsMatch(string candidate)
        {
            byte[] candidateHash = PasswordManager.HashPasswordBytes(candidate);
            return ByteArrayEquals(_targetHashBytes, candidateHash);
        }

        private bool ByteArrayEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private byte[] ConvertHexToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            return bytes;
        }
    }
}
