using Chirp.Infrastructure;
using Chirp.Core;
using Chirp.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// have removed all logic checking which "environment" we are running in. so no azure db anymore see older commits if needed
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

var dbPath = Path.Combine(dataDir, "Chirp.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<CheepDbContext>(options =>
    options.UseSqlite(connectionString));

//changed login requirements to be more lenient to allow emails that are specefied in the simulator csv file.
builder.Services.AddDefaultIdentity<Author>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 1;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;

        // Allow emails like "test@test"
        options.User.RequireUniqueEmail = false;

        options.Lockout.AllowedForNewUsers = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.@ "; 
        //do not change the whitespace!, allows for spaces in the username which is required for the simulator csv file.
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

builder.Services.AddControllers();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromHours(1);
});

builder.Services.AddAuthentication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    using var context = scope.ServiceProvider.GetRequiredService<CheepDbContext>();

    context.Database.EnsureCreated();

   //DbInitializer.SeedDatabase(context); // this is no longer needed when runnign test
   //initial data is seeded with the simulator, might need it later not sure. 
}

// No HTTPS redirect since the simulator uses http
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllers();
app.MapRazorPages();

app.Run();