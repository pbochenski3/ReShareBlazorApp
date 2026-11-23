using Microsoft.AspNetCore.Authentication.Cookies;
using ReShareBlazorApp.Web.ApiServices;
using ReShareBlazorApp.Web.Components;
using ReShareBlazorApp.Web.Endpoints;
using ReShareBlazorApp.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseAddress"]!);
});

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("API");
});

builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ProductApiService>();
builder.Services.AddScoped<RecentlyViewedApiService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<NotificationApiService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<ReviewApiService>();
builder.Services.AddScoped<ReportApiService>();
builder.Services.AddScoped<ContactApiService>();
builder.Services.AddScoped<AdminApiService>();
builder.Services.AddScoped<PromoBannerApiService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ReShare.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles(); // Upewnij się, że masz standardowe statyczne pliki (jeśli potrzebne)
app.MapStaticAssets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware blokady systemowej (Lockdown) uruchamiamy po autoryzacji
app.UseLockdown();

app.UseAntiforgery();

// Mapowanie endpointów
app.MapAuthEndpoints();
app.MapAvatarEndpoints();

// Zoptymalizowany endpoint proxy dla zdjęć (używa strumieniowania zamiast ładowania do RAM)
app.MapGet("/images/{id:int}", async (
    int id,
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient("API");

    // Używamy HttpCompletionOption.ResponseHeadersRead, aby nie pobierać od razu całego ciała response do pamięci
    var response = await client.GetAsync($"api/product/image/{id}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    if (!response.IsSuccessStatusCode)
        return Results.NotFound();

    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

    context.Response.Headers.CacheControl = "public, max-age=86400";
    context.Response.Headers.XContentTypeOptions = "nosniff";

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    return Results.Stream(stream, contentType);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();