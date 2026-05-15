namespace Book_An_Appointment1.API.Clients
{
    public class ApiUrlBuilder
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<ApiUrlBuilder> logger;

        public  ApiUrlBuilder(IConfiguration configuration, ILogger<ApiUrlBuilder> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public string BuildUrl(string baseUrl, string sectionKey, 
            Dictionary<string, string> ?dynamicParams = null)
        {
            try
            {
                var allParams = new Dictionary<string, string>();

                // static Parameter from Config File
                var section = configuration.GetSection($"ApiParam:{sectionKey}");

                if (!section.Exists())
                {
                    logger.LogWarning("API section '{SectionKey}' not found in configuration.", sectionKey);
                    return baseUrl;
                }
                var queryParams = section.GetChildren()
                    .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value ?? "")}");

                // Dynamic Parameters passed from method call
                var dynamicQueryParams = dynamicParams?
                    .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value ?? "")}")
                    ?? Enumerable.Empty<string>();

                var queryString = string.Join("&", queryParams.Concat(dynamicQueryParams));
               
                return $"{baseUrl}?{queryString}";

            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error building API URL for section '{SectionKey}'", sectionKey);
                
                return baseUrl;
            }
        }
    }
}
