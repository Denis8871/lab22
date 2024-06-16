using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Lab_22_Shostya
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer autoSaveTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFirstDocument();
            InitializeAutoSaveTimer();
        }

        private void InitializeFirstDocument()
        {
            var firstTab = DocumentsTabControl.Items[0] as TabItem;
            if (firstTab != null)
            {
                var richTextBox = new RichTextBox
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(10)
                };

                firstTab.Content = richTextBox;
            }
        }

        private void InitializeAutoSaveTimer()
        {
            autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // Автозбереження кожні 5 хвилин
            };
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            SaveActiveDocument();
        }

        private void SaveActiveDocument()
        {
            TabItem selectedTab = DocumentsTabControl.SelectedItem as TabItem;
            if (selectedTab != null && selectedTab.Content is RichTextBox richTextBox)
            {
                string fileName = selectedTab.Tag as string;
                if (!string.IsNullOrEmpty(fileName))
                {
                    TextRange documentTextRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    using (var stream = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
                    {
                        documentTextRange.Save(stream, DataFormats.Rtf);
                    }
                }
                else
                {
                    SaveActiveDocumentAs(true);
                }
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            TabItem newTab = new TabItem
            {
                Header = "New Document"
            };
            RichTextBox newRichTextBox = new RichTextBox
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10)
            };
            newTab.Content = newRichTextBox;
            DocumentsTabControl.Items.Add(newTab);
            DocumentsTabControl.SelectedItem = newTab;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TabItem newTab = new TabItem
                {
                    Header = System.IO.Path.GetFileName(openFileDialog.FileName),
                    Tag = openFileDialog.FileName
                };

                RichTextBox richTextBox = new RichTextBox
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(10)
                };

                TextRange documentTextRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                using (var stream = openFileDialog.OpenFile())
                {
                    documentTextRange.Load(stream, DataFormats.Rtf);
                }

                newTab.Content = richTextBox;
                DocumentsTabControl.Items.Add(newTab);
                DocumentsTabControl.SelectedItem = newTab;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveActiveDocument();
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveActiveDocumentAs(true);
        }

        private void SaveActiveDocumentAs(bool forceDialog = false)
        {
            TabItem selectedTab = DocumentsTabControl.SelectedItem as TabItem;
            if (selectedTab != null && selectedTab.Content is RichTextBox richTextBox)
            {
                string fileName = selectedTab.Tag as string;
                if (forceDialog || string.IsNullOrEmpty(fileName))
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Rich Text Format (*.rtf)|*.rtf|All Files (*.*)|*.*"
                    };
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        fileName = saveFileDialog.FileName;
                        selectedTab.Tag = fileName;
                        selectedTab.Header = System.IO.Path.GetFileName(fileName);
                    }
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    TextRange documentTextRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    using (var stream = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
                    {
                        documentTextRange.Save(stream, DataFormats.Rtf);
                    }
                }
            }
        }

        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            ApplyTextAlignment(TextAlignment.Left);
        }

        private void AlignCenter_Click(object sender, RoutedEventArgs e)
        {
            ApplyTextAlignment(TextAlignment.Center);
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            ApplyTextAlignment(TextAlignment.Right);
        }

        private void Justify_Click(object sender, RoutedEventArgs e)
        {
            ApplyTextAlignment(TextAlignment.Justify);
        }

        private void ApplyTextAlignment(TextAlignment alignment)
        {
            TabItem selectedTab = DocumentsTabControl.SelectedItem as TabItem;
            if (selectedTab != null && selectedTab.Content is RichTextBox richTextBox)
            {
                richTextBox.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, alignment);
            }
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            TabItem selectedTab = DocumentsTabControl.SelectedItem as TabItem;
            if (selectedTab != null && selectedTab.Content is RichTextBox richTextBox)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    string imagePath = openFileDialog.FileName;
                    Image image = new Image
                    {
                        Source = new BitmapImage(new Uri(imagePath)),
                        Stretch = Stretch.Uniform,
                        Width = 200, // Початковий розмір
                        Height = 200 // Початковий розмір
                    };

                    image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    image.MouseMove += Image_MouseMove;
                    image.MouseLeftButtonUp += Image_MouseLeftButtonUp;

                    InlineUIContainer container = new InlineUIContainer(image, richTextBox.CaretPosition);
                }
            }
        }

        private bool isResizing = false;
        private Point lastPosition;

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                isResizing = true;
                lastPosition = e.GetPosition(image);
                image.CaptureMouse();
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing && sender is Image image)
            {
                Point currentPosition = e.GetPosition(image);
                double deltaX = currentPosition.X - lastPosition.X;
                double deltaY = currentPosition.Y - lastPosition.Y;

                if (Math.Abs(deltaX) > Math.Abs(deltaY))
                {
                    image.Width = Math.Max(20, image.Width + deltaX);
                }
                else
                {
                    image.Height = Math.Max(20, image.Height + deltaY);
                }

                lastPosition = currentPosition;
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                isResizing = false;
                image.ReleaseMouseCapture();
            }
        }

        private void ChangeFont_Click(object sender, RoutedEventArgs e)
        {
            Forms.FontDialog fontDialog = new Forms.FontDialog();
            if (fontDialog.ShowDialog() == Forms.DialogResult.OK)
            {
                ApplyTextProperty(TextElement.FontFamilyProperty, new FontFamily(fontDialog.Font.Name));
                ApplyTextProperty(TextElement.FontSizeProperty, (double)fontDialog.Font.Size);
            }
        }

        private void ChangeFontColor_Click(object sender, RoutedEventArgs e)
        {
            Forms.ColorDialog colorDialog = new Forms.ColorDialog();
            if (colorDialog.ShowDialog() == Forms.DialogResult.OK)
            {
                var color = colorDialog.Color;
                ApplyTextProperty(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B)));
            }
        }

        private void ApplyTextProperty(DependencyProperty property, object value)
        {
            TabItem selectedTab = DocumentsTabControl.SelectedItem as TabItem;
            if (selectedTab != null && selectedTab.Content is RichTextBox richTextBox)
            {
                richTextBox.Selection.ApplyPropertyValue(property, value);
            }
        }

        private void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string lang = menuItem.Tag.ToString();
                ChangeLanguage(lang);
            }
        }

        private void ChangeLanguage(string lang)
        {
            CultureInfo culture = new CultureInfo(lang);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Application.Current.Resources.MergedDictionaries.Clear();
            ResourceDictionary dict = new ResourceDictionary();
            switch (lang)
            {
                case "uk":
                    dict.Source = new Uri("Resources/Resources.uk.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("Resources/Resources.en.xaml", UriKind.Relative);
                    break;
            }
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
