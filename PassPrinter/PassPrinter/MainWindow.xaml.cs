using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace PassPrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static DirectoryInfo PDFDirectory
        {
            get
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

                while (directoryInfo != null && !directoryInfo.GetDirectories("PDFs").Any())
                {
                    directoryInfo = directoryInfo.Parent;
                }

                return directoryInfo?.GetDirectories("PDFs").FirstOrDefault();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            RenamePDFs();
        }

        private void RenamePDFs()
        {
            FileInfo[] files = PDFDirectory?.GetFiles("*.pdf");

            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    RenamePDF(file);
                }
            }
        }

        private void RenamePDF(FileInfo file)
        {
            string attendeeName = GetAttendeeName(file.FullName);

            if (!string.IsNullOrWhiteSpace(attendeeName)
                && file.Name.Length == PassFile.GuidLength + PassFile.ExtensionLength)
            {
                string newFileName = $"{file.DirectoryName}\\{attendeeName} {file.Name}";
                File.Move(file.FullName, newFileName);
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
    }
}
