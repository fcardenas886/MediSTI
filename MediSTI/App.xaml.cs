using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using Plugin.LocalNotification.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediSTI.Models;

namespace MediSTI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Solicitamos los permisos de notificación al iniciar la app de forma segura en el hilo de UI
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Esperamos a que la Activity principal esté lista
                    await Task.Delay(2000);
                    await MediSTI.Services.NotificationService.SolicitarPermisosAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al solicitar permisos al iniciar: {ex.Message}");
                }
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}