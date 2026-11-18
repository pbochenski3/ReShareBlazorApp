using Microsoft.AspNetCore.Authentication.Cookies;
using ReShareBlazorApp.Web.ApiServices;
using ReShareBlazorApp.Web.Components;
using ReShareBlazorApp.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress =
    new Uri(builder.Configuration["ApiSettings:BaseAddress"]!);
});
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("API");
});
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ProductApiService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<ReviewApiService>();
builder.Services.AddScoped<ReportApiService>();
builder.Services.AddScoped<ContactApiService>();
builder.Services.AddScoped<AdminApiService>();
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthEndpoints();
// Zdjęcia ogłoszeń pobierane z API i serwowane pod tym samym adresem co aplikacja
app.MapGet("/images/{id:int}", async (
    int id,
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient("API");
    using var response = await client.GetAsync($"api/product/image/{id}", cancellationToken);
    if (!response.IsSuccessStatusCode)
        return Results.NotFound();

    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

    context.Response.Headers.CacheControl = "public,max-age=86400";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    return Results.File(bytes, contentType);
});
app.MapAvatarEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
