using Chirp.Infrastructure;
using Chirp.Core;
using Chirp.Infrastructure.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("DefaultConnection missing");

builder.Services.AddDbContext<CheepDbContext>(options =>
    options.UseNpgsql(connectionString));

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
builder.Services.AddMemoryCache();
// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(
        ConnectionMultiplexer.Connect(
            builder.Configuration.GetConnectionString("Redis")!),
        "DataProtection-Keys");

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromHours(1);
});

builder.Services.AddAuthentication();

Metrics.ConfigureMeterAdapter(options =>
{
    options.InstrumentFilterPredicate = instrument =>
        !instrument.Meter.Name.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    using var context = scope.ServiceProvider.GetRequiredService<CheepDbContext>();

    context.Database.Migrate();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    });
});

// No HTTPS redirect since the simulator uses http
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    var start = DateTime.UtcNow;
    await next();
    var ms = (DateTime.UtcNow - start).TotalMilliseconds;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HTTP {Method} {Path} {StatusCode} in {ElapsedMS}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        (int)ms);
});

app.UseHttpMetrics();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllers();
app.MapRazorPages();
app.MapMetrics();

app.Run();