namespace PassPrinter
{
    class PassFile
    {
        public const int GuidLength = 36; //00000000-0000-0000-0000-000000000000
        public const int ExtensionLength = 4; //.pdf

        public string FileName { get; set; }

        public int IndexOfFirstSpace => FileName.IndexOf(" ");

        public string FirstName => FileName.Contains(" ")
            ? FileName.Substring(0, IndexOfFirstSpace)
            : string.Empty;

        public string LastName => FileName.Contains(" ")
            ? FileName.Substring(IndexOfFirstSpace + 1, FileName.Length - GuidLength - ExtensionLength - IndexOfFirstSpace - 1).Trim()
            : string.Empty;

        public string Guid => FileName.Substring(FileName.Length - GuidLength - ExtensionLength, GuidLength);

        public PassFile(string filename)
        {
            FileName = filename;
        }
    }
}
