using Dapper;
using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

DefaultTypeMap.MatchNamesWithUnderscores = true;
SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
SqlMapper.AddTypeHandler(new DapperTimeSpanHandler());

builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<HospitalRepository>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "HMS.AntiForgery";
    options.FormFieldName = "__RequestVerificationToken";
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "HMS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "This field is required.");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<HospitalRepository>();
    await repository.EnsureFeatureSchemaAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var isStaticAsset =
        path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".map", StringComparison.OrdinalIgnoreCase);

    if (!isStaticAsset)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/healthz", () => Results.Ok("OK"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
