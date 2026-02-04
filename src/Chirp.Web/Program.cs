using Chirp.Infrastructure;
using Chirp.Core;
using Chirp.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "";
// Check if code is running in production environment (like Azure)
if (builder.Environment.IsProduction())
{
    //ChatGPT help here
    connectionString = builder.Configuration.GetConnectionString("AzureSQL")
        ?? Environment.GetEnvironmentVariable("SQLAZURECONNSTR_AzureSQL")
                       ?? throw new InvalidOperationException(
                           "AzureSQL connection string not found.  Configure it in Azure Portal.");
    // to here
    builder.Services.AddDbContext<CheepDbContext>(options => 
        options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: new List<int> { 0 });
                sqlOptions.CommandTimeout(60);
            }));
} 
else 
{ 
    connectionString = builder. Configuration.GetConnectionString("DefaultConnection")
                       ??  "Data Source=Chirp.db";
    builder.Services.AddDbContext<CheepDbContext>(options => options.UseSqlite(connectionString));
} 

// Adds the Identity services to the DI container and uses a custom user type, ApplicationUser
builder.Services.AddDefaultIdentity<Author>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.@";
    })
    .AddEntityFrameworkStores<CheepDbContext>();

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<ICheepService, CheepService>();
builder.Services.AddScoped<ICheepRepository, CheepRepository>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();

// To use HTTP Session cookies to handle if a user is logged into our system
builder.Services.AddDistributedMemoryCache();
// Used from microsofts documentation here
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HSTS necessary for the HSTS header for reasons
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromHours(1);
});

var authBuilder = builder.Services.AddAuthentication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    using var context =
        scope.ServiceProvider.GetRequiredService<CheepDbContext>();
    if (app.Environment.IsProduction())
    {
        //For Azure SQL: applies SQL server migration
        context.Database.Migrate();
    }
    else
    {
        //For localhost/testing: create Schema directly
        //context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
    
    if (app.Environment.EnvironmentName != "Testing")
    {
        DbInitializer.SeedDatabase(context);
    }
}

// if(app.Environment.IsProduction())
// {
//     app.UseHsts(); // Send HSTS headers, but only in production
// }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();
app.UseSession();

app.MapRazorPages();

app.Run();
