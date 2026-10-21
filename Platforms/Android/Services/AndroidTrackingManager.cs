#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
using Application = Android.App.Application;

namespace ZamZamApp.Platforms.Android.Services
{
    public static class AndroidTrackingManager
    {
        public static void StartService()
        {
            try
            {
                var context = Application.Context;
                var intent = new Intent(context, typeof(LocationTrackingService));
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    context.StartForegroundService(intent);
                }
                else
                {
                    context.StartService(intent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidTrackingManager] StartService error: {ex.Message}");
            }
        }

        public static void StopService()
        {
            try
            {
                var context = Application.Context;
                var intent = new Intent(context, typeof(LocationTrackingService));
                context.StopService(intent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidTrackingManager] StopService error: {ex.Message}");
            }
        }

        public static void RequestIgnoreBatteryOptimizations()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var context = Application.Context;
                    var powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
                    string packageName = context.PackageName ?? string.Empty;

                    if (powerManager != null && !string.IsNullOrEmpty(packageName) && !powerManager.IsIgnoringBatteryOptimizations(packageName))
                    {
                        var intent = new Intent();
                        intent.SetAction(Settings.ActionRequestIgnoreBatteryOptimizations);
                        intent.SetData(global::Android.Net.Uri.Parse("package:" + packageName));
                        intent.AddFlags(ActivityFlags.NewTask);
                        context.StartActivity(intent);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidTrackingManager] Battery optimization request error: {ex.Message}");
            }
        }
    }
}
#endif
