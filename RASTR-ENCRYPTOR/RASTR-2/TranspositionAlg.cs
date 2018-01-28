using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Windows;

namespace RASTR_2
{
    public class TranspositionAlg
    {
        public static List<List<byte[]>> Keys { get; set; }
        public static List<int> Ways { get; set; }
        public static byte[] IV { get; set; }
        public static void SetKey(int bytesPerPixel)
        {
            Keys = new List<List<byte[]>>();
            int n = 0;
            while (n < bytesPerPixel)
            {
                List<byte[]> k = new List<byte[]>();
                for (int i = 0; i < Encryption.algParams[3]; i++)
                {
                    k.Add(new byte[0]);
                }
                Keys.Add(k);
                n++;
            }
        }
        public static void Generate(string keyId, int parametr, string pass)
        {
            switch (keyId)
            {
                case "allRng":
                    int n = 0;
                    while (n < parametr)
                    {
                        for (int i = 0; i < Encryption.algParams[3]; i++)
                        {
                            Keys[n][i] = Rng();
                        }
                        n++;
                    }
                    break;
                case "GrayRng":
                    Keys[0][parametr] = Rng();
                    break;
                case "RedRng":
                    Keys[0][parametr] = Rng();
                    break;
                case "GreenRng":
                    Keys[1][parametr] = Rng();
                    break;
                case "BlueRng":
                    Keys[2][parametr] = Rng();
                    break;
                case "GrayPass":
                    Keys[0][parametr] = Pass(pass);
                    break;
                case "RedPass":
                    Keys[0][parametr] = Pass(pass);
                    break;
                case "GreenPass":
                    Keys[1][parametr] = Pass(pass);
                    break;
                case "BluePass":
                    Keys[2][parametr] = Pass(pass);
                    break;
            }
        }
        public static byte[] Rng()
        {
            var rNg = new RNGCryptoServiceProvider();
            byte[] key = new byte[Encryption.algParams[2]];
            rNg.GetBytes(key);
            return key;
        }
        public static byte[] Pass(string pass)
        {
            byte[] salt = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var rfc = new Rfc2898DeriveBytes(pass, salt);
            byte[] key = rfc.GetBytes(Encryption.algParams[2]);
            return key;
        }
        public static BitmapSource Encrypt(BitmapSource inputImg)
        {
            int length = Encryption.algParams[2];
            foreach (List<byte[]> key in Keys)
            {
                for (int i = 0; i < Encryption.algParams[3]; i++)
                {
                    if (key[i].Length != length)
                    {
                        MainWindow.ShowMessageBox("Довжина зненерованих ключів менша за розмір блока! \nЗгенеруйте нові!", "Warning");
                        return null;
                    }
                }
            }

            int bytesPerPixel = Encryption.CountBytesPerPixel(inputImg);

            if ((inputImg.PixelWidth % length) != 0 || (inputImg.PixelHeight % length) != 0)
                inputImg = Encryption.ResizeImg(inputImg, length);
            else
                Encryption.algParams.Add(0);

            List<byte[]> blocks = new List<byte[]>();
            int blocksCount = (inputImg.PixelWidth * inputImg.PixelHeight) / (int)(Math.Pow(length, 2));
            IV = Rng();
            for (int yI = 0; yI < inputImg.PixelHeight / length; yI++)
            {
                for (int xI = 0; xI < inputImg.PixelWidth / length; xI++)
                {
                    byte[] pixels = new byte[(int)(Math.Pow(length, 2)) * bytesPerPixel];
                    Int32Rect rect = new Int32Rect(xI * length, yI * length, length, length);
                    inputImg.CopyPixels(rect, pixels, length * bytesPerPixel, 0);
                    if (Encryption.algParams[1] == 1)
                        pixels = EncryptBlocks(pixels, bytesPerPixel);
                    blocks.Add(pixels);
                }
            }
            Random rng = new Random();
            int n = blocks.Count;
            Ways = new List<int>();
            while (n > 1)
            {
                int k = rng.Next(n--);
                Ways.Add(k);
                byte[] temp = blocks[n];
                blocks[n] = blocks[k];
                blocks[k] = temp;
            }

            var result = new WriteableBitmap(inputImg);
            int blockI = 0;
            for (int yI = 0; yI < result.PixelHeight / length; yI++)
            {
                for (int xI = 0; xI < result.PixelWidth / length; xI++)
                {
                    Int32Rect rect = new Int32Rect(xI * length, yI * length, length, length);
                    result.WritePixels(rect, blocks[blockI], length * bytesPerPixel, 0);
                    blockI++;
                }
            }
            return result;
        }
        public static BitmapSource Decrypt(BitmapSource inputImg)
        {
            int length = Encryption.algParams[2];
            int bytesPerPixel = Encryption.CountBytesPerPixel(inputImg);

            List<byte[]> blocks = new List<byte[]>();
            for (int yI = 0; yI < inputImg.PixelHeight / length; yI++)
            {
                for (int xI = 0; xI < inputImg.PixelWidth / length; xI++)
                {
                    byte[] pixels = new byte[(int)(Math.Pow(length, 2)) * bytesPerPixel];
                    Int32Rect rect = new Int32Rect(xI * length, yI * length, length, length);
                    inputImg.CopyPixels(rect, pixels, length * bytesPerPixel, 0);
                    if (Encryption.algParams[1] == 1)
                        pixels = DecryptBlocks(pixels, bytesPerPixel);
                    blocks.Add(pixels);
                }
            }
            int n = 1;
            while (n < blocks.Count)
            {
                int k = Ways[(blocks.Count - 1) - n];
                byte[] temp = blocks[n];
                blocks[n] = blocks[k];
                blocks[k] = temp;
                n++;
            }
            var result = new WriteableBitmap(inputImg);
            int blockI = 0;
            for (int yI = 0; yI < result.PixelHeight / length; yI++)
            {
                for (int xI = 0; xI < result.PixelWidth / length; xI++)
                {
                    Int32Rect rect = new Int32Rect(xI * length, yI * length, length, length);
                    result.WritePixels(rect, blocks[blockI], length * bytesPerPixel, 0);
                    blockI++;
                }
            }
            if (Encryption.algParams[4] != 0)
                return Encryption.DesizeImg(result);
            return result;
        }
        
