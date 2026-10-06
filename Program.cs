using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Implementations;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Give_Aid_NGO_Donation_Management_System.Services.Payments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Give_Aid_NGO_Donation_Management_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null)));

            // 2. Add ASP.NET Core Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // 3. Configure Cookies
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            // 4. Bind strongly typed configuration settings
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.Configure<JazzCashSettings>(builder.Configuration.GetSection("JazzCash"));
            builder.Services.Configure<EasypaisaSettings>(builder.Configuration.GetSection("Easypaisa"));
            builder.Services.Configure<CardPaymentSettings>(builder.Configuration.GetSection("CardPayment"));
            builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUpload"));
            builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection("AdminSeed"));

            // 5. Register Application Services & Repositories
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<IFileUploadService, FileUploadService>();
            builder.Services.AddScoped<IReceiptService, ReceiptService>();
            builder.Services.AddScoped<IDonationService, DonationService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            // 6. Register Payment Gateways
            builder.Services.AddScoped<IPaymentService, JazzCashPaymentService>();
            builder.Services.AddScoped<IPaymentService, EasypaisaPaymentService>();
            builder.Services.AddScoped<IPaymentService, CardPaymentService>();
            builder.Services.AddScoped<IPaymentService, BankTransferPaymentService>();
            builder.Services.AddScoped<IPaymentServiceResolver, PaymentServiceResolver>();

            // 7. Add MVC & Razor Views
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure HTTP Request Pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Map Area routes
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            // Map Default routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // 8. Auto-migrate database & run idempotent seed
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<ApplicationDbContext>();
                    if (db.Database.IsRelational())
                    {
                        try
                        {
                            db.Database.Migrate();
                        }
                        catch (Exception dbEx)
                        {
                            var logger = services.GetRequiredService<ILogger<Program>>();
                            logger.LogWarning(dbEx, "Note: Database migration encountered an issue during startup. Ensuring seed fallback...");
                        }
                    }

                    DbSeeder.SeedAsync(services).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during database migration or seeding.");
                }
            }

            app.Run();
        }
    }
}
