using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Set DataDirectory to current directory so we can easily find and use our LocalDatabase.mdf
        AppDomain.CurrentDomain.SetData("DataDirectory", Directory.GetCurrentDirectory());

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Add database
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // Add database seeder as scoped service
        builder.Services.AddScoped<DatabaseSeeder>();

        // Add our custom services
        builder.Services.AddApplicationServices();

        
        // Add session 
        builder.Services.AddDistributedMemoryCache();// Required to store session data in memory
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(12);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true; // required for GDPR compliance
        });

        // Needed to access HttpContext in views/controllers/services
        builder.Services.AddHttpContextAccessor();

        // Add auth
        builder
            .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Use session
        app.UseSession();

        // Use auth
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}