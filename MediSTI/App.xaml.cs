using Microsoft.Extensions.DependencyInjection;

namespace MediSTI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Solicitamos los permisos de notificación al iniciar la app
            // Task.Run evita que la interfaz se congele mientras el sistema responde
            Task.Run(async () =>
            {
                await MediSTI.Services.NotificationService.SolicitarPermisosAsync();
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}