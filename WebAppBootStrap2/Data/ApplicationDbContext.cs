using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace WebAppBootStrap2.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<FileLocationAndType> FileLocationAndTypes { get; set; }
    }
}
