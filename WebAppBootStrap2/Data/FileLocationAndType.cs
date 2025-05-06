namespace WebAppBootStrap2.Data
{
    public class FileLocationAndType
    {
        public int Id { get; set; }
        public string FileLocation { get; set; }
        public FileType FileType { get; set; }
    }

    public enum FileType
    {
        Normal,
        Purdue,
        Kaggle,
    }
}
