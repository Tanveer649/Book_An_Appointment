using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace Book_An_Appointment1.Security
{
    public enum BookingStep { Appointment = 1, PatientDetails = 2, Review = 3 }

    // Booking wizard URLs: /appointment/{id}, /patientdetails/{id}, /reviewconfirm/{id}
    // • id = 64-char random value, bound to the visitor's session.
    // • Only ONE URL is valid at a time: the current step + its current id. Every move (Continue, Back, Edit)
    //   happens on the server through the page buttons and issues a new id, so every older URL stops working.
    // • Editing the address bar (page name, id, browser Back to an old URL) always lands on the current step.
    public static class BookingFlow
    {
        public const string BasePath = "/book";   // entry point → current step
        private const string StepKey = "Flow.Step";
        private const string IdKey = "Flow.Id";
        private const string PublicPathItem = "Flow.PublicPath";
        private const int IdBytes = 48;            // 384-bit → 64 URL characters

        // Address-bar name and internal Razor Page route of each step
        private static readonly Dictionary<BookingStep, (string Slug, string Page)> Steps = new()
        {
            [BookingStep.Appointment] = ("appointment", "/book-appointment"),
            [BookingStep.PatientDetails] = ("patientdetails", "/patient-details"),
            [BookingStep.Review] = ("reviewconfirm", "/review-confirm")
        };

        // Internal routes and old addresses — never open a page directly
        private static readonly HashSet<string> BlockedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/book-appointment", "/patient-details", "/review-confirm",
            "/PatientDetails/PatientDetails", "/ReviewAndConfirmation/ReviewAndConfirmation"
        };

        public static BookingStep CurrentStep(ISession session) =>
            (BookingStep)Math.Clamp(session.GetInt32(StepKey) ?? 1, 1, 3);

        // The only URL that works right now, e.g. /patientdetails/Xq3…(64 chars)
        //Whenever a user tries to access an invalid URL, you can redirect them to the correct current URL.
        public static string CurrentUrl(HttpContext context) =>
            $"{context.Request.PathBase}/{Steps[CurrentStep(context.Session)].Slug}/{CurrentId(context.Session)}";

        // Server-side move (a completed step or a Back/Edit button) — new id, all older URLs die
        public static void MoveTo(ISession session, BookingStep step)
        {
            session.SetInt32(StepKey, (int)step);
            session.SetString(IdKey, NewId());
        }

        // Booking finished — next visit starts a fresh flow
        public static void Reset(ISession session)
        {
            session.Remove(StepKey);
            session.Remove(IdKey);
        }

        // Address-bar path of this request (before it was mapped to the internal page)
        public static string PublicPath(HttpContext context) =>
            context.Items[PublicPathItem] as string ?? $"{context.Request.PathBase}{context.Request.Path}";

        // ── Middleware helpers ───────────────────────────────
        internal static bool IsBlocked(string path) => BlockedPaths.Contains(path.TrimEnd('/'));

        // "/patientdetails/{id}" → step when the slug is a step name (id checked separately)
        internal static bool TryParse(string path, out BookingStep step, out string id)
        {
            step = default;
            id = "";
            var parts = path.Trim('/').Split('/');
            if (parts.Length is < 1 or > 2) return false;

            foreach (var (s, info) in Steps)
            {
                if (!parts[0].Equals(info.Slug, StringComparison.OrdinalIgnoreCase)) continue;
                step = s;
                id = parts.Length == 2 ? parts[1] : "";
                return true;
            }
            return false;
        }

        internal static bool IsCurrent(ISession session, BookingStep step, string id)
        {
            var current = session.GetString(IdKey);
            return step == CurrentStep(session) &&
                   !string.IsNullOrEmpty(current) && id.Length == current.Length &&
                   CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(id), Encoding.ASCII.GetBytes(current));
        }

        internal static void MapToPage(HttpContext context, BookingStep step)
        {
            context.Items[PublicPathItem] = $"{context.Request.PathBase}{context.Request.Path}";
            context.Request.Path = Steps[step].Page;
        }

        private static string CurrentId(ISession session)
        {
            var id = session.GetString(IdKey);
            if (string.IsNullOrEmpty(id))
            {
                id = NewId();
                session.SetString(IdKey, id);
            }
            return id;
        }

        private static string NewId() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(IdBytes));
    }

    public static class BookingFlowPageExtensions
    {
        // Move the visitor to a step on the server and redirect to its new URL (Continue / Back / Edit)
        public static RedirectResult GoToStep(this PageModel page, BookingStep step)
        {
            BookingFlow.MoveTo(page.HttpContext.Session, step);
            return new RedirectResult(BookingFlow.CurrentUrl(page.HttpContext));
        }
    }

    // Serves the current step at its URL; everything else is sent to the current step
    public class BookingFlowMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BookingFlowMiddleware> _logger;

        public BookingFlowMiddleware(RequestDelegate next, ILogger<BookingFlowMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            if (BookingFlow.TryParse(path, out var step, out var id))
            {
                await context.Session.LoadAsync();
                if (!BookingFlow.IsCurrent(context.Session, step, id))
                {
                    _logger.LogWarning("Booking flow: URL is not the current step | Requested={Step}", step);
                    await SendToCurrentStep(context);
                    return;
                }

                // Never cached: browser Back / bfcache must not show an older step
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                BookingFlow.MapToPage(context, step);
            }
            else if (path.TrimEnd('/').Equals(BookingFlow.BasePath, StringComparison.OrdinalIgnoreCase) || BookingFlow.IsBlocked(path))
            {
                await SendToCurrentStep(context);
                return;
            }

            await _next(context);
        }

        private static async Task SendToCurrentStep(HttpContext context)
        {
            await context.Session.LoadAsync();
            var target = BookingFlow.CurrentUrl(context);

            // Page scripts (fetch with JSON) get the same answer shape as an expired session
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) &&
                (context.Request.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                var localizer = context.RequestServices.GetRequiredService<Localization.JsonStringLocalizer>();
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = localizer.Text("Errors.SessionExpired"),
                    redirectUrl = target
                });
                return;
            }

            // Browser navigation / form post → 303 so a POST becomes a GET of the current step
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
            context.Response.Headers.Location = target;
        }
    }
}
