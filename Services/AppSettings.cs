namespace ZamZamApp.Services
{
    public static class AppSettings
    {
        public const string DefaultBaseUrl = "https://api.zztbl.com";
        private const string PrefBaseUrl = "ZamZam_BaseUrl";

        public static string BaseUrl
        {
            get
            {
                string stored = Preferences.Default.Get(PrefBaseUrl, DefaultBaseUrl);
                return string.IsNullOrWhiteSpace(stored) ? DefaultBaseUrl : stored.TrimEnd('/');
            }
            set
            {
                string sanitized = string.IsNullOrWhiteSpace(value) ? DefaultBaseUrl : value.Trim().TrimEnd('/');
                Preferences.Default.Set(PrefBaseUrl, sanitized);
            }
        }

        public static string LoginEndpoint => $"{BaseUrl}/api/Auth/Login";
        public static string EmployeeTaskGetAllEndpoint => $"{BaseUrl}/api/EmployeeTask/GetAll";
    }
}
