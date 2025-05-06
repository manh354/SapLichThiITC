namespace SapLichThiITCInputHelper
{
    public static class FileHelper
    {
        public static string GetFilePath(string fileName)
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(folderPath, fileName);
        }

        public static bool FileExists(string fileName)
        {
            string filePath = GetFilePath(fileName);
            return File.Exists(filePath);
        }

        public static string ReadFileContent(string fileName)
        {
            string filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                throw new FileNotFoundException($"File '{fileName}' not found at {filePath}");
            }
        }

        public static void WriteToFile(string fileName, string content)
        {
            string filePath = GetFilePath(fileName);
            File.WriteAllText(filePath, content);
        }
    }
}
