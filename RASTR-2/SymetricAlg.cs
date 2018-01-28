using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Windows;
using System.IO;

namespace RASTR_2
{
    public class SymetricAlg
    {
        public static byte[] IV { get; set; }
        public static byte[] Key { get; set; }
        public static void Generate(string pass)
        {
            IV = Rng(Encryption.algParams[2]);
            if (Encryption.algParams[0] == 0)
                Key = Rng(Encryption.algParams[1] / 8);
            else
                Key = Pass(pass);
        }
        private static byte[] Rng(int size)
        {
            var rNg = new RNGCryptoServiceProvider();
            byte[] key = new byte[size];
            rNg.GetBytes(key);
            return key;
        }
        private static byte[] Pass(string pass)
        {
            byte[] salt = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var rfc = new Rfc2898DeriveBytes(pass, salt);
            byte[] key = rfc.GetBytes(Encryption.algParams[1] / 8);
            return key;
        }
        public static BitmapSource RunAlg(BitmapSource inputImg, int action)
        {
            if (Key == null || IV == null)
                return null;

            int bytesPerPixel = Encryption.CountBytesPerPixel(inputImg);
            int lenght = inputImg.PixelWidth * inputImg.PixelHeight * bytesPerPixel;
            byte[] bytes;
            using (var alg = ChechAlg())
            {
                alg.KeySize = Encryption.algParams[1];
                alg.Key = Key;
                alg.IV = IV;
                alg.Padding = PaddingMode.None;
                if (action == 0)
                {
                    if (lenght % Encryption.algParams[2] != 0)
                        inputImg = Encryption.ResizeImg(inputImg, Encryption.algParams[2]);
                    else
                        Encryption.algParams.Add(0);
                    bytes = new byte[inputImg.PixelWidth * inputImg.PixelHeight * bytesPerPixel];
                    inputImg.CopyPixels(bytes, inputImg.PixelWidth * bytesPerPixel, 0);
                    bytes = EncryptImg(alg, bytes);
                }
                else
                {
                    bytes = new byte[inputImg.PixelWidth * inputImg.PixelHeight * bytesPerPixel];
                    inputImg.CopyPixels(bytes, inputImg.PixelWidth * bytesPerPixel, 0);
                    bytes = DecryptImg(alg, bytes);
                }

                if (Encryption.algParams[3] != 0 && action == 1)
                    return Encryption.DesizeImg(BitmapSource.Create(inputImg.PixelWidth, inputImg.PixelHeight, inputImg.DpiX, inputImg.DpiY, inputImg.Format, inputImg.Palette, bytes, inputImg.PixelWidth * bytesPerPixel));
                return BitmapSource.Create(inputImg.PixelWidth, inputImg.PixelHeight, inputImg.DpiX, inputImg.DpiY, inputImg.Format, inputImg.Palette, bytes, inputImg.PixelWidth * bytesPerPixel);
            }
        }
        private static dynamic ChechAlg()
        {
            dynamic Alg = null;
            switch (Encryption.currentAlg)
            {
                case 1:
                    Alg = new DESCryptoServiceProvider();
                    break;
                case 2:
                    Alg = new TripleDESCryptoServiceProvider();
                    break;
                case 3:
                    Alg = new RijndaelManaged();
                    break;
                case 4:
                    Alg = new RC2CryptoServiceProvider();
                    break;
            }
            return Alg;
        }
        private static byte[] EncryptImg(SymmetricAlgorithm alg, byte[] pixels)
        {
            using (var stream = new MemoryStream())
            using (var encryptor = alg.CreateEncryptor())
            using (var encrypt = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            {
                encrypt.Write(pixels, 0, pixels.Length);
                encrypt.FlushFinalBlock();
                return stream.ToArray();
            }
        }
        private static byte[] DecryptImg(SymmetricAlgorithm alg, byte[] pixels)
        {
            using (var stream = new MemoryStream())
            using (var decryptor = alg.CreateDecryptor())
            using (var decrypt = new CryptoStream(stream, decryptor, CryptoStreamMode.Write))
            {
                decrypt.Write(pixels, 0, pixels.Length);
                decrypt.FlushFinalBlock();
                return stream.ToArray();
            }
        }
        public static string Export()
        {
            List<string> key = new List<string>();
            key.Add(String.Join(",", Encryption.algParams));
            key.Add(String.Join(",", IV));
            if (Encryption.algParams[0] == 0)
                key.Add(String.Join(",", Key));
            return String.Join(";", key);
        }
    }
}
