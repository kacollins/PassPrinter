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

        private const string PDFDirectoryName = "PDFs";
        private const int GuidLength = 36; //00000000-0000-0000-0000-000000000000
        private static DirectoryInfo PDFDirectory { get; set; }
        private static string CurrentDirectory { get; set; }
        private static string PDFDirectoryNotFoundErrorMessage => $"PDF Directory not found.\nCurrent Directory = {CurrentDirectory}";

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CurrentDirectory = GetCurrentDirectory();
            PDFDirectory = GetPDFDirectory();
            RenamePDFs();
            txtInput.Focus();
        }

        private static string GetCurrentDirectory()
        {
            string currentDirectory = string.Empty;

            try
            {
                currentDirectory = Assembly.GetExecutingAssembly().Location;
                currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf("\\"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

            return currentDirectory;
        }

        private static DirectoryInfo GetPDFDirectory()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(CurrentDirectory);

            try
            {
                while (directoryInfo != null && !directoryInfo.GetDirectories(PDFDirectoryName).Any())
                {
                    directoryInfo = directoryInfo.Parent;
                }

                directoryInfo = directoryInfo?.GetDirectories(PDFDirectoryName).First();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

            return directoryInfo;
        }

        private static void RenamePDFs()
        {
            if (PDFDirectory == null)
            {
                MessageBox.Show(PDFDirectoryNotFoundErrorMessage, "Error");
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
                    MessageBox.Show(ex.Message, "Error");
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
                MessageBox.Show(ex.Message, "Error");
            }

            return text;
        }

        private void Search(string input)
        {
            if (PDFDirectory == null)
            {
                MessageBox.Show(PDFDirectoryNotFoundErrorMessage, "Error");
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
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        private void btnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            Search(txtInput.Text);
        }

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

        private void btnClear_OnClick(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            grdPDFs.ItemsSource = new List<PassFile>();
            PDFPreview.Visibility = Visibility.Collapsed;
            txtInput.Clear();
            txtInput.Focus();
        }

        private void btnPreviewPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            PreviewPDF(file);
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
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void btnPrintPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            PrintPDF(file);
            MessageBox.Show($"{file.FullName}, your pass is in the queue to be printed!", "Success");
            Clear();
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
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void PDFPreview_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            MainWindowStackPanel.IsEnabled = true;
        }

        private void grdPDFs_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Are you sure that you want to print this pass?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                PassFile file = grdPDFs.SelectedItem as PassFile;
                PrintPDF(file);
                MessageBox.Show($"{file.FullName}, your pass is in the queue to be printed!", "Success");
                Clear();
            }
        }

    }
}
