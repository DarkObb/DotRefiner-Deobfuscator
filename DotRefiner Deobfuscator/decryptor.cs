using System.IO;
using System.Security.Cryptography;

namespace DotRefiner_Deobfuscator {
    public static class decryptor {
        public static byte[] aesDec(byte[] arrayy) {
            byte[] result;

            using (var rijndaelManaged = new RijndaelManaged()) {
                rijndaelManaged.BlockSize = 128;
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.GenerateKey();
                rijndaelManaged.GenerateIV();

                using (var memoryStream = new MemoryStream(arrayy)) {
                    byte[] array = new byte[rijndaelManaged.Key.Length];
                    byte[] array2 = new byte[rijndaelManaged.IV.Length];
                    memoryStream.Read(array, 0, array.Length);
                    memoryStream.Read(array2, 0, array2.Length);

                    using (var cryptoTransform = rijndaelManaged.CreateDecryptor(array, array2)) {
                        using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read)) {
                            byte[] array3 = new byte[memoryStream.Length - memoryStream.Position];
                            cryptoStream.Read(array3, 0, array3.Length);
                            result = array3;
                        }
                    }
                }
            }

            return result;
        }

        public static byte[] DecryptIt(Stream stream) {
            byte[] array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);
            return aesDec(array);
        }
    }
}

/* Made by DarkObb */