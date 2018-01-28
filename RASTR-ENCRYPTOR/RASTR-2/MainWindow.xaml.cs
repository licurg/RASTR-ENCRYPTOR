using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;

namespace RASTR_2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public class Encryption
    {
        // Змінна містить обраний алгоритм шифрування
        public static int currentAlg { get; set; }
        // У колекцію записуються параметри для виконання потрібного алгоритму
        public static List<int> algParams { get; set; }
        public static int CountBytesPerPixel(BitmapSource image)
        {
            int bytesPerPixel = (image.Format.BitsPerPixel + 7) / 8;
            return bytesPerPixel;
        }
        public static BitmapSource ResizeImg(BitmapSource inputImg, int lenght)
        {
            int bytesPerPixel = CountBytesPerPixel(inputImg);
            algParams.Add(inputImg.PixelWidth);
            algParams.Add(inputImg.PixelHeight);
            int Width = 0, Height = 0;
            if (inputImg.PixelWidth > inputImg.PixelHeight)
            {
                if ((inputImg.PixelWidth % lenght) != 0)
                {
                    int newSize = (inputImg.PixelWidth / lenght + 1) * lenght;
                    Width = newSize;
                    Height = newSize;
                }
                else
                {
                    Width = inputImg.PixelWidth;
                    Height = inputImg.PixelWidth;
                }
            }
            else if (inputImg.PixelWidth < inputImg.PixelHeight)
            {
                if ((inputImg.PixelHeight % lenght) != 0)
                {
                    int newSize = (inputImg.PixelHeight / lenght + 1) * lenght;
                    Width = newSize;
                    Height = newSize;
                }
                else
                {
                    Width = inputImg.PixelHeight;
                    Height = inputImg.PixelHeight;
                }
            }
            else if (inputImg.PixelWidth == inputImg.PixelHeight)
            {
                int newSize = (inputImg.PixelWidth / lenght + 1) * lenght;
                Width = newSize;
                Height = newSize;
            }
            int neededBytes = Width * Height * bytesPerPixel;
            byte[] originalPixels = new byte[inputImg.PixelWidth * inputImg.PixelHeight * bytesPerPixel];
            inputImg.CopyPixels(originalPixels, inputImg.PixelWidth * bytesPerPixel, 0);
            byte[] pixels = new byte[neededBytes];
            int row = 0, col = 0, iteration = 1;
            for (int i = 0; i < originalPixels.Length; i++)
            {
                pixels[row * Width * bytesPerPixel + col] = originalPixels[i];
                col++;
                if (i == (inputImg.PixelWidth * iteration * bytesPerPixel) - 1)
                {
                    row++;
                    iteration++;
                    col = 0;
                }
            }
            BitmapSource resizedImg = BitmapSource.Create(Width, Height, inputImg.DpiX, inputImg.DpiY, inputImg.Format, inputImg.Palette, pixels, Width * bytesPerPixel);
            return resizedImg;
        }
        public static BitmapSource DesizeImg(BitmapSource inputImg)
        {
            int bytesPerPixel = CountBytesPerPixel(inputImg);
            byte[] bytes = new byte[inputImg.PixelWidth * inputImg.PixelHeight * bytesPerPixel];
            inputImg.CopyPixels(bytes, inputImg.PixelWidth * bytesPerPixel, 0);
            int index = currentAlg != 0 ? 3 : 4;
            byte[] pixels = new byte[algParams[index] * algParams[index + 1] * bytesPerPixel];
            int row = 0, col = 0, iteration = 1;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = bytes[row * inputImg.PixelWidth * bytesPerPixel + col];
                col++;
                if (i == (algParams[index] * iteration * bytesPerPixel) - 1)
                {
                    row++;
                    iteration++;
                    col = 0;
                }
            }
            BitmapSource resizedImg = BitmapSource.Create(algParams[index], algParams[index + 1], inputImg.DpiX, inputImg.DpiY, inputImg.Format, inputImg.Palette, pixels, algParams[index] * bytesPerPixel);
            return resizedImg;
        }
    }
    public partial class MainWindow : Window
    {
        public static int currentAction { get; set; }
        private static bool decrypted { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Encryption.algParams = new List<int>() {
                0, 1, 256, 2
            };
            currentAction = 0;
            DrawSettings();
        }
        private void OpenImg(object sender, RoutedEventArgs e)
        {
            var openFile = new OpenFileDialog();
            openFile.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG;)|*.BMP;*.JPG;*.GIF;*.PNG;";
            openFile.RestoreDirectory = true;
            if (openFile.ShowDialog() == true)
            {
                dynamic decoder = null;
                try
                {
                    var streamSource = new FileStream(openFile.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    switch (new FileInfo(openFile.FileName).Extension)
                    {
                        case ".jpg":
                            decoder = new JpegBitmapDecoder(streamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            break;
                        case ".png":
                            decoder = new PngBitmapDecoder(streamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            break;
                        case ".bmp":
                            decoder = new BmpBitmapDecoder(streamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            break;
                        case ".gif":
                            decoder = new GifBitmapDecoder(streamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            break;
                    }
                    uploadedImg.Source = decoder.Frames[0];
                }
                catch (Exception err)
                {
                    ShowMessageBox(err.Message.ToString(), "Error");
                    return;
                }
                imagePath.Text = openFile.FileName;
                keyPath.Text = null;
                SetParams();
                DrawSettings();
                if (Encryption.currentAlg == 0 && uploadedImg.Source != null)
                    TranspositionAlg.SetKey(Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource));
                decrypted = false;
            }
        }
        private void OpenKey(object sender, RoutedEventArgs e)
        {
            var openFile = new OpenFileDialog();
            openFile.Filter = "TEXT FILE(*.TXT;)|*.TXT;";
            openFile.RestoreDirectory = true;
            if (openFile.ShowDialog() == true)
            {
                try
                {
                    using (StreamReader file = new StreamReader(openFile.FileName, true))
                    {
                        dynamic key = Encoding.Unicode.GetString(Convert.FromBase64String(file.ReadLine()));
                        key = key.Split(';');
                        Encryption.currentAlg = Int32.Parse(key[0]);
                        Encryption.algParams = (key[1].Split(',') as string[]).Select(Int32.Parse).ToList();
                        if (Encryption.currentAlg == 0)
                        {
                            if (key[2] != "")
                                TranspositionAlg.Ways = (key[2].Split(',') as string[]).Select(Int32.Parse).ToList();
                            else
                                TranspositionAlg.Ways = new List<int>() { 0 };
                            if (Encryption.algParams[1] == 1)
                            {
                                TranspositionAlg.IV = (key[3].Split(',') as string[]).Select(Byte.Parse).ToArray();
                                if (Encryption.algParams[0] == 0)
                                    for (int i = 0, step = 4; i < TranspositionAlg.Keys.Count; i++, step += Encryption.algParams[3])
                                    {
                                        TranspositionAlg.Keys[i][0] = (key[step].Split(',') as string[]).Select(Byte.Parse).ToArray();
                                        if (Encryption.algParams[3] == 2)
                                            TranspositionAlg.Keys[i][1] = (key[step + 1].Split(',') as string[]).Select(Byte.Parse).ToArray();
                                    }
                            }
                        }
                        else
                        {
                            SymetricAlg.IV = (key[2].Split(',') as string[]).Select(Byte.Parse).ToArray();
                            if (Encryption.algParams[0] == 0)
                                SymetricAlg.Key = (key[3].Split(',') as string[]).Select(Byte.Parse).ToArray();
                        }
                        file.Close();
                    }
                }
                catch (Exception err)
                {
                    ShowMessageBox(err.Message.ToString(), "Error");
                    return;
                }
                keyPath.Text = openFile.FileName;
                DrawSettings();
                decrypted = false;
            }
        }
        private void AlgChanged(object sender, EventArgs e)
        {
            if (!window.IsLoaded)
                return;
            Encryption.currentAlg = algorithms.SelectedIndex;
            SetParams();
            ShowStatus("Зачекайте");
            DrawSettings();
        }
        private void SetParams()
        {
            switch (Encryption.currentAlg)
            {
                case 0:
                    Encryption.algParams = new List<int>() {
                        0, 1, 256, 2
                    };
                    if (uploadedImg.Source != null)
                        TranspositionAlg.SetKey(Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource));
                    break;
                case 1:
                    Encryption.algParams = new List<int>() {
                        0, 64, 8
                    };
                    break;
                case 2:
                    Encryption.algParams = new List<int>() {
                        0, 128, 8
                    };
                    break;
                case 3:
                    Encryption.algParams = new List<int>() {
                        0, 128, 16
                    };
                    break;
                case 4:
                    Encryption.algParams = new List<int>() {
                        0, 40, 8
                    };
                    break;
            }
        }
        private void ActionChanged(object sender, RoutedEventArgs e)
        {
            currentAction = (sender as TabControl).SelectedIndex;
            if (currentAction != 0)
            {
                algSelection.Visibility = Visibility.Collapsed;
                encrypt.Visibility = Visibility.Collapsed;
                keyUpload.Visibility = Visibility.Visible;
            }
            else
            {
                keyUpload.Visibility = Visibility.Collapsed;
                algSelection.Visibility = Visibility.Visible;
                encrypt.Visibility = Visibility.Visible;
            }
            decrypted = false;
            algorithms.SelectedIndex = 0;
            Encryption.currentAlg = 0;
            uploadedImg.Source = null;
            processedImg.Source = null;
            keyPath.Text = null;
            imagePath.Text = null;
            SetParams();
            DrawSettings();
            
            e.Handled = true;
        }
        public void DrawSettings()
        {
            if (uploadedImg.Source == null)
            {
                ShowStatus("Завантажте зображення");
                return;
            }

            if (currentAction != 0)
            {
                if (Encryption.algParams[0] == 1)
                    decryptorSettings.Content = DrawPassword();
                else
                    ShowStatus("У дешифраторі налаштування обираються автоматично");
                return;
            }
                
            WrapPanel mainWP = new WrapPanel() {
                Style = Resources["WrapPanel"] as Style
            };

            StackPanel[] sPs = new StackPanel[] {
                new StackPanel(),
                new StackPanel(),
                new StackPanel()
            };
            string[] sPLabels = new string[]
            {
                "Генерація ключів:",
                "Налаштування:",
                "Параметри:"
            };
            int panel = 0;
            foreach(StackPanel sP in sPs)
            {
                var label = new Label()
                {
                    Margin = Encryption.algParams[0] == 1 && panel == 0 && Encryption.currentAlg == 0 ? new Thickness(0, 0, 0, 30) : new Thickness(0, 0, 0, 10)
                };
                
                label.Content = sPLabels[panel];
                label.HorizontalAlignment = HorizontalAlignment.Center;
                sP.Style = Resources["StackPanel"] as Style;
                sP.Children.Add(label);
                
                if (Encryption.currentAlg == 0)
                   TranspositionDraw(ref sPs[panel], panel);
                else
                   AlgDraw(ref sPs[panel], panel);

                panel++;
                mainWP.Children.Add(sP);
            }

            encryptorSettings.Content = mainWP;
            
        }
        private void ShowStatus(string status)
        {
            var label = new Label();

            label.Content = status;
            label.Style = Resources["Status"] as Style;

            var settingsWindow = (currentAction == 0) ? encryptorSettings : decryptorSettings;
            settingsWindow.Content = label;
        }
        private UIElement DrawPassword()
        {
            var mainWP = new WrapPanel()
            {
                Style = Resources["WrapPanel"] as Style
            };
            var sP = new StackPanel()
            {
                Margin = new Thickness(10, 0, 0, 0),
                Style = Resources["StackPanel"] as Style
            };

            sP.Children.Add(new Label() {
                Content = "Введіть паролі для генерації ключів:",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center
            });
            if (Encryption.currentAlg == 0)
            {
                int bytesPerPixel = Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource);
                List<WrapPanel> wPs = new List<WrapPanel>();
                List<Brush> colors = new List<Brush>();
                List<string> genLabels = new List<string>();
                if (bytesPerPixel == 1)
                {
                    wPs.Add(new WrapPanel());
                    colors.Add(Brushes.Gray);
                    genLabels.Add("Gray");
                }
                else
                {
                    wPs.AddRange(new[] {
                        new WrapPanel(),
                        new WrapPanel(),
                        new WrapPanel()
                    });

                    colors.AddRange(new[] {
                        Brushes.Red,
                        Brushes.Green,
                        Brushes.Blue
                    });
                    genLabels.AddRange(new[] {
                        "Red",
                        "Green",
                        "Blue"
                    });
                }
                int color = 0;
                foreach (WrapPanel wP in wPs)
                {
                    wP.Style = Resources["WrapPanel"] as Style;
                    wP.Margin = new Thickness(0, 10, 0, 0);
                    var colorRect = new Rectangle();
                    colorRect.Style = Resources["ColorRectangle"] as Style;
                    colorRect.Fill = colors[color];
                    wP.Children.Add(colorRect);
                    if (Encryption.algParams[3] == 1)
                    {
                        var generateField = new PasswordBox()
                        {
                            Style = Resources["MainPasswordBox"] as Style,
                            Width = 347,
                            Margin = new Thickness(5, 0, 0, 0),
                            Name = genLabels[color] + "Pass",
                            Uid = "0"
                        };
                        generateField.AddHandler(PasswordBox.LostFocusEvent, new RoutedEventHandler(GeneratePass));
                        wP.Children.Add(generateField);
                    }
                    else
                    {
                        for (int i = 0; i < Encryption.algParams[3]; i++)
                        {
                            var generateField = new PasswordBox()
                            {
                                Style = Resources["MainPasswordBox"] as Style,
                                Margin = new Thickness(5, 0, 0, 0),
                                Width = 171,
                                Name = genLabels[color] + "Pass",
                                Uid = i.ToString()
                            };
                            generateField.AddHandler(PasswordBox.LostFocusEvent, new RoutedEventHandler(GeneratePass));

                            wP.Children.Add(generateField);
                        }
                    }

                    sP.Children.Add(wP);
                    color++;
                }
            }
            else
            {
                var keyField = new PasswordBox()
                {
                    Style = Resources["MainPasswordBox"] as Style,
                    Width = 347,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                keyField.AddHandler(PasswordBox.LostFocusEvent, new RoutedEventHandler(GeneratePass));
                sP.Children.Add(keyField);
            }
            mainWP.Children.Add(sP);
            return mainWP;
        }
        // Setting panel for Transposition Algorithm
        #region TRANSPOSITION_ALG_INTERFACE
        private void TranspositionDraw(ref StackPanel sP, int panel)
        {
            switch (panel)
            {
                case 0:
                    int bytesPerPixel = Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource);
                    if (Encryption.algParams[0] == 0)
                    {
                        var generateAllBtn = new Button()
                        {
                            Style = Resources["GenerateButton"] as Style,
                            Margin = new Thickness(0, 5, 0, 0),
                            Width = 240,
                            Content = "Згенерувати все",
                            Name = "allRng",
                            Uid = Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource).ToString()
                        };
                        generateAllBtn.AddHandler(Button.ClickEvent, new RoutedEventHandler(GenerateRng));
                        sP.Children.Add(generateAllBtn);
                    }

                    List<WrapPanel> wPs = new List<WrapPanel>();
                    List<Brush> colors = new List<Brush>();
                    List<string> genLabels = new List<string>();
                    if (bytesPerPixel == 1)
                    {
                        wPs.Add(new WrapPanel());
                        colors.Add((SolidColorBrush)new BrushConverter().ConvertFrom("#BDBDBD"));
                        genLabels.Add("Gray");
                    }
                    else
                    {
                        wPs.AddRange(new[] { new WrapPanel(),
                                             new WrapPanel(),
                                             new WrapPanel()
                        });

                        colors.AddRange(new[] { (SolidColorBrush)new BrushConverter().ConvertFrom("#f44336"),
                                                (SolidColorBrush)new BrushConverter().ConvertFrom("#4CAF50"),
                                                (SolidColorBrush)new BrushConverter().ConvertFrom("#2196F3")
                        });
                        genLabels.AddRange(new[] {
                            "Red",
                            "Green",
                            "Blue"
                        });
                    }
                    int color = 0;
                    foreach (WrapPanel wP in wPs)
                    {
                        wP.Style = Resources["WrapPanel"] as Style;
                        wP.Margin = new Thickness(0, 10, 0, 0);
                        var colorRect = new Rectangle()
                        {
                            Style = Resources["ColorRectangle"] as Style,
                            Fill = colors[color]
                        };
                        wP.Children.Add(colorRect);
                        if (Encryption.algParams[3] == 1)
                        {
                            dynamic generateField;
                            if (Encryption.algParams[0] == 0)
                            {
                                generateField = new Button()
                                {
                                    Style = Resources["GenerateButton"] as Style,
                                    Width = 347,
                                    Margin = new Thickness(5, 0, 0, 0),
                                    Content = "Генерація для " + genLabels[color],
                                    Name = genLabels[color] + "Rng",
                                    Uid = "0"
                                };
                                generateField.AddHandler(Button.ClickEvent, new RoutedEventHandler(GenerateRng));
                            }
                            else
                            {
                                generateField = new PasswordBox()
                                {
                                    Style = Resources["MainPasswordBox"] as Style,
                                    Width = 347,
                                    Margin = new Thickness(5, 0, 0, 0),
                                    Name = genLabels[color] + "Pass",
                                    Uid = "0"
                                };
                                generateField.AddHandler(PasswordBox.LostFocusEvent, new RoutedEventHandler(GeneratePass));
                            }
                            wP.Children.Add(generateField);
                        }
                        else
                        {
                            string[] labels = new string[] {
                                "Генерація для рядків",
                                "Генерація для стовпців"
                            };
                            for (int i = 0; i < labels.Length; i++)
                            {
                                dynamic generateField;
                                if (Encryption.algParams[0] == 0)
                                {
                                    generateField = new Button()
                                    {
                                        Style = Resources["GenerateButton"] as Style,
                                        Margin = new Thickness(5, 0, 0, 0),
                                        Width = 171,
                                        Content = labels[i],
                                        Name = genLabels[color] + "Rng",
                                        Uid = i.ToString()
                                    };
                                    generateField.AddHandler(Button.ClickEvent, new RoutedEventHandler(GenerateRng));
                                }
                                else
                                {
                                    generateField = new PasswordBox()
                                    {
                                        Style = Resources["MainPasswordBox"] as Style,
                                        Margin = new Thickness(5, 0, 0, 0),
                                        Width = 171,
                                        Name = genLabels[color] + "Pass",
                                        Uid = i.ToString()
                                    };
                                    generateField.AddHandler(PasswordBox.LostFocusEvent, new RoutedEventHandler(GeneratePass));
                                }

                                wP.Children.Add(generateField);
                            }
                        }

                        sP.Children.Add(wP);
                        color++;
                    }
                    break;
                case 1:
                    sP.Margin = new Thickness(20, 0, 0, 0);
                    var rngType = new RadioButton()
                    {
                        Width = 330,
                        Style = Resources["MainRadioButton"] as Style,
                        Margin = new Thickness(0, 5, 0, 0),
                        GroupName = "keyType",
                        IsChecked = Encryption.algParams[0] == 0 ? true : false
                    };
                    rngType.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(KeyTypeRng));
                    var rngTypeText = new TextBlock()
                    {
                        Style = Resources["TextBlock"] as Style,
                        Text = "Ключ з криптографічного генератора псевдовипадкових послідовностей",
                        
                    };

                    var passType = new RadioButton()
                    {
                        Width = 330,
                        Style = Resources["MainRadioButton"] as Style,
                        Margin = new Thickness(0, 10, 0, 10),
                        GroupName = "keyType",
                        IsChecked = Encryption.algParams[0] == 1 ? true : false
                    };
                    passType.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(KeyTypePass));
                    var passTypeText = new TextBlock()
                    {
                        Style = Resources["TextBlock"] as Style,
                        Text = "Ключ генерується розширенням паролю"
                    };
                    var inBlocks = new CheckBox()
                    {
                        Style = Resources["MainCheckBox"] as Style,
                        Content = "Перестановки в середині блока",
                        Margin = new Thickness(0, 5, 0, 0),
                        IsChecked = Encryption.algParams[1] == 1 ? true : false
                    };
                    inBlocks.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(InBlockEncription));
                    rngType.Content = rngTypeText;
                    passType.Content = passTypeText;
                    sP.Children.Add(rngType);
                    sP.Children.Add(passType);
                    sP.Children.Add(inBlocks);
                    break;
                case 2:
                    sP.Margin = new Thickness(20, 0, 0, 0);
                    var label = new Label()
                    {
                        Style = Resources["ParamLabel"] as Style,
                        Content = "Розміри блока:"
                    };
                    List<string> cBItems = new List<string>()
                    {
                        "256",
                        "128",
                        "64",
                        "32",
                        "16"
                    };
                    var comboBox = new ComboBox()
                    {
                        Width = 100,
                        Margin = new Thickness(5, 0, 0, 0),
                        ItemsSource = cBItems,
                        SelectedIndex = cBItems.IndexOf(Encryption.algParams[2].ToString())
                    };
                    comboBox.AddHandler(ComboBox.SelectionChangedEvent, new RoutedEventHandler(BlockSize));
                    var wPc = new WrapPanel();
                    wPc.Children.Add(label);
                    wPc.Children.Add(comboBox);
                    sP.Children.Add(wPc);
                    var rowType = new RadioButton()
                    {
                        Style = Resources["MainRadioButton"] as Style,
                        Margin = new Thickness(0, 5, 0, 0),
                        GroupName = "chipherType",
                        IsChecked = Encryption.algParams[3] == 1 ? true : false
                    };
                    rowType.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(ChipherType1D));
                    var rowTypeText = new TextBlock()
                    {
                        Style = Resources["TextBlock"] as Style,
                        Text = "Режим одномірного шифрування"
                    };

                    var colType = new RadioButton()
                    {
                        Style = Resources["MainRadioButton"] as Style,
                        Margin = new Thickness(0, 10, 0, 10),
                        GroupName = "chipherType",
                        IsChecked = Encryption.algParams[3] == 2 ? true : false
                    };
                    var colTypeText = new TextBlock()
                    {
                        Style = Resources["TextBlock"] as Style,
                        Text = "Режим двомірного шифрування"
                    };
                    colType.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(ChipherType2D));
                    rowType.Content = rowTypeText;
                    colType.Content = colTypeText;
                    sP.Children.Add(rowType);
                    sP.Children.Add(colType);
                    break;
            }
        }
        #endregion
        // Setting panel for DES Algorithm
        #region SYMETRIC_ALG_INTERFACE
        private void AlgDraw(ref StackPanel sP, int panel)
        {
            switch (panel)
            {
                case 0:
                    sP.Margin = new Thickness(20, 0, 0, 0);
                    dynamic keyField;
                    if (Encryption.algParams[0] == 0)
                    {
                        keyField = new Button()
                        {
                            Style = Resources["GenerateButton"] as Style,
                            Margin = new Thickness(0, 5, 0, 5),
                            Width = 347,
                            Content = "Згенерувати ключ"
                        };
                        keyField.AddHandler(Button.ClickEvent, new RoutedEventHandler(GenerateRng));
                    }
                    else
                    {
                        keyField = new PasswordBox()
                        {
                            Style = Resources["MainPasswordBox"] as Style,
                            Width = 347,
                            Margin = new Thickness(0, 5, 0, 5)
                        };
                        keyField.AddHandler(PasswordBox.LostFocusEvent, new RoutedEventHandler(GeneratePass));
                    }
                    sP.Children.Add(keyField);
                    break;
                case 1:
                    sP.Margin = new Thickness(20, 0, 0, 0);
                    var rngType = new RadioButton()
                    {
                        Width = 330,
                        Style = Resources["MainRadioButton"] as Style,
                        Margin = new Thickness(0, 5, 0, 0),
                        GroupName = "keyType",
                        IsChecked = Encryption.algParams[0] == 0 ? true : false
                    };
                    rngType.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(KeyTypeRng));
                    var rngTypeText = new TextBlock()
                    {
                        Style = Resources["TextBlock"] as Style,
                        Text = "Ключ з криптографічного генератора псевдовипадкових послідовностей"
                    };

                    var passType = new RadioButton()
                    {
                        Width = 330,
                        Style = Resources["MainRadioButton"] as Style,
                        Margin = new Thickness(0, 10, 0, 10),
                        GroupName = "keyType",
                        IsChecked = Encryption.algParams[0] == 1 ? true : false
                    };
                    passType.AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(KeyTypePass));
                    var passTypeText = new TextBlock()
                    {
                        Style = Resources["TextBlock"] as Style,
                        Text = "Ключ генерується розширенням паролю"
                    };
                    rngType.Content = rngTypeText;
                    passType.Content = passTypeText;
                    sP.Children.Add(rngType);
                    sP.Children.Add(passType);
                    break;
                case 2:
                    sP.Margin = new Thickness(20, 0, 0, 0);
                    var elements = new UIElement[3, 2]
                    {
                        { new Label(){ Style = Resources["ParamLabel"] as Style, Content = "Розмір блока (біт):" }, Element(0)},
                        { new Label(){ Style = Resources["ParamLabel"] as Style, Content = "Довжина ключа (біт):" }, Element(1)},
                        { new Label(){ Style = Resources["ParamLabel"] as Style, Content = "Кількість раундів:" }, Element(2)}
                    };
                    for(int i = 0; i < elements.GetLength(0); i++)
                    {
                        var wPc = new WrapPanel();
                        wPc.Children.Add(elements[i, 0]);
                        wPc.Children.Add(elements[i, 1]);
                        sP.Children.Add(wPc);
                    }
                    break;
            }
        }
        private UIElement Element(int id)
        {
            dynamic element = null;
            switch (id)
            {
                case 0:
                    element = new Label()
                    {
                        Style = Resources["ParamLabel"] as Style,
                        Content = Encryption.currentAlg == 3 ? "128" : "64"
                    };
                    break;
                case 1:
                    if (Encryption.currentAlg == 1)
                        element = new Label()
                        {
                            Style = Resources["ParamLabel"] as Style,
                            Content = "64"
                        };
                    else if(Encryption.currentAlg == 4)
                    {
                        element = new WrapPanel();
                        element.Children.Add(new TextBox()
                        {
                            Style = Resources["MainTextBox"] as Style,
                            Width = 50,
                            Text = Encryption.algParams[1].ToString()
                        });
                        element.Children[0].AddHandler(TextBox.TextChangedEvent, new RoutedEventHandler(BlockSize));
                        var buttons = new StackPanel();
                        buttons.Children.Add(new Button()
                        {
                            Style = Resources["AddButton"] as Style,
                            Content = '+'
                        });
                        buttons.Children[0].AddHandler(Button.ClickEvent, new RoutedEventHandler(PlusButton));
                        buttons.Children.Add(new Button()
                        {
                            Style = Resources["AddButton"] as Style,
                            Content = '-'
                        });
                        buttons.Children[1].AddHandler(Button.ClickEvent, new RoutedEventHandler(MinusButton));
                        element.Children.Add(buttons);
                    }
                    else
                    {
                        var items = Encryption.currentAlg == 2 ? new List<string>() { "128", "192" }
                                            : new List<string>() { "128", "192", "256" };
                        element = new ComboBox()
                        {
                            Width = 100,
                            Margin = new Thickness(5, 0, 0, 0),
                            ItemsSource = items,
                            SelectedIndex = items.IndexOf(Encryption.algParams[1].ToString())
                        };
                        element.AddHandler(ComboBox.SelectionChangedEvent, new RoutedEventHandler(BlockSize));
                    }  
                    break;
                case 2:
                    element = new Label()
                    {
                        Style = Resources["ParamLabel"] as Style,
                        Content = Encryption.currentAlg == 1 ? "16"
                                : Encryption.currentAlg == 2 ? "16 * 3"
                                : Encryption.currentAlg == 3 ? "10, 12, 14"
                                : Encryption.currentAlg == 4 ? "15 + 3"
                                : null
                    };
                    break;
            }
            return element;
        }
        #endregion
        private void StartEncryption(object sender, RoutedEventArgs e)
        {
            if (uploadedImg.Source == null)
            {
                ShowMessageBox("Зображення для зашифровки не завантажено!", "Warning");
                return;
            }
            window.Cursor = Cursors.Wait;
            try
            {
                if (Encryption.currentAlg == 0)
                    processedImg.Source = TranspositionAlg.Encrypt(uploadedImg.Source as BitmapSource);
                else
                    processedImg.Source = SymetricAlg.RunAlg(uploadedImg.Source as BitmapSource, 0);
            }
            catch (Exception err)
            {
                ShowMessageBox(err.Message.ToString(), "Error");
                window.Cursor = Cursors.Arrow;
                return;
            }
            decrypted = false;
            window.Cursor = Cursors.Arrow;
        }
        private void StartDecryption(object sender, RoutedEventArgs e)
        {
            if (decrypted)
            {
                ShowMessageBox("Ви вже розшифровували це зображення!\nЗашифруйте знову!", "Warning");
                return;
            }
            if (Encryption.algParams.Count < (Encryption.currentAlg == 0 ? 5 : 4))
            {
                ShowMessageBox("Ключ розшифровки не завантажено!", "Warning");
                return;
            }
            if (uploadedImg.Source == null)
            {
                ShowMessageBox("Зображення для розшифровки не завантажено!", "Warning");
                return;
            }
            window.Cursor = Cursors.Wait;
            try
            {
                var image = currentAction == 0 ? processedImg.Source as BitmapSource : uploadedImg.Source as BitmapSource;
                if (Encryption.currentAlg == 0)
                    processedImg.Source = TranspositionAlg.Decrypt(image);
                else
                    processedImg.Source = SymetricAlg.RunAlg(image, 1);
            }
            catch (Exception err)
            {
                ShowMessageBox(err.Message.ToString(), "Error");
                window.Cursor = Cursors.Arrow;
                return;
            }
            decrypted = true;
            window.Cursor = Cursors.Arrow;
        }
        private void StartExport(object sender, RoutedEventArgs e)
        {
            if (processedImg.Source == null)
            {
                ShowMessageBox("Немає зображення для експорту!", "Warning");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "BMP Files|*.bmp";
            saveFileDialog.CreatePrompt = true;
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.RestoreDirectory = true;
            if ((bool)saveFileDialog.ShowDialog())
            {
                window.Cursor = Cursors.Wait;
                try
                {
                    var encoder = new BmpBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create((processedImg.Source as BitmapSource)));
                    using (FileStream stream = File.Open(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(stream);
                        stream.Close();
                    }
                    if (currentAction == 0)
                    {
                        string key = Encryption.currentAlg.ToString() + ";";
                        key = Convert.ToBase64String(Encoding.Unicode.GetBytes(
                            key + (Encryption.currentAlg == 0 ? TranspositionAlg.Export() : SymetricAlg.Export())));
                        using (StreamWriter file = new StreamWriter(saveFileDialog.FileName + ".txt", false))
                        {
                            file.Write(key);
                            file.Close();
                        }
                        this.Cursor = Cursors.Arrow;
                        ShowMessageBox("Файл розшифровки: \n(" + saveFileDialog.FileName + ".txt) \nуспішно створено!", "Info");
                    }
                    window.Cursor = Cursors.Arrow;
                }
                catch (Exception err)
                {
                    ShowMessageBox(err.Message.ToString(), "Error");
                    window.Cursor = Cursors.Arrow;
                    return;
                }
            }
        }
        private void KeyTypeRng(object sender, RoutedEventArgs e)
        {
            Encryption.algParams[0] = 0;
            DrawSettings();
        }
        private void KeyTypePass(object sender, RoutedEventArgs e)
        {
            Encryption.algParams[0] = 1;
            DrawSettings();
        }
        private void ChipherType1D(object sender, RoutedEventArgs e)
        {
            Encryption.algParams[3] = 1;
            DrawSettings();
            if (Encryption.currentAlg == 0 && uploadedImg.Source != null)
                TranspositionAlg.SetKey(Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource));
        }
        private void ChipherType2D(object sender, RoutedEventArgs e)
        {
            Encryption.algParams[3] = 2;
            DrawSettings();
            if (Encryption.currentAlg == 0 && uploadedImg.Source != null)
                TranspositionAlg.SetKey(Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource));
        }
        private void InBlockEncription(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
                Encryption.algParams[1] = 1;
            else
                Encryption.algParams[1] = 0;

            DrawSettings();
        }
        private void GenerateRng(object sender, RoutedEventArgs e)
        {
            window.Cursor = Cursors.Wait;
            int bytesPerPixel = Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource);
            if (Encryption.currentAlg == 0)
                TranspositionAlg.Generate((sender as Button).Name, Int32.Parse((sender as Button).Uid), null);
            else
                SymetricAlg.Generate(null);
            window.Cursor = Cursors.Arrow;
        }
        private void GeneratePass(object sender, RoutedEventArgs e)
        {
            window.Cursor = Cursors.Wait;
            if (currentAction == 1)
                decrypted = false;
            int bytesPerPixel = Encryption.CountBytesPerPixel(uploadedImg.Source as BitmapSource);
            if (Encryption.currentAlg == 0)
                TranspositionAlg.Generate((sender as PasswordBox).Name, Int32.Parse((sender as PasswordBox).Uid), (sender as PasswordBox).Password);
            else
                SymetricAlg.Generate((sender as PasswordBox).Password);
            window.Cursor = Cursors.Arrow;
        }
        private void PlusButton(object sender, RoutedEventArgs e)
        {
            if (Encryption.algParams[1] >= 128)
                return;
            Encryption.algParams[1] += 8;

            DrawSettings();
        }
        private void MinusButton(object sender, RoutedEventArgs e)
        {
            if (Encryption.algParams[1] <= 40)
                return;
            Encryption.algParams[1] -= 8;

            DrawSettings();
        }
        private void BlockSize(object sender, RoutedEventArgs e)
        {
            int index = Encryption.currentAlg == 0 ? 2 : 1;
            Encryption.algParams[index] = Convert.ToInt32((sender as ComboBox).SelectedValue);

            DrawSettings();
            e.Handled = true;
        }
        private void OpenImageInWindow(object sender, RoutedEventArgs e)
        {
            if ((sender as Image).Source == null)
                return;
            var imgWindow = new ImgWindow();
            imgWindow.Background = new ImageBrush((sender as Image).Source);
            imgWindow.Show();
        }

        public static void ShowMessageBox(string message, string type)
        {
            var messageBox = new MessageBoxWindow();
            messageBox.text.Text = message;
            if (type == "Error")
            {
                messageBox.window.Style = messageBox.Resources["ErrorWindow"] as Style;
                messageBox.button.Style = messageBox.Resources["ErrorButton"] as Style;
                messageBox.window.Title = "Помилка!";
            }
            else if (type == "Info")
            {
                messageBox.window.Style = messageBox.Resources["InfoWindow"] as Style;
                messageBox.button.Style = messageBox.Resources["InfoButton"] as Style;
                messageBox.window.Title = "Інфо!";
            }
            else
            {
                messageBox.window.Style = messageBox.Resources["WarningWindow"] as Style;
                messageBox.button.Style = messageBox.Resources["WarningButton"] as Style;
                messageBox.window.Title = "Увага!";
            }
            messageBox.Show();
        }
    }
}
