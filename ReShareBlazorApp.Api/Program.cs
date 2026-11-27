using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using ReShareBlazorApp.Api.Data;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Hubs;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Api.Middleware;
using ReShareBlazorApp.Api.Repositories;
using ReShareBlazorApp.Api.Services;
using ReShareBlazorApp.Shared.DTOs;
using Stripe;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ReshareDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(180);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

builder.Services
    .AddIdentity<User, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ReshareDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(
    options => options.TokenLifespan = TimeSpan.FromHours(1));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!
                )
            )
        };

        // SignalR (WebSocket) nie wysyła nagłówka Authorization - token jest w query stringu.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ReShareBlazorApp.Api.Services.ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IImageStorage, DiskImageStorage>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<IAppEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];

if (string.IsNullOrWhiteSpace(stripeSecretKey))
{
    throw new InvalidOperationException(
        "Stripe:SecretKey is not configured.");
}

builder.Services.AddSingleton(new StripeClient(stripeSecretKey));

builder.Services.AddScoped<StripeCheckoutService>();

// Czat: SignalR + szyfrowanie wiadomości
var chatEncryptionKey = builder.Configuration["Chat:EncryptionKey"];
if (string.IsNullOrWhiteSpace(chatEncryptionKey))
{
    throw new InvalidOperationException(
        "Chat:EncryptionKey is not configured.");
}

builder.Services.AddSignalR();
builder.Services.AddSingleton<IMessageEncryptionService>(new MessageEncryptionService(chatEncryptionKey));
builder.Services.AddScoped<IChatService, ChatService>();


// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
         .GetRequiredService<ReshareDbContext>();
    await LocationImportService.ImportAsync(context, app.Environment.ContentRootPath);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    await IdentitySeeder.SeedAsync(userManager, roleManager);
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ReshareDbContext>();

    await BadgeSeeder.SeedBadgesAsync(context);
}


app.UseHttpsRedirection();

var imageStorage = app.Services.GetRequiredService<IImageStorage>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageStorage.GetFolderPath("avatars")),
    RequestPath = "/avatars"
});

app.UseAuthentication();

app.UseMiddleware<UserBlockCheckMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>(ChatConstants.HubPath);

app.Run();