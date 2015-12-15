namespace Hyperbyte_Patcher
{
    class Package
    {
        public uint Version { get; set; }
        public string Name { get; set; }
        public string Localization { get; set; }
        public bool Downloaded { get; set; }
        public bool Extracted { get; set; }

        public Package()
        {
            Version = 0;
            Name = string.Empty;
            Localization = string.Empty;
            Downloaded = false;
            Extracted = false;
        }
    }
}
