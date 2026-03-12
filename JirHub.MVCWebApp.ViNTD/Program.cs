using JirHub.Repository.ViNTD.Repositories;
using JirHub.Services.ViNTD.IServices;
using JirHub.Services.ViNTD.Services;

namespace JirHub.MVCWebApp.ViNTD
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
            
            // Add Data Protection for token encryption
            builder.Services.AddDataProtection();
            
            // Add HttpClientFactory for API calls
            builder.Services.AddHttpClient();
            
            // Register Repositories
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<ProjectConfigRepository>();
            builder.Services.AddScoped<ProjectReposRepository>();
            
            // Register Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IEncryptionService, EncryptionService>();
            builder.Services.AddScoped<IConnectionService, ConnectionService>();
            builder.Services.AddScoped<IProjectConfigService, ProjectConfigService>();

            var app = builder.Build();


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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
