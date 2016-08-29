using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PassPrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties

        private const int GuidLength = 36; //00000000-0000-0000-0000-000000000000
        private static string CurrentDirectory { get; set; }
        private static DirectoryInfo PDFDirectory { get; set; }
        private static DirectoryInfo CustomImagesDirectory { get; set; }

        private enum Directories
        {
            CustomImages,
            PDFs
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CurrentDirectory = GetCurrentDirectory();
            PDFDirectory = GetDirectory(Directories.PDFs);
            CustomImagesDirectory = GetDirectory(Directories.CustomImages);
            RenamePDFs();
            LoadCustomImages();
            txtInput.Focus();
        }

        #region Methods

        private static string GetCurrentDirectory()
        {
            string currentDirectory = string.Empty;

            try
            {
                currentDirectory = Assembly.GetExecutingAssembly().Location;
                currentDirectory = currentDirectory?.Substring(0, currentDirectory.LastIndexOf("\\"));
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "getting current directory");
            }

            return currentDirectory;
        }

        private static void ShowErrorMessage(Exception ex, string process)
        {
            MessageBox.Show(ex.Message, $"Error in {process}");
        }

        private static DirectoryInfo GetDirectory(Directories directory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(CurrentDirectory);

            try
            {
                while (directoryInfo != null && !directoryInfo.GetDirectories(directory.ToString()).Any())
                {
                    directoryInfo = directoryInfo.Parent;
                }

                directoryInfo = directoryInfo?.GetDirectories(directory.ToString()).First();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, $"getting {directory} directory");
            }

            return directoryInfo;
        }

        private static void RenamePDFs()
        {
            if (PDFDirectory == null)
            {
                string PDFDirectoryNotFoundErrorMessage = $"{Directories.PDFs} Directory not found.\nCurrent Directory = {CurrentDirectory}";
                MessageBox.Show(PDFDirectoryNotFoundErrorMessage, $"Error in getting {Directories.PDFs} directory");
            }
            else
            {
                try
                {
                    FileInfo[] files = PDFDirectory.GetFiles($"*{PassFile.Extension}");

                    foreach (FileInfo file in files)
                    {
                        RenamePDF(file);
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "renaming PDFs");
                }
            }
        }

        private static void RenamePDF(FileInfo file)
        {
            if (file.Name.Length == GuidLength + PassFile.Extension.Length)
            {
                string attendeeName = GetAttendeeName(file.FullName);

                if (!string.IsNullOrWhiteSpace(attendeeName))
                {
                    string newFileName = $"{file.DirectoryName}\\{attendeeName}";

                    while (File.Exists($"{newFileName}{PassFile.Extension}"))
                    {
                        newFileName += PassFile.DuplicateHack;
                    }

                    File.Move(file.FullName, $"{newFileName}{PassFile.Extension}");
                }
            }
        }

        private static string GetAttendeeName(string fileName)
        {
            string text = string.Empty;

            try
            {
                PdfReader pdfReader = new PdfReader(fileName);

                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                text = PdfTextExtractor.GetTextFromPage(pdfReader, 1, strategy);

                text = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text)));

                pdfReader.Close();

                if (text.Contains("\n"))
                {
                    text = text.Substring(0, text.IndexOf("\n")).Trim();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "getting attendee name");
            }

            return text;
        }

        private void LoadCustomImages()
        {
            if (CustomImagesDirectory != null)
            {
                FileInfo headerFile = CustomImagesDirectory.GetFiles("*header*").FirstOrDefault();

                if (headerFile != null)
                {
                    header.Source = (ImageSource)new ImageSourceConverter().ConvertFromString(headerFile.FullName);
                }

                FileInfo backgroundFile = CustomImagesDirectory.GetFiles("*background*").FirstOrDefault();

                if (backgroundFile != null)
                {
                    ImageSource backgroundSource = (ImageSource)new ImageSourceConverter().ConvertFromString(backgroundFile.FullName);

                    Images.Background = new ImageBrush()
                    {
                        ImageSource = backgroundSource
                    };
                }
            }
        }

        private void Search(string input)
        {
            if (PDFDirectory == null)
            {
                string PDFDirectoryNotFoundErrorMessage = $"{Directories.PDFs} Directory not found.\nCurrent Directory = {CurrentDirectory}";
                MessageBox.Show(PDFDirectoryNotFoundErrorMessage, $"Error in getting {Directories.PDFs} directory");
            }
            else
            {
                try
                {
                    FileInfo[] files = PDFDirectory.GetFiles($"*{input}*{PassFile.Extension}");

                    List<PassFile> passFiles = files.Select(f => new PassFile(f.Name)).ToList();
                    grdPDFs.ItemsSource = passFiles;

                    if (passFiles.Count == 1)
                    {
                        PreviewPDF(passFiles.First());
                    }
                    else
                    {
                        PDFPreview.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "searching PDFs");
                }
            }
        }

        private void Clear()
        {
            grdPDFs.ItemsSource = new List<PassFile>();
            PDFPreview.Visibility = Visibility.Collapsed;
            txtInput.Clear();
            txtInput.Focus();
        }

        private void PreviewPDF(PassFile file)
        {
            try
            {
                MainWindowStackPanel.IsEnabled = false;
                string fileName = file.GetFullPath(PDFDirectory);

                Uri url = new Uri($"file:///{fileName}", UriKind.Absolute);
                PDFPreview.Navigate(url);
                PDFPreview.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "previewing PDF");
            }
        }

        private static void PrintPDF(PassFile file)
        {
            try
            {
                string fileName = file.GetFullPath(PDFDirectory);

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        CreateNoWindow = true,
                        Verb = "print",
                        FileName = fileName
                    }
                };

                process.Start();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "printing PDF");
            }
        }

        #endregion

        #region Event Handlers

        private void txtInput_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                Clear();
            }
            else if (txtInput.Text.Trim().Length >= 3 && grdPDFs.Items.Count != 1)
            {
                Search(txtInput.Text);
            }
        }

        private void btnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            Search(txtInput.Text);
            lblMessage.Content = string.Empty;
        }

        private void btnClear_OnClick(object sender, RoutedEventArgs e)
        {
            Clear();
            lblMessage.Content = string.Empty;
        }

        private void btnPreviewPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            PreviewPDF(file);
        }

        private void btnPrintPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;

            if (file != null)
            {
                PrintPDF(file);
                lblMessage.Content = $"{file.FullName}, your pass is in the queue to be printed!";
                Clear();
            }
        }

        private void PDFPreview_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            MainWindowStackPanel.IsEnabled = true;
        }

        private void grdPDFs_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            PassFile file = grdPDFs.SelectedItem as PassFile;

            if (file != null)
            {
                PreviewPDF(file);
            }
        }

        private void grdPDFs_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PassFile file = grdPDFs.SelectedItem as PassFile;

            if (file != null && MessageBox.Show($"Are you sure that you want to print the pass for {file.FullName}?",
                                                "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                PrintPDF(file);
                lblMessage.Content = $"{file.FullName}, your pass is in the queue to be printed!";
                Clear();
            }
        }

        #endregion
    }
}
