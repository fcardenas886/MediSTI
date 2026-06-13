using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;

namespace MediSTI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                // 1. Configuración de los Canales de Notificación (Solo Android 8.0+ / API 26+)
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    string gentleChannelId = "medicamentos_channel_gentle_v3";
                    string alarmChannelId = "medicamentos_channel_alarm_v3";

                    var notificationManager = GetSystemService(NotificationService) as NotificationManager;

                    if (notificationManager != null)
                    {
                        // A. CANAL SUAVE (Notificación estándar que respeta silencio/DND)
                        var gentleChannel = new NotificationChannel(gentleChannelId, "Recordatorios MediSTI (Suaves)", NotificationImportance.High)
                        {
                            Description = "Recordatorios de medicamentos que respetan el volumen de notificaciones y modo silencio del sistema",
                            LockscreenVisibility = NotificationVisibility.Public
                        };
                        var gentleAudioAttrs = new AudioAttributes.Builder()
                            .SetContentType(AudioContentType.Sonification)
                            .SetUsage(AudioUsageKind.Notification)
                            .Build();
                        try
                        {
                            var gentleSoundUri = Android.Net.Uri.Parse($"{Android.Content.ContentResolver.SchemeAndroidResource}://{PackageName}/raw/reloj");
                            gentleChannel.SetSound(gentleSoundUri, gentleAudioAttrs);
                        }
                        catch (Exception soundEx)
                        {
                            Android.Util.Log.Error("MediSTI", $"Error al configurar sonido de canal suave: {soundEx.Message}");
                        }
                        gentleChannel.EnableVibration(true);
                        notificationManager.CreateNotificationChannel(gentleChannel);

                        // B. CANAL CRÍTICO (Alarma ruidosa que actúa como despertador)
                        var alarmChannel = new NotificationChannel(alarmChannelId, "Alarmas Críticas MediSTI", NotificationImportance.High)
                        {
                            Description = "Recordatorios críticos que omiten el modo silencio del sistema y suenan como alarma",
                            LockscreenVisibility = NotificationVisibility.Public
                        };
                        var alarmAudioAttrs = new AudioAttributes.Builder()
                            .SetContentType(AudioContentType.Sonification)
                            .SetUsage(AudioUsageKind.Alarm) // Usar Alarm para asegurar volumen de alarma
                            .Build();

                        try
                        {
                            var soundUri = Android.Net.Uri.Parse($"{Android.Content.ContentResolver.SchemeAndroidResource}://{PackageName}/raw/absolute");
                            alarmChannel.SetSound(soundUri, alarmAudioAttrs);
                        }
                        catch (Exception soundEx)
                        {
                            Android.Util.Log.Error("MediSTI", $"Error al configurar sonido de alarma: {soundEx.Message}");
                        }

                        alarmChannel.EnableVibration(true);
                        alarmChannel.SetVibrationPattern(new long[] { 0, 200, 400, 200, 400 });
                        notificationManager.CreateNotificationChannel(alarmChannel);
                    }
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("MediSTI", $"Error configurando canales de notificación: {ex}");
            }

            try
            {
                // 2. Permisos en Android 13+
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
                {
                    if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
                    {
                        RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("MediSTI", $"Error solicitando permisos en MainActivity: {ex}");
            }

            try
            {
                // 3. Procesar el intent de la notificación que pudo haber abierto la app
                Plugin.LocalNotification.LocalNotificationCenter.NotifyNotificationTapped(Intent);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("MediSTI", $"Error al notificar intent de notificación en OnCreate: {ex}");
            }
        }

        protected override void OnNewIntent(Android.Content.Intent intent)
        {
            base.OnNewIntent(intent);
            try
            {
                Plugin.LocalNotification.LocalNotificationCenter.NotifyNotificationTapped(intent);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("MediSTI", $"Error al notificar intent de notificación en OnNewIntent: {ex}");
            }
        }
    }


}
