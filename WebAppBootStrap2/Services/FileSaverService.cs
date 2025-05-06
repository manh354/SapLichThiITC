using Microsoft.AspNetCore.Components.Forms;

namespace WebAppBootStrap2.Services
{
    public class FileSaverService
    {
        public async Task<string> SaveAsync(IBrowserFile file, string storagePath, long maxFileSize, string? fileName = null)
        {
            if (fileName == null)
            {
                string newFileName = Path.ChangeExtension(
                        Path.GetRandomFileName(), // Generate a random file name
                        Path.GetExtension(file.Name)); // but keep the original extension.
                string path = Path.Combine(
                    storagePath,
                    newFileName);
                Directory.CreateDirectory(Path.Combine(
                    storagePath));
                if (File.Exists(path))
                    File.Delete(path);
                await using FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
                await file.OpenReadStream(maxFileSize).CopyToAsync(fs);
                return path;
            }
            else
            {
                string path = Path.Combine(
                    storagePath,
                    fileName);
                Directory.CreateDirectory(Path.Combine(
                    storagePath));
                if (File.Exists(path))
                    File.Delete(path);
                await using FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
                await file.OpenReadStream(maxFileSize).CopyToAsync(fs);
                return path;
            }
        }
    }
}
