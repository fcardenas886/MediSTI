using MediSTI.Data;
using MediSTI.ViewModels;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using Microsoft.Extensions.Logging;

namespace MediSTI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif


            // Registramos el servicio como "Singleton" (una sola instancia para toda la app)
            builder.Services.AddSingleton<Data.DatabaseService>();

            // ViewModels
            builder.Services.AddSingleton<ViewModels.MainViewModel>();
            builder.Services.AddTransient<PacientesViewModel>();
            builder.Services.AddTransient<MedicamentosViewModel>();
            builder.Services.AddTransient<HorariosViewModel>();
            builder.Services.AddTransient<RegistrosViewModel>();

            // Vistas (Páginas con inyección en constructor)
            builder.Services.AddTransient<Views.HomePage>();
            builder.Services.AddTransient<Views.PacientesPage>();
            builder.Services.AddTransient<Views.HistorialPage>();

            // Registrar categoría de notificaciones para botones de acción (Tomado y Posponer)
            // Se realiza aquí en MauiProgram para que esté disponible también cuando la app se levanta en segundo plano por alarmas.
            LocalNotificationCenter.Current.RegisterCategoryList(new System.Collections.Generic.HashSet<NotificationCategory>
            {
                new NotificationCategory(NotificationCategoryType.Reminder)
                {
                    ActionList = new System.Collections.Generic.HashSet<NotificationAction>
                    {
                        new NotificationAction(101)
                        {
                            Title = "🟢 TOMADO",
                            Android = new AndroidAction
                            {
                                LaunchAppWhenTapped = true
                            }
                        },
                        new NotificationAction(102)
                        {
                            Title = "⏰ POSPONER 15M",
                            Android = new AndroidAction
                            {
                                LaunchAppWhenTapped = true
                            }
                        }
                    }
                }
            });

            // Inicializar manejador global de acciones de notificación
            MediSTI.Services.NotificationService.InicializarActionTapped();

            return builder.Build();
        }
    }
}
