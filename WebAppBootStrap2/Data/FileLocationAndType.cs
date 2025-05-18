namespace WebAppBootStrap2.Data
{
    public class FileLocationAndType
    {
        public int Id { get; set; } = 0;
        public required string Name { get; set; }
        public required string FileLocation { get; set; }
        public FileType FileType { get; set; }
    }

    public enum FileType
    {
        Normal,
        Purdue,
        Kaggle,
    }

    public static class FileTypeExtension
    {
        public static string GetFileTypeName(this FileType? fileType)
        {
            switch (fileType)
            {
                case FileType.Normal:
                    return "Bộ dữ liệu ITC 2007 track 1";
                case FileType.Purdue:
                    return "Bộ dữ liệu Purdue";
                case FileType.Kaggle:
                    return "Bộ dữ liệu Kaggle";
                default:
                    return string.Empty;
            }
        }
    }
}
