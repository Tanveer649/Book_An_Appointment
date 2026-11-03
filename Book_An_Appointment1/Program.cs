using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.Handlers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Models.Settings;
using Book_An_Appointment1.Services;
using Book_An_Appointment1.Services.Implementations;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddTransient<AuthHandler>();

builder.Services.AddHttpClient("TokenClient", client =>
{
    client.BaseAddress = new Uri(
    builder.Configuration["ApiSettings:BaseUrl"]!);
});

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(
    builder.Configuration["ApiSettings:BaseUrl"]!);
})
.AddHttpMessageHandler<AuthHandler>();

builder.Services.Configure<HospitalSettings>(
    builder.Configuration.GetSection("HospitalSettings"));

builder.Services.AddMemoryCache();

// ── Localization (languages from appsettings LanguageSettings) ──
var languageCatalog = new LanguageCatalog(
    builder.Configuration.GetSection("LanguageSettings").Get<LanguageSettings>() ?? new LanguageSettings());

builder.Services.AddSingleton(languageCatalog);
builder.Services.AddSingleton<JsonStringLocalizer>();
builder.Services.AddSingleton<IStringLocalizer>(sp => sp.GetRequiredService<JsonStringLocalizer>());

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(languageCatalog.CultureOf(languageCatalog.Default));
    options.SupportedCultures = languageCatalog.Cultures.ToList();
    options.SupportedUICultures = languageCatalog.Cultures.ToList();
    // Only the user's explicit choice (cookie) — browser language must not override the default
    options.RequestCultureProviders = new List<IRequestCultureProvider> { new CookieRequestCultureProvider() };
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<ApiUrlBuilder>();


builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<ISpecialityService,SpecialityService>();
builder.Services.AddScoped<IDoctorService,DoctorService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ITitleService, TitleService>();
builder.Services.AddScoped<IAppointmentBookingService, AppointmentBookingService>();

var app = builder.Build();

foreach (var code in languageCatalog.InvalidCodes)
    app.Logger.LogWarning("LanguageSettings: unknown or duplicate language code '{Code}' — skipped", code);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRequestLocalization();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Appointment");
    return Task.CompletedTask;
});

// Header language dropdown → remember the choice in the culture cookie and go back to the same page
app.MapGet("/set-language", (string? culture, string? returnUrl, HttpContext context) =>
{
    var lang = languageCatalog.Find(culture);
    if (lang != null)
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang.Code)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
    }

    // Only redirect inside this site
    var target = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
        ? returnUrl
        : "/";
    return Results.LocalRedirect(target);
});

app.Run();
app.UseStaticFiles();
