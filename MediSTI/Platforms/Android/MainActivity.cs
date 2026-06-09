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

            // Si es Android 13 (API 33) o superior, pedimos permiso de notificaciones
            // 1. Configuración del Canal de Notificación con sonido personalizado
            string channelId = "medicamentos_channel_v5";

            // Referencia al archivo en Platforms/Android/Resources/raw/absolute.mp3
            // Nota: El nombre debe estar en minúsculas en el código
            var soundUri = Android.Net.Uri.Parse($"{Android.Content.ContentResolver.SchemeAndroidResource}://{PackageName}/raw/absolute");
            
            
            var channel = new NotificationChannel(channelId, "Alarmas MediSTI", NotificationImportance.High)
            {
                Description = "Recordatorios de medicamentos con sonido personalizado",
                LockscreenVisibility = NotificationVisibility.Public,
                Importance = NotificationImportance.High
            };

            // Configurar atributos de audio para que el sistema reconozca el tipo de sonido
            var audioAttributes = new AudioAttributes.Builder()
                .SetContentType(AudioContentType.Sonification)
                //.SetUsage(AudioUsageKind.Notification)
                .SetUsage(AudioUsageKind.Alarm) // Usar Alarm para asegurar que suene incluso en modo silencio
                .Build();

            channel.SetSound(soundUri, audioAttributes);
            channel.EnableVibration(true);
            channel.SetVibrationPattern(new long[] {0,200,400,200,400 });

            // Registrar el canal en el NotificationManager
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);

            // 2. Tu código existente para permisos en Android 13+
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 0);
                }
            }
        }
    }


}
