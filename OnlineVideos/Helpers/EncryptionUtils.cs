using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OnlineVideos.Helpers
{

    public class CRC32
    {
        private static readonly uint[] table = GenerateTable();
        private uint crc = 0xFFFFFFFF;

        public void Update(byte[] buffer)
        {
            foreach (var b in buffer)
            {
                crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
            }
        }

        public uint Value => crc ^ 0xFFFFFFFF;

        private static uint[] GenerateTable()
        {
            var t = new uint[256];
            const uint poly = 0xEDB88320;
            for (uint i = 0; i < 256; i++)
            {
                uint temp = i;
                for (int j = 0; j < 8; j++)
                {
                    temp = ((temp & 1) == 1) ? (poly ^ (temp >> 1)) : (temp >> 1);
                }
                t[i] = temp;
            }
            return t;
        }
    }

    public static class EncryptionUtils
    {
        public static string CalculateCRC32(string strLine)
        {
            if (string.IsNullOrEmpty(strLine))
            {
                return string.Empty;
            }

            CRC32 crc = new CRC32();
            crc.Update(Encoding.UTF8.GetBytes(strLine));
            return crc.Value.ToString();
        }

        public static string GetMD5Hash(string input)
        {
            using (MD5 md5Hasher = MD5.Create())
            {
                byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                // MD5 always produces 16 bytes = 32 hex characters
                var result = new StringBuilder(32);
                foreach (byte b in data)
                {
                    result.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                }

                return result.ToString();
            }
        }

        static readonly byte[] aditionalEntropy = { };
        public static string SymEncryptLocalPC(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            bytes = ProtectedData.Protect(bytes, aditionalEntropy, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(bytes);
        }

        public static string SymDecryptLocalPC(string data)
        {
            byte[] bytes = Convert.FromBase64String(data);
            bytes = ProtectedData.Unprotect(bytes, aditionalEntropy, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
