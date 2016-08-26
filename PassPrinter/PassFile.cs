namespace PassPrinter
{
    class PassFile
    {
        public const int ExtensionLength = 4; //.pdf
        public const char DuplicateHack = '_';

        public string FileName { get; set; }

        public int IndexOfFirstSpace => FileName.IndexOf(" ");

        public string FirstName => FileName.Contains(" ")
            ? FileName.Substring(0, IndexOfFirstSpace)
            : string.Empty;

        public string LastName => FileName.Contains(" ")
            ? FileName.Substring(IndexOfFirstSpace + 1, FileName.Length - ExtensionLength - IndexOfFirstSpace - 1)
                .Replace(DuplicateHack, ' ')
                .Trim()
            : string.Empty;

        public PassFile(string filename)
        {
            FileName = filename;
        }
    }
}
