using Book_An_Appointment1.Localization;
using System.Threading.RateLimiting;

namespace Book_An_Appointment1.Security
{
    // Limits per visitor (IP) for the AJAX handlers that send SMS, check OTPs or create bookings.
    // Limits come from appsettings "RateLimits"; every other request is not limited.
    public static class RateLimitingSetup
    {
        private static readonly Dictionary<string, string> HandlerPolicies = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SendOtp"] = "OtpSend",
            ["SendRegisteredOtp"] = "OtpSend",
            ["VerifyOtp"] = "OtpVerify",
            ["VerifyRegisteredOtp"] = "OtpVerify",
            ["CreateAppointment"] = "Booking"
        };

        public static IServiceCollection AddPortalRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var window = TimeSpan.FromMinutes(configuration.GetValue("RateLimits:WindowMinutes", 10));
            var limits = new Dictionary<string, int>
            {
                ["OtpSend"] = configuration.GetValue("RateLimits:OtpSend", 5),
                ["OtpVerify"] = configuration.GetValue("RateLimits:OtpVerify", 10),
                ["Booking"] = configuration.GetValue("RateLimits:Booking", 10)
            };

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var handler = context.Request.Query["handler"].ToString();
                    if (!HttpMethods.IsPost(context.Request.Method) || !HandlerPolicies.TryGetValue(handler, out var policy))
                        return RateLimitPartition.GetNoLimiter("none");

                    var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter($"{policy}:{client}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits[policy],
                        Window = window,
                        QueueLimit = 0
                    });
                });

                // Same JSON shape the page scripts already handle → message shown in the form
                options.OnRejected = async (ctx, token) =>
                {
                    var localizer = ctx.HttpContext.RequestServices.GetRequiredService<JsonStringLocalizer>();
                    ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("RateLimiting")
                        .LogWarning("Rate limit hit | Handler={H} Ip={Ip}",
                            ctx.HttpContext.Request.Query["handler"].ToString(), ctx.HttpContext.Connection.RemoteIpAddress);

                    ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await ctx.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = localizer.Text("Errors.TooManyAttempts")
                    }, token);
                };
            });

            return services;
        }
    }
}
