using System.IO;

namespace PassPrinter
{
    class PassFile
    {
        public const string Extension = ".pdf";
        public const char DuplicateHack = '_';

        public string FileName { get; set; }

        private int IndexOfFirstSpace => FileName.IndexOf(" ");

        public string FirstName => IndexOfFirstSpace > 0
            ? FileName.Substring(0, IndexOfFirstSpace)
            : FileName.Replace(Extension, "").Trim();

        public string LastName => IndexOfFirstSpace > 0
            ? FileName.Substring(IndexOfFirstSpace + 1, FileName.Length - IndexOfFirstSpace - 1)
                .Replace(Extension, "").Replace(DuplicateHack, ' ').Trim()
            : string.Empty;

        public PassFile(string filename)
        {
            FileName = filename;
        }

        public string GetFullPath(DirectoryInfo PDFDirectory) => $"{PDFDirectory?.FullName}\\{FileName}";
    }
}
