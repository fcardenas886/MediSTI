using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
#endif

namespace MediSTI.Views
{
    public partial class DiagnosticoPage : ContentPage
    {
        public DiagnosticoPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await EvaluarPermisosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al evaluar permisos: {ex.Message}");
            }
        }

        private async Task EvaluarPermisosAsync()
        {
            // 1. Validar Permiso de Notificaciones
            bool notifPermitidas = await Plugin.LocalNotification.LocalNotificationCenter.Current.AreNotificationsEnabled();
            ActualizarUIItem(
                lblNotifIcon, lblNotifStatus, btnNotifAction,
                notifPermitidas,
                "🟢 Permitido", "🔴 Desactivado",
                "Las notificaciones están activas.", "Las notificaciones están desactivadas en los ajustes.");

            // 2. Validar Permiso de Alarma Exacta
            bool exactasPermitidas = CheckExactAlarmStatus();
            ActualizarUIItem(
                lblExactIcon, lblExactStatus, btnExactAction,
                exactasPermitidas,
                "🟢 Activo", "🔴 Inactivo (Recomendado activar)",
                "Permiso de alarmas exactas concedido.", "Se requiere permiso para sonar exactamente a la hora.");

            // 3. Validar Optimización de Batería
            bool sinOptimizacion = CheckBatteryBypassStatus();
            ActualizarUIItem(
                lblBatteryIcon, lblBatteryStatus, btnBatteryAction,
                sinOptimizacion,
                "🟢 Excluido (Sin restricciones)", "🟡 Optimizado (Puede demorar la alarma)",
                "La batería no tiene restricciones para MediSTI.", "El sistema podría retrasar alertas para ahorrar batería.");
        }

        private void ActualizarUIItem(
            Label iconLabel, Label statusLabel, Button actionBtn,
            bool estadoValido,
            string textoValido, string textoInvalido,
            string descValida, string descInvalida)
        {
            if (estadoValido)
            {
                iconLabel.Text = "✅";
                iconLabel.TextColor = Colors.Green;
                statusLabel.Text = textoValido;
                statusLabel.TextColor = Colors.Green;
                actionBtn.Text = "Habilitado";
                actionBtn.IsEnabled = false;
                actionBtn.BackgroundColor = Colors.LightGray;
                actionBtn.TextColor = Colors.DarkGray;
            }
            else
            {
                iconLabel.Text = "⚠️";
                iconLabel.TextColor = Colors.Orange;
                statusLabel.Text = textoInvalido;
                statusLabel.TextColor = Colors.DarkOrange;
                actionBtn.Text = "Configurar";
                actionBtn.IsEnabled = true;
                actionBtn.BackgroundColor = Color.FromArgb("#1967B3");
                actionBtn.TextColor = Colors.White;
            }
        }

        private bool CheckExactAlarmStatus()
        {
#if ANDROID
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var alarmManager = Android.App.Application.Context.GetSystemService(Context.AlarmService) as Android.App.AlarmManager;
                return alarmManager == null || alarmManager.CanScheduleExactAlarms();
            }
#endif
            return true;
        }

        private bool CheckBatteryBypassStatus()
        {
#if ANDROID
            var powerManager = Android.App.Application.Context.GetSystemService(Context.PowerService) as PowerManager;
            return powerManager == null || powerManager.IsIgnoringBatteryOptimizations(AppInfo.Current.PackageName);
#endif
            return true;
        }

        private async void OnRequestNotifClicked(object? sender, EventArgs e)
        {
            try
            {
                await Plugin.LocalNotification.LocalNotificationCenter.Current.RequestNotificationPermission();
                await Task.Delay(1000);
                await EvaluarPermisosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al solicitar permisos notif: {ex.Message}");
            }
        }

        private async void OnRequestExactClicked(object? sender, EventArgs e)
        {
            try
            {
#if ANDROID
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    try
                    {
                        var intent = new Intent(Settings.ActionRequestScheduleExactAlarm);
                        intent.SetData(Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName));
                        Platform.CurrentActivity?.StartActivity(intent);
                    }
                    catch
                    {
                        AppInfo.Current.ShowSettingsUI();
                    }
                }
#else
                await DisplayAlert("Información", "Esta configuración se gestiona automáticamente en tu plataforma.", "Entendido");
#endif
                await Task.Delay(1000);
                await EvaluarPermisosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al solicitar alarmas exactas: {ex.Message}");
            }
        }

        private async void OnRequestBatteryClicked(object? sender, EventArgs e)
        {
            try
            {
#if ANDROID
                try
                {
                    var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName));
                    Platform.CurrentActivity?.StartActivity(intent);
                }
                catch
                {
                    try
                    {
                        var intent = new Intent(Settings.ActionIgnoreBatteryOptimizationSettings);
                        Platform.CurrentActivity?.StartActivity(intent);
                    }
                    catch
                    {
                        AppInfo.Current.ShowSettingsUI();
                    }
                }
#else
                await DisplayAlert("Información", "La gestión de batería se maneja por defecto en tu plataforma.", "Entendido");
#endif
                await Task.Delay(1000);
                await EvaluarPermisosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al solicitar bypass batería: {ex.Message}");
            }
        }

        private void OnOpenAppSettingsClicked(object? sender, EventArgs e)
        {
            try
            {
                AppInfo.Current.ShowSettingsUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir ajustes: {ex.Message}");
            }
        }

        private async void OnReevaluarClicked(object? sender, EventArgs e)
        {
            try
            {
                await EvaluarPermisosAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al reevaluar: {ex.Message}");
            }
        }

        private async void OnCerrarClicked(object? sender, EventArgs e)
        {
            try
            {
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar modal: {ex.Message}");
            }
        }
    }
}