        private static byte[] EncryptBlocks(byte[] pixels, int bytesPerPixel)
        {
            int length = Encryption.algParams[2];
            int row, col;
            for (int i = 0; i < Keys.Count; i++)
            {
                row = 0;
                while (row < length * bytesPerPixel)
                {
                    col = 0;
                    while (col <= (length - 1) * bytesPerPixel)
                    {
                        int I = IV[row / bytesPerPixel];
                        int K = Keys[i][0][col / bytesPerPixel];

                        while (I >= length)
                            I -= length;
                        while (K >= length)
                            K -= length;

                        byte tmp = (byte)(pixels[row * length + col + i] ^ Keys[i][0][col / bytesPerPixel]);
                        pixels[row * length + col + i] = (byte)(pixels[row * length + ((K ^ I) * bytesPerPixel) + i] ^ Keys[i][0][col / bytesPerPixel]);
                        pixels[row * length + ((K ^ I) * bytesPerPixel) + i] = tmp;

                        col += bytesPerPixel;
                    }
                    row += bytesPerPixel;
                }
            }
            
            
            if (Encryption.algParams[3] == 1)
                return pixels;
            for (int i = 0; i < Keys.Count; i++)
            {
                col = 0;
                while (col < length * bytesPerPixel)
                {
                    row = 0;
                    while (row <= (length - 1) * bytesPerPixel)
                    {
                        int I = IV[col / bytesPerPixel];
                        int K = Keys[i][1][row / bytesPerPixel];

                        while (I >= length)
                            I -= length;
                        while (K >= length)
                            K -= length;

                        byte tmp = (byte)(pixels[row * length + col + i] ^ Keys[i][1][row / bytesPerPixel]);
                        pixels[row * length + col + i] = (byte)(pixels[row * length + ((K ^ I) * bytesPerPixel) + i] ^ Keys[i][1][row / bytesPerPixel]);
                        pixels[row * length + ((K ^ I) * bytesPerPixel) + i] = tmp;

                        row += bytesPerPixel;
                    }
                    col += bytesPerPixel;
                }
            }
            return pixels;
        }
        private static byte[] DecryptBlocks(byte[] pixels, int bytesPerPixel)
        {
            int length = Encryption.algParams[2];
            int row, col;
            if (Encryption.algParams[3] == 2)
            {
                for (int i = 0; i < Keys.Count; i++)
                {
                    col = (length - 1) * bytesPerPixel;
                    while (col >= 0)
                    {
                        row = (length - 1) * bytesPerPixel;
                        while (row >= 0)
                        {
                            int I = IV[col / bytesPerPixel];
                            int K = Keys[i][1][row / bytesPerPixel];

                            while (I >= length)
                                I -= length;
                            while (K >= length)
                                K -= length;

                            byte tmp = (byte)(pixels[row * length + col + i] ^ Keys[i][1][row / bytesPerPixel]);
                            pixels[row * length + col + i] = (byte)(pixels[row * length + ((K ^ I) * bytesPerPixel) + i] ^ Keys[i][1][row / bytesPerPixel]);
                            pixels[row * length + ((K ^ I) * bytesPerPixel) + i] = tmp;

                            row -= bytesPerPixel;
                        }
                        col -= bytesPerPixel;
                    }
                }
            }
                
            for (int i = 0; i < Keys.Count; i++)
            {
                row = (length - 1) * bytesPerPixel;
                while (row >= 0)
                {
                    col = (length - 1) * bytesPerPixel;
                    while (col >= 0)
                    {
                        int I = IV[row / bytesPerPixel];
                        int K = Keys[i][0][col / bytesPerPixel];

                        while (I >= length)
                            I -= length;
                        while (K >= length)
                            K -= length;

                        byte tmp = (byte)(pixels[row * length + col + i] ^ Keys[i][0][col / bytesPerPixel]);
                        pixels[row * length + col + i] = (byte)(pixels[row * length + ((K ^ I) * bytesPerPixel) + i] ^ Keys[i][0][col / bytesPerPixel]);
                        pixels[row * length + ((K ^ I) * bytesPerPixel) + i] = tmp;

                        col -= bytesPerPixel;
                    }
                    row -= bytesPerPixel;
                }
            }
            return pixels;
        }
        public static string Export()
        {
            List<string> key = new List<string>();
            key.Add(String.Join(",", Encryption.algParams));
            key.Add(String.Join(",", Ways));
            if (Encryption.algParams[1] == 1)
            {
                key.Add(String.Join(",", IV));
                if (Encryption.algParams[0] == 0)
                {
                    for (int i = 0; i < Keys.Count; i++)
                    {
                        key.Add(String.Join(",", Keys[i][0]));
                        if (Encryption.algParams[3] == 2)
                            key.Add(String.Join(",", Keys[i][1]));
                    }
                }
            }

            return String.Join(";", key);
        }
    }
}
