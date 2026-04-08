using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Resend;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;
using TodoView.Services;
using TodoView.Authorization;
using TodoView.Data;
using TodoView.Data.Seed;
using TodoView.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String handling
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Identity & Roles
builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TodoDbContext>();

builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));

// 3. Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
});

// 4. Razor Pages Configuration
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Todos");
    options.Conventions.AuthorizeFolder("/Admin/Users", AppRoles.Admin);
});

// 5. Email & Resend Integration
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    // Ensure this matches your Render Environment Variable: EmailSettings__ApiKey
    o.ApiToken = builder.Configuration["EmailSettings:ApiKey"]!;
});

builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<ReminderScheduler>();
builder.Services.AddTransient<ReminderDispatchService>();

// 6. Hangfire Background Jobs
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => 
        {
            // This uses your main connection string
            options.UseNpgsqlConnection(connectionString);
        }, new PostgreSqlStorageOptions 
        {
            // If your tables are in a schema other than 'public', change this name:
            SchemaName = "public", 
            PrepareSchemaIfNecessary = true, // Attempt to create tables if missing
            QueuePollInterval = TimeSpan.FromSeconds(15)
        }));

builder.Services.AddHangfireServer();

var app = builder.Build();

// 7. Initialize Database (Modified for Render)
await InitializeDatabaseAsync(app);

// 8. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
// CRITICAL: Only use HttpsRedirection locally. Render handles SSL for you.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseRouting();

// Dashboard for background jobs
app.UseHangfireDashboard("/hangfire");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/Todos"));
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

// Updated Helper Method
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // NOTE: MigrateAsync() is removed here because you have already migrated manually.
        // This prevents the common "Status 139" crash on startup in cloud environments.
        
        logger.LogInformation("Attempting to seed Identity data...");
        await IdentitySeeder.SeedAsync(services);
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        // We log the error but allow the app to keep running.
        // This prevents a database connection flicker from killing your web service.
        logger.LogError(ex, "An error occurred while seeding the database. The app will continue to start.");
    }
}