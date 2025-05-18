using Microsoft.EntityFrameworkCore;
using WebAppBootStrap2.Components;
using WebAppBootStrap2.Data;
using WebAppBootStrap2.Services;

namespace WebAppBootStrap2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddBlazorBootstrap();
            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            Console.WriteLine("I Dont need MYSQL");
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var DbPath = Path.Join(path, "blogging.db");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={DbPath}", b=>b.MigrationsAssembly("WebAppBootStrap2")));

            builder.Services.AddSingleton<FileSaverService>();
            builder.Services.AddScoped<FileLocationAndTypeService>();
            builder.Services.AddScoped<PurdueSchedulingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
