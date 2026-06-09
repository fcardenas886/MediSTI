using MediSTI.Data;
using MediSTI.ViewModels;
using Plugin.LocalNotification;

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

            builder.Services.AddSingleton<ViewModels.MainViewModel>();
            builder.Services.AddTransient<PacientesViewModel>();
            builder.Services.AddTransient<MedicamentosViewModel>();
            builder.Services.AddTransient<HorariosViewModel>();
            builder.Services.AddTransient<RegistrosViewModel>();

            return builder.Build();
        }
    }
}
