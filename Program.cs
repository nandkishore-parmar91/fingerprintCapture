using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using FingerprintService.Data;
using FingerprintService.Services;

SetDllDirectory(@"C:\Windows\SysWOW64");

[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
static extern bool SetDllDirectory(string lpPathName);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<FingerprintMatchService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAll");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            db.Database.EnsureCreated();
            Console.WriteLine("✅ Database connected successfully");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"⚠️ DB connection failed. Retries left: {retries}. Error: {ex.Message}");
            if (retries == 0)
            {
                Console.WriteLine("❌ Could not connect to DB. Starting without DB init.");
                break;
            }
            Thread.Sleep(3000);
        }
    }
}

app.Run("http://localhost:5001");