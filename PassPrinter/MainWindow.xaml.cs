using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PassPrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int GuidLength = 36; //00000000-0000-0000-0000-000000000000
        public static DirectoryInfo PDFDirectory { get; set; }
        public static string CurrentDirectory { get; set; }

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
            string currentDirectory = Assembly.GetExecutingAssembly().Location;
            currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf("\\"));
            return currentDirectory;
        }

        private static DirectoryInfo GetPDFDirectory()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(CurrentDirectory);

            while (directoryInfo != null && !directoryInfo.GetDirectories("PDFs").Any())
            {
                directoryInfo = directoryInfo.Parent;
            }

            directoryInfo = directoryInfo?.GetDirectories("PDFs").First();

            return directoryInfo;
        }

        private void RenamePDFs()
        {
            if (PDFDirectory == null)
            {
                MessageBox.Show("PDF Directory not found.");
            }
            else
            {
                FileInfo[] files = PDFDirectory.GetFiles("*.pdf");

                foreach (FileInfo file in files)
                {
                    RenamePDF(file);
                }
            }
        }

        private void RenamePDF(FileInfo file)
        {
            if (file.Name.Length == GuidLength + PassFile.ExtensionLength)
            {
                string attendeeName = GetAttendeeName(file.FullName);

                if (!string.IsNullOrWhiteSpace(attendeeName))
                {
                    string newFileName = $"{file.DirectoryName}\\{attendeeName}";

                    while (File.Exists(newFileName + ".pdf"))
                    {
                        newFileName += PassFile.DuplicateHack;
                    }

                    File.Move(file.FullName, newFileName + ".pdf");
                }
            }
        }

        public string GetAttendeeName(string fileName)
        {
            string text = string.Empty;

            if (File.Exists(fileName))
            {
                PdfReader pdfReader = new PdfReader(fileName);

                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                text = PdfTextExtractor.GetTextFromPage(pdfReader, 1, strategy);

                text = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text)));

                pdfReader.Close();

                if (text.Contains("\n"))
                {
                    text = text.Substring(0, text.IndexOf("\n")).Trim();
                }
            }

            return text;
        }

        public void Search(string input)
        {
            if (PDFDirectory == null)
            {
                MessageBox.Show("PDF Directory not found.");
            }
            else
            {
                FileInfo[] files = PDFDirectory.GetFiles($"*{input}*.pdf");

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
            MainWindowStackPanel.IsEnabled = false;
            string fileName = $"{PDFDirectory.FullName}\\{file.FileName}";

            Uri url = new Uri($"file:///{fileName}", UriKind.Absolute);
            PDFPreview.Navigate(url);
            PDFPreview.Visibility = Visibility.Visible;
        }

        private void btnOpenPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            OpenPDF(file);
            Clear();
        }

        private void OpenPDF(PassFile file)
        {
            string fileName = $"{PDFDirectory.FullName}\\{file.FileName}";

            Process process = new Process
            {
                StartInfo = { FileName = fileName }
            };

            process.Start();
            process.WaitForExit();
            txtInput.Focus();
        }

        private void btnPrintPDF_OnClick(object sender, RoutedEventArgs e)
        {
            PassFile file = (sender as Button).DataContext as PassFile;
            PrintPDF(file);
            Clear();
        }

        private static void PrintPDF(PassFile file)
        {
            string fileName = $"{PDFDirectory.FullName}\\{file.FileName}";

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

        private void PDFPreview_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            MainWindowStackPanel.IsEnabled = true;
        }
    }
}
