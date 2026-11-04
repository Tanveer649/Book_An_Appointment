namespace Book_An_Appointment1.Security
{
    // Browser security headers on every response.
    // CSP allows only this site + the CDNs the layout uses. 'unsafe-inline' is still needed for scripts
    // because pages use inline <script> blocks and onclick="" handlers.
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _csp;        // real host names (servers)
        private readonly string _cspLocal;   // localhost: + Visual Studio Browser Link / Hot Reload

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
            _csp = BuildPolicy("");
            // VS tooling connects to other localhost ports (any environment the developer runs, e.g. Staging)
            _cspLocal = BuildPolicy(" ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*");
        }

        private static string BuildPolicy(string extraConnect) => string.Join("; ",
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://code.jquery.com",
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com",
            "font-src 'self' https://fonts.gstatic.com data:",
            "img-src 'self' data: https:",
            // cdn.jsdelivr.net: browser DevTools load the libraries' .map source maps from there
            "connect-src 'self' https://cdn.jsdelivr.net" + extraConnect,
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");

        private static bool IsLocalhost(HttpContext context) =>
            context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";

        public Task InvokeAsync(HttpContext context)
        {
            var csp = IsLocalhost(context) ? _cspLocal : _csp;

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = csp;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers.Remove("X-Powered-By");
                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
