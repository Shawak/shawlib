using System.IO;
using System.Security.Cryptography;

namespace ShawLib.Cryptography
{
    public static class Rinjdael
    {
        public static byte[] RijndaelSaltBytes = "3x=*#_d:1%dlDx:&".GetBytes();
        public static int rijndaelIterations = 1000;

        public static byte[] Encrypt(byte[] data, byte[] key)
        {
            using (var AES = new RijndaelManaged())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;

                var driveBytes = new Rfc2898DeriveBytes(key, RijndaelSaltBytes, rijndaelIterations);
                AES.Key = driveBytes.GetBytes(AES.KeySize / 8);
                AES.IV = driveBytes.GetBytes(AES.BlockSize / 8);
                AES.Mode = CipherMode.CBC;

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    return ms.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            using (var AES = new RijndaelManaged())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;

                var driveBytes = new Rfc2898DeriveBytes(key, RijndaelSaltBytes, rijndaelIterations);
                AES.Key = driveBytes.GetBytes(AES.KeySize / 8);
                AES.IV = driveBytes.GetBytes(AES.BlockSize / 8);
                AES.Mode = CipherMode.CBC;

                using(var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encrypted, 0, encrypted.Length);
                    return ms.ToArray();
                }
            }
        }
    }
}