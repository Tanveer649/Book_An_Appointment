using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.Handlers;
using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Models.Settings;
using Book_An_Appointment1.Security;
using Book_An_Appointment1.Services;
using Book_An_Appointment1.Services.Implementations;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;


var builder = WebApplication.CreateBuilder(args);

// Daily log files in logs/ — only the last FileLogging:RetainDays days are kept (Helpers/FileLogger.cs)
builder.Logging.AddPortalFileLogging(builder.Configuration, builder.Environment);

// No "Server: Kestrel" header (do not advertise the stack)
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // Secure on https (production)
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Keys that sign/encrypt antiforgery tokens, the session cookie and appointment slip links.
// Kept on disk (not only in memory) so an app-pool recycle / restart does not break slip links patients already have.
// Several servers behind a load balancer: point DataProtection:KeysPath of all of them to the same shared folder.
// Nothing to set up: the folder is created automatically. If the site cannot write there (IIS permissions),
// the portal still starts and uses ASP.NET's default key storage instead — see the warning in the log.


var keysPath = builder.Configuration["DataProtection:KeysPath"];
var keysDir = new DirectoryInfo(string.IsNullOrWhiteSpace(keysPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")
    : keysPath);
var keysWritable = CanWrite(keysDir);
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("BookAnAppointmentPortal");
if (keysWritable)
{
    dataProtection.PersistKeysToFileSystem(keysDir);
    if (OperatingSystem.IsWindows())
        dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);   // key files are encrypted at rest
}

builder.Services.AddTransient<AuthHandler>();

// A hanging hospital API must not keep requests open for the 100 s default
var apiTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("ApiSettings:TimeoutSeconds", 30));

builder.Services.AddHttpClient("TokenClient", client =>
{
    client.BaseAddress = new Uri(
    builder.Configuration["ApiSettings:BaseUrl"]!);
    client.Timeout = apiTimeout;
});

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(
    builder.Configuration["ApiSettings:BaseUrl"]!);
    client.Timeout = apiTimeout;
})
.AddHttpMessageHandler<AuthHandler>();

// Doctor photos proxied by the Appointment page (?handler=DoctorImage)
builder.Services.AddHttpClient("DoctorImageClient", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { MaxAutomaticRedirections = 3 });

builder.Services.Configure<HospitalSettings>(
    builder.Configuration.GetSection("HospitalSettings"));

builder.Services.AddMemoryCache();

// Hospital theme colours: appsettings ThemeSettings:Theme → wwwroot/themes/{name}.css
builder.Services.AddSingleton<Book_An_Appointment1.Helpers.ThemeProvider>();

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

// ── Security: OTP / booking request limits (appsettings RateLimits) ──
builder.Services.AddPortalRateLimiting(builder.Configuration);

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
builder.Services.AddScoped<IAppointmentSlipService, AppointmentSlipService>();
builder.Services.AddScoped<IDoctorDirectory, DoctorDirectory>();
builder.Services.AddScoped<IPatientRegistrationService, PatientRegistrationService>();
builder.Services.AddSingleton<ISlipIndex, SlipIndex>();   // "Find my slip" store (App_Data/slips)

var app = builder.Build();

foreach (var code in languageCatalog.InvalidCodes)
    app.Logger.LogWarning("LanguageSettings: unknown or duplicate language code '{Code}' — skipped", code);

// API credentials come from appsettings.json → ApiSettings (set per client deployment)
if (string.IsNullOrWhiteSpace(app.Configuration["ApiSettings:UserName"]) ||
    string.IsNullOrWhiteSpace(app.Configuration["ApiSettings:Password"]))
{
    app.Logger.LogCritical("ApiSettings:UserName / ApiSettings:Password are not configured (Environment={Env}) — hospital API calls will fail. Set them in appsettings.json → ApiSettings",
        app.Environment.EnvironmentName);
}
else
{
    app.Logger.LogInformation("API credentials loaded | Environment={Env}", app.Environment.EnvironmentName);
}

if (keysWritable)
    app.Logger.LogInformation("Data Protection keys folder: {Path}", keysDir.FullName);
else
    app.Logger.LogWarning("Data Protection keys folder {Path} is not writable — using ASP.NET's default key storage. " +
        "Slip links / QR codes may stop working after an app-pool restart. Give the app-pool user write access to it " +
        "(or set DataProtection:KeysPath). See Docs/Deploy-New-Client.md section 6.", keysDir.FullName);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Friendly page for 404 / 400 etc. (JSON responses with a body are left untouched)
app.UseStatusCodePagesWithReExecute("/error", "?code={0}");

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRequestLocalization();

app.UseRateLimiter();

// Session before the booking flow: /appointment|patientdetails|reviewconfirm/{id} is checked against the session (Security/BookingFlow.cs)
app.UseSession();

app.UseMiddleware<BookingFlowMiddleware>();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context =>
{
    context.Response.Redirect(BookingFlow.BasePath);
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

// Keys folder usable? Creates it if needed and tries a small test write.
static bool CanWrite(DirectoryInfo dir)
{
    try
    {
        dir.Create();
        var probe = Path.Combine(dir.FullName, ".write-test");
        File.WriteAllText(probe, "");
        File.Delete(probe);
        return true;
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
    {
        return false;
    }
}
