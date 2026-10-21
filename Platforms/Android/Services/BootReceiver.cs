#if ANDROID
using Android.App;
using Android.Content;
using ZamZamApp.Services;

namespace ZamZamApp.Platforms.Android.Services
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON" })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null)
                return;

            if (intent.Action == Intent.ActionBootCompleted || intent.Action == "android.intent.action.QUICKBOOT_POWERON")
            {
                bool isSaved = Preferences.Default.Get(TrackingManager.PrefIsSaved, false);
                if (isSaved)
                {
                    AndroidTrackingManager.StartService();
                }
            }
        }
    }
}
#endif
