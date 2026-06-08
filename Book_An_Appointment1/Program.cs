using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.Handlers;
using Book_An_Appointment1.Models.Settings;
using Book_An_Appointment1.Services;
using Book_An_Appointment1.Services.Implementations;
using Book_An_Appointment1.Services.Interfaces;


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

builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<ApiUrlBuilder>();


builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<ISpecialityService,SpecialityService>();
builder.Services.AddScoped<IDoctorService,DoctorService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<ITokenService, TokenService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Appointment");
    return Task.CompletedTask;
});

app.Run();
app.UseStaticFiles();
