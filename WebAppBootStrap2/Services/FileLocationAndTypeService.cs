using Microsoft.EntityFrameworkCore;
using WebAppBootStrap2.Data;

namespace WebAppBootStrap2.Services
{
    public class FileLocationAndTypeService(ApplicationDbContext context)
    {
        public async Task<int> AddFileLocation(FileLocationAndType fileLocationAndType)
        {
            await context.FileLocationAndTypes.AddAsync(fileLocationAndType);
            await context.SaveChangesAsync();
            return fileLocationAndType.Id;
        }

        public async Task<FileLocationAndType?> GetFileLocation(int id)
        {
            return await context.FileLocationAndTypes.FindAsync(id);
        }

        public async Task<List<FileLocationAndType>> GetFileLocations()
        {
            var result = await context.FileLocationAndTypes.ToListAsync();
            return result;
        }
    }
}
