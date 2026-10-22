using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ZamZamApp.Models;

namespace ZamZamApp.Services
{
    public class TrackingManager
    {
        private static readonly Lazy<TrackingManager> _instance = new(() => new TrackingManager());
        public static TrackingManager Instance => _instance.Value;

        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _timerCts;

        public const string PrefEmployeeId = "ZamZam_EmployeeId";
        public const string PrefDateOfBirth = "ZamZam_DateOfBirth";
        public const string PrefMobile = "ZamZam_Mobile";
        public const string PrefApiTriggerInterval = "ZamZam_ApiTriggerInterval";
        public const string PrefApiUrl = "ZamZam_ApiUrl";
        public const string PrefIsSaved = "ZamZam_IsSettingsSaved";

        public event Action<string, bool>? TrackingLogEvent;

        private TrackingManager()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public void StartTracking()
        {
#if ANDROID
            Platforms.Android.Services.AndroidTrackingManager.StartService();
#endif
            StartCrossPlatformTimer();
        }

        public void StopTracking()
        {
            StopCrossPlatformTimer();
#if ANDROID
            Platforms.Android.Services.AndroidTrackingManager.StopService();
#endif
        }

        public void StartCrossPlatformTimer()
        {
            StopCrossPlatformTimer();

            _timerCts = new CancellationTokenSource();
            var token = _timerCts.Token;

            Task.Run(async () =>
            {
                // Initial immediate run
                await SendTrackingUpdateAsync();

                while (!token.IsCancellationRequested)
                {
                    int intervalMinutes = 15;
                    string intervalStr = Preferences.Default.Get(PrefApiTriggerInterval, "15");
                    if (int.TryParse(intervalStr, out int parsed) && parsed > 0)
                    {
                        intervalMinutes = parsed;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    if (!token.IsCancellationRequested)
                    {
                        await SendTrackingUpdateAsync();
                    }
                }
            }, token);
        }

        public void StopCrossPlatformTimer()
        {
            if (_timerCts != null)
            {
                try
                {
                    _timerCts.Cancel();
                    _timerCts.Dispose();
                }
                catch
                {
                }
                _timerCts = null;
            }
        }

        public async Task<bool> SendTrackingUpdateAsync()
        {
            string employeeIdStr = Preferences.Default.Get(PrefEmployeeId, string.Empty);
            string apiUrl = Preferences.Default.Get(PrefApiUrl, string.Empty);

            if (string.IsNullOrWhiteSpace(employeeIdStr) || string.IsNullOrWhiteSpace(apiUrl))
            {
                Log("Cannot send update: Employee ID or API URL is not configured.", isSuccess: false);
                return false;
            }

            long.TryParse(employeeIdStr, out long employeeId);

            // Fetch Current Location
            Location? location = null;
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                location = await Geolocation.Default.GetLocationAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tracking] Failed to get current location: {ex.Message}");
            }

            if (location == null)
            {
                try
                {
                    location = await Geolocation.Default.GetLastKnownLocationAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tracking] Failed to get last known location: {ex.Message}");
                }
            }

            // Fetch Battery Level using native BatteryManager driver (supported across all Android versions without security exceptions)
            int batteryPercentage = GetBatteryPercentage();

            string dateOfBirth = Preferences.Default.Get(PrefDateOfBirth, string.Empty);
            string mobile = Preferences.Default.Get(PrefMobile, string.Empty);

            // Build Payload
            var payload = new TrackingPayload
            {
                Id = 0,
                EmployeeId = employeeId,
                DateOfBirth = dateOfBirth,
                Mobile = mobile,
                Latitude = location?.Latitude ?? 0,
                Longitude = location?.Longitude ?? 0,
                Accuracy = location?.Accuracy ?? 0,
                Speed = location?.Speed ?? 0,
                BatteryPercentage = batteryPercentage,
                RecordedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            string json = JsonSerializer.Serialize(payload);
            Log($"Sending payload to {apiUrl}:\n{json}", isSuccess: true);

            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Log($"API triggered successfully (Status: {(int)response.StatusCode} {response.ReasonPhrase})", isSuccess: true);
                    return true;
                }
                else
                {
                    Log($"API trigger failed: {(int)response.StatusCode} {response.ReasonPhrase}", isSuccess: false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"Error sending API request: {ex.Message}", isSuccess: false);
                return false;
            }
        }

        public async Task RequestPermissionsAndBatteryOptimizationAsync()
        {
            try
            {
                // 1. Location permission
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

#if ANDROID
                // Background location permission (Android 10+ / API 29+)
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                {
                    var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                    if (bgStatus != PermissionStatus.Granted)
                    {
                        await Permissions.RequestAsync<Permissions.LocationAlways>();
                    }
                }

                // 4. Request Battery Optimization Exemption
                Platforms.Android.Services.AndroidTrackingManager.RequestIgnoreBatteryOptimizations();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrackingManager] Permission request error: {ex.Message}");
            }
        }

        private void Log(string message, bool isSuccess)
        {
            Debug.WriteLine($"[TrackingManager] {message}");
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TrackingLogEvent?.Invoke(message, isSuccess);
                });
            }
            catch
            {
            }
        }

        public static int GetBatteryPercentage()
        {
#if ANDROID
            try
            {
                var context = global::Android.App.Application.Context;
                var bm = (global::Android.OS.BatteryManager?)context.GetSystemService(global::Android.Content.Context.BatteryService);
                if (bm != null)
                {
                    int capacity = bm.GetIntProperty((int)global::Android.OS.BatteryProperty.Capacity);
                    if (capacity >= 0 && capacity <= 100)
                    {
                        return capacity;
                    }
                }

                // Fallback: Read from sticky battery intent (safe with flags)
                var filter = new global::Android.Content.IntentFilter(global::Android.Content.Intent.ActionBatteryChanged);
                global::Android.Content.Intent? intent = null;
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
                {
                    intent = context.RegisterReceiver(null, filter, global::Android.Content.ReceiverFlags.Exported);
                }
                else
                {
                    intent = context.RegisterReceiver(null, filter);
                }

                if (intent != null)
                {
                    int level = intent.GetIntExtra(global::Android.OS.BatteryManager.ExtraLevel, -1);
                    int scale = intent.GetIntExtra(global::Android.OS.BatteryManager.ExtraScale, -1);
                    if (level >= 0 && scale > 0)
                    {
                        return (int)Math.Round((level / (float)scale) * 100);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Battery] Android native error: {ex.Message}");
            }
#endif

            // Cross-platform MAUI Essentials fallback (Windows / Desktop / iOS)
            try
            {
                double chargeLevel = Battery.Default.ChargeLevel;
                if (chargeLevel >= 0)
                {
                    return (int)Math.Round(chargeLevel * 100);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Battery] MAUI default error: {ex.Message}");
            }

            return 100;
        }
    }
}
