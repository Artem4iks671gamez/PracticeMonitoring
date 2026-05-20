using Android.App;
using Android.Content;
using Android.OS;
using Firebase.Messaging;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile;

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public sealed class AppFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        Preferences.Default.Set(PushRegistrationService.DeviceTokenPreferenceKey, token);
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var title = message.GetNotification()?.Title;
        var body = message.GetNotification()?.Body;

        if (string.IsNullOrWhiteSpace(title) && message.Data.TryGetValue("title", out var dataTitle))
            title = dataTitle;
        if (string.IsNullOrWhiteSpace(body) && message.Data.TryGetValue("body", out var dataBody))
            body = dataBody;

        AndroidNotificationHelper.Show(
            string.IsNullOrWhiteSpace(title) ? "Practice Monitoring" : title,
            string.IsNullOrWhiteSpace(body) ? "Новое уведомление" : body);
    }
}

internal static class AndroidNotificationHelper
{
    private const string ChannelId = "practice_monitoring_updates";
    private const string ChannelName = "Сообщения и уведомления";

    public static void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var context = Platform.AppContext;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null)
            return;

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
        {
            Description = "Новые сообщения и уведомления Practice Monitoring"
        };
        manager.CreateNotificationChannel(channel);
    }

    public static void Show(string title, string body)
    {
        var context = Platform.AppContext;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null)
            return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
            {
                Description = "Новые сообщения и уведомления Practice Monitoring"
            };
            manager.CreateNotificationChannel(channel);
        }

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty);
        var pendingIntent = PendingIntent.GetActivity(
            context,
            0,
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(context, ChannelId)
            : new Notification.Builder(context);

        var notification = builder
            .SetContentTitle(title)
            .SetContentText(body)
            .SetStyle(new Notification.BigTextStyle().BigText(body))
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .SetAutoCancel(true)
            .Build();

        manager.Notify(Random.Shared.Next(1000, int.MaxValue), notification);
    }
}
