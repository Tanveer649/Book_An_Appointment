using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Book_An_Appointment1.Pages
{
    // Shown for unhandled errors (production) and 404 / 400 status codes — never shows technical details
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public int Code { get; private set; }

        public void OnGet(int? code) => Code = code ?? 500;

        // A failed POST (e.g. expired form) is re-executed as POST
        public void OnPost(int? code) => Code = code ?? 500;
    }
}
