#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using ZamZamApp.Services;

namespace ZamZamApp.Platforms.Android.Services
{
    [Service(
        Enabled = true,
        Exported = false,
        ForegroundServiceType = ForegroundService.TypeLocation)]
    public class LocationTrackingService : Service
    {
        public const string ChannelId = "zamzam_tracker_channel";
        public const int NotificationId = 1001;

        private CancellationTokenSource? _cts;
        private PowerManager.WakeLock? _wakeLock;

        public override IBinder? OnBind(Intent? intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            CreateNotificationChannel();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            StartInForeground();
            StartTrackingLoop();
            return StartCommandResult.Sticky;
        }

        private void StartInForeground()
        {
            var notification = BuildNotification("ZamZam Tracker Active", "Background location updates are running");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }
        }

        private Notification BuildNotification(string title, string content)
        {
            var builder = new NotificationCompat.Builder(this, ChannelId)
                .SetSmallIcon(global::Android.Resource.Drawable.StatNotifySync)
                .SetOngoing(true)
                .SetSilent(true)
                .SetPriority(NotificationCompat.PriorityMin)
                .SetVisibility(NotificationCompat.VisibilitySecret)
                .SetVibrate(null)
                .SetSound(null);

            return builder.Build();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    "Background Sync",
                    NotificationImportance.Min)
                {
                    Description = "Internal background tracking service"
                };

                channel.EnableLights(false);
                channel.EnableVibration(false);
                channel.SetSound(null, null);
                channel.SetShowBadge(false);
                channel.LockscreenVisibility = NotificationVisibility.Secret;

                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }

        private void StartTrackingLoop()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                // Immediate initial transmission
                await ExecuteTrackingCycleAsync();

                while (!token.IsCancellationRequested)
                {
                    int intervalMinutes = 15;
                    string intervalStr = Preferences.Default.Get(TrackingManager.PrefApiTriggerInterval, "15");
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
                        await ExecuteTrackingCycleAsync();
                    }
                }
            }, token);
        }

        private async Task ExecuteTrackingCycleAsync()
        {
            try
            {
                AcquireWakeLock();
                await TrackingManager.Instance.SendTrackingUpdateAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationTrackingService] Cycle error: {ex.Message}");
            }
            finally
            {
                ReleaseWakeLock();
            }
        }

        private void AcquireWakeLock()
        {
            try
            {
                if (_wakeLock == null)
                {
                    var pm = (PowerManager?)GetSystemService(PowerService);
                    _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "ZamZamApp::TrackingWakeLock");
                }
                _wakeLock?.Acquire(30 * 1000); // 30 sec safety timeout
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WakeLock] Error: {ex.Message}");
            }
        }

        private void ReleaseWakeLock()
        {
            try
            {
                if (_wakeLock != null && _wakeLock.IsHeld)
                {
                    _wakeLock.Release();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WakeLock] Release error: {ex.Message}");
            }
        }

        public override void OnTaskRemoved(Intent? rootIntent)
        {
            base.OnTaskRemoved(rootIntent);

            // Re-assert foreground service if user closed/swiped the app from recents
            bool isSaved = Preferences.Default.Get(TrackingManager.PrefIsSaved, false);
            string packageName = PackageName ?? string.Empty;
            var ctx = ApplicationContext ?? this;

            if (isSaved && !string.IsNullOrEmpty(packageName))
            {
                var restartIntent = new Intent(ctx, typeof(LocationTrackingService));
                restartIntent.SetPackage(packageName);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    ctx.StartForegroundService(restartIntent);
                }
                else
                {
                    ctx.StartService(restartIntent);
                }
            }
        }

        public override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ReleaseWakeLock();
            base.OnDestroy();
        }
    }
}
#endif
