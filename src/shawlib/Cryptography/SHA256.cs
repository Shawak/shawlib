using System;
using System.IO;
using System.Security.Cryptography;

namespace ShawLib.Cryptography
{
    public static class SHA256
    {
        public static string Hash(IConvertible val)
        {
            return Hash(val.GetBytes()).To<string>();
        }

        public static byte[] Hash(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
                return Hash(stream).GetBytes();
        }

        public static string Hash(Stream stream)
        {
            using (var sha256 = SHA256CryptoServiceProvider.Create())
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLower();
        }
    }
}
