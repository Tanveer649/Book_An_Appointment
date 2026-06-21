using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.ViewModule.ReviewAndConfirmation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Book_An_Appointment1.Pages.ReviewAndConfimation
{
    public class ReviewAndConfirmationModel : PageModel
    {
        private readonly ILogger<ReviewAndConfirmationModel> _logger;
        private readonly IConfiguration _configuration;
        public ReviewAndConfirmationViewModel Summary { get; set; } = new();

        public ReviewAndConfirmationModel(ILogger<ReviewAndConfirmationModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public IActionResult OnGet()
        {
            _logger.LogInformation("ReviewAndConfirmation OnGet started");

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);

            if (sessionData == null)
            {
                _logger.LogWarning("Session null — redirecting to Page 1");
                return RedirectToPage("/Appointment/Appointment");
            }

            // Appointment details
            Summary.Hospital = sessionData.Hospital ?? "";
            Summary.Specialisation = sessionData.Specialisation ?? "";
            Summary.DoctorName = sessionData.DoctorName ?? "";
            Summary.SlotDisplayText = sessionData.SlotDisplayText ?? "";
            Summary.SlotDate = sessionData.SlotDate ?? "";
            Summary.SlotTime = sessionData.SlotTime ?? "";
            Summary.ConsultationFee = sessionData.ConsultationFee;


            if (sessionData.PatientType == "registered")
            {
                Summary.FirstName = sessionData.RegisteredPatient.FirstName;
                Summary.LastName = sessionData.RegisteredPatient.LastName;
                Summary.Mobile = sessionData.RegisteredPatient.Mobile;
                Summary.Gender = sessionData.RegisteredPatient.Gender;
                Summary.DOB = sessionData.RegisteredPatient.DOB;
                Summary.Title = sessionData.RegisteredPatient.TitleName;
                Summary.RegistrationNo = sessionData.RegisteredPatient.RegistrationNo;
            }

            else
            {
                Summary.Title = sessionData.NewPatient.TitleName ?? "";
                Summary.FirstName = sessionData.NewPatient.FirstName ?? "";
                Summary.MiddleName = sessionData.NewPatient.MiddleName ?? "";
                Summary.LastName = sessionData.NewPatient.LastName ?? "";
                Summary.Gender = sessionData.NewPatient.Gender ?? "";
                Summary.DOB = sessionData.NewPatient.DOB ?? "";
                Summary.Mobile = sessionData.NewPatient.Mobile ?? "";
                Summary.CountryCode = sessionData.NewPatient.CountryCode ?? "+91";
                Summary.Email = sessionData.NewPatient.Email ?? "";
                Summary.BloodGroup = sessionData.NewPatient.BloodGroup ?? "";
                Summary.Address = sessionData.NewPatient.Address ?? "";
                Summary.CountryId = sessionData.NewPatient.CountryId;
                Summary.StateId = sessionData.NewPatient.StateId;
                Summary.CityId = sessionData.NewPatient.CityId;
                Summary.Pincode = sessionData.NewPatient.Pincode ?? "";

            }

            // Notes + Consent
            Summary.AppointmentNotes = sessionData.AppointmentNotes ?? "";
            Summary.ConsentTerms = sessionData.ConsentAccepted;
            Summary.ConsentMarketing = sessionData.ConsentMarketing;


            Summary.EnablePayNow = _configuration.GetValue<bool>("PaymentSettings:EnablePayNow", true);
            Summary.EnablePayOnArrival = _configuration.GetValue<bool>("PaymentSettings:EnablePayOnArrival", true);

            _logger.LogInformation(
                "ReviewAndConfirmation loaded | Patient={Name} Doctor={Doctor}",
                $"{Summary.FirstName} {Summary.LastName}",
                Summary.DoctorName);

            return Page();
        }
    }
}
