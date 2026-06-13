using MediSTI.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
#endif

namespace MediSTI.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public HomePage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // AppInfo requiere Microsoft.Maui.ApplicationModel
            VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null)
                {
                    // Cargar datos al entrar
                    await _viewModel.CargarTomasDeHoyAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar tomas de hoy: {ex.Message}");
            }

            try
            {
                // Lógica para Xiaomi y permisos
#if ANDROID
                if (DeviceInfo.Current.Manufacturer != null && DeviceInfo.Current.Manufacturer.ToLower().Contains("xiaomi"))
                {
                    // Preferences requiere Microsoft.Maui.Storage
                    var yaVio = Preferences.Default.Get("PermisosMostrados", false);
                    if (!yaVio && MostrarBannerPermisos != null)
                    {
                        MostrarBannerPermisos.IsVisible = true;
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar fabricante: {ex.Message}");
            }

            try
            {
                await SolicitarPermisoAlarmaExacta();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al solicitar permisos de alarma exacta: {ex.Message}");
            }
        }

        private void OnConfigurarPermisos(object? sender, EventArgs e)
        {
#if ANDROID
            try
            {
                var intent = new Intent();
                intent.SetComponent(new ComponentName("com.miui.securitycenter",
                    "com.miui.permcenter.autostart.AutoStartManagementActivity"));

                Platform.CurrentActivity?.StartActivity(intent);

                Preferences.Default.Set("PermisosMostrados", true);
                if (MostrarBannerPermisos != null)
                    MostrarBannerPermisos.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir autostart: {ex.Message}");
                try
                {
                    // Si falla, abrir configuración general
                    AppInfo.Current.ShowSettingsUI();
                }
                catch { }
            }
#endif
        }

        private async Task SolicitarPermisoAlarmaExacta()
        {
#if ANDROID
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    var alarmManager = Android.App.Application.Context.GetSystemService(Context.AlarmService) as Android.App.AlarmManager;
                    if (alarmManager != null && !alarmManager.CanScheduleExactAlarms())
                    {
                        var intent = new Intent(Settings.ActionRequestScheduleExactAlarm);
                        intent.SetData(Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName));
                        Platform.CurrentActivity?.StartActivity(intent);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al solicitar exact alarms: {ex.Message}");
            }
#endif
            await Task.CompletedTask;
        }

        private async void OnDiagnosticoClicked(object? sender, EventArgs e)
        {
            try
            {
                await Navigation.PushModalAsync(new DiagnosticoPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir DiagnosticoPage: {ex.Message}");
            }
        }
    }
}