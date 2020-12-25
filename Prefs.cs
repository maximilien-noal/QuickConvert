namespace QuickConvert
{
    public record Prefs
    {
        public bool UseSourceFolderAsDest { get; set; } = true;
        public int ShortFileNameCharLimit { get; set; } = 15;
    }
}