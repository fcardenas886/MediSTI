using MediSTI.Data;
using MediSTI.Models;

using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MediSTI.ViewModels
{
    public class MainViewModel : BindableObject
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<TomaDelDia> TomasDeHoy { get; set; } = new();

        // Comandos
        public ICommand AlarmaRapidaCommand { get; }
        public ICommand MarcarTomadaCommand { get; }
        public ICommand VerAgendaCompletaCommand { get; }
        public ICommand IrAPacientesCommand { get; }
        public ICommand IrAMedicinasCommand { get; }

        // Próxima toma
        private string _proximoTexto = "Sin tomas pendientes";
        public string ProximoTexto
        {
            get => _proximoTexto;
            set { _proximoTexto = value; OnPropertyChanged(); }
        }

        private string _cuentaRegresiva = "";
        public string CuentaRegresiva
        {
            get => _cuentaRegresiva;
            set { _cuentaRegresiva = value; OnPropertyChanged(); }
        }

        private IDispatcherTimer _timer;

        public MainViewModel(DatabaseService dbService)
        {
            _dbService = dbService;

            AlarmaRapidaCommand = new Command(async () => await EjecutarAlarmaRapida());
            //MarcarTomadaCommand = new Command<TomaDelDia>(async t => await MarcarComoTomadaAsync(t));
            VerAgendaCompletaCommand = new Command(async () => await Shell.Current.GoToAsync("//Historial"));
            IrAPacientesCommand = new Command(async () => await Shell.Current.GoToAsync("//Pacientes"));
            IrAMedicinasCommand = new Command(async () => await Shell.Current.GoToAsync("//Medicamentos"));

            // Timer cada segundo
            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => ActualizarCuentasRegresivas();
            _timer.Start();
        }

        public async Task CargarTomasDeHoyAsync()
        {
            var lista = await _dbService.GetTomasDeHoyAsync();

            // Ejecutamos en el hilo principal para asegurar que la UI reaccione
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TomasDeHoy.Clear();
                foreach (var toma in lista)
                {
                    TomasDeHoy.Add(toma);
                }
                // Notificamos explícitamente que la lista cambió
                OnPropertyChanged(nameof(TomasDeHoy));
            });
        }

        public void IniciarTimer()
        {
            if (!_timer.IsRunning) _timer.Start();
        }

        public void DetenerTimer()
        {
            if (_timer.IsRunning) _timer.Stop();
        }

        private void ActualizarCuentasRegresivas()
        {
            var ahora = DateTime.Now;

            // Próxima toma pendiente
            var proxima = TomasDeHoy.FirstOrDefault(t => !t.YaTomada);
            if (proxima != null)
            {
                ProximoTexto = proxima.MedicamentoNombre;
                var diferencia = DateTime.Today.Add(proxima.Hora) - ahora;
                CuentaRegresiva = diferencia.TotalSeconds > 0
                    ? string.Format("{0:hh\\:mm\\:ss}", diferencia)
                    : "¡Hora de la toma!";
            }
            else
            {
                ProximoTexto = "Sin tomas pendientes";
                CuentaRegresiva = "";
            }

            // Actualizar cada tarjeta
            foreach (var toma in TomasDeHoy)
            {
                if (toma.YaTomada)
                {
                    toma.TiempoRestante = "✅ Completado";
                    continue;
                }

                var fechaToma = DateTime.Today.Add(toma.Hora);
                var diferencia = fechaToma - ahora;

                toma.TiempoRestante = diferencia.TotalSeconds > 0
                    ? string.Format("{0:hh\\:mm\\:ss}", diferencia)
                    : "¡Hora de la toma!";
            }
        }



        private async Task EjecutarAlarmaRapida()
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
                await LocalNotificationCenter.Current.RequestNotificationPermission();

            string nombreAlarma = await Shell.Current.DisplayActionSheet(
                "¿Qué quieres recordar?",
                "Cancelar", null,
                "Tomar Agua", "Vitamina", "Pastilla puntual");

            if (string.IsNullOrEmpty(nombreAlarma) || nombreAlarma == "Cancelar") return;

            // El usuario elige el tiempo
            string tiempoElegido = await Shell.Current.DisplayActionSheet(
                "¿En cuánto tiempo?",
                "Cancelar", null,
                "5 minutos", "10 minutos", "15 minutos", "30 minutos", "1 hora");

            if (string.IsNullOrEmpty(tiempoElegido) || tiempoElegido == "Cancelar") return;

            int minutos = tiempoElegido switch
            {
                "1 Minuto" => 1,
                "5 minutos" => 5,
                "10 minutos" => 10,
                "15 minutos" => 15,
                "30 minutos" => 30,
                "45 minutos" => 30,
                "1 hora" => 60,
                "2 hora" => 120,
                _ => 10
            };


            var request = new NotificationRequest
            {
                NotificationId = 1337,
                Title = "MediSTI - Recordatorio Rápido",
                Description = $"Es hora de: {nombreAlarma}",
                BadgeNumber = 1,
                Android = new AndroidOptions
                {
                    ChannelId = "medicamentos_channel_v6",
                    Priority = AndroidPriority.Max,
                    AutoCancel = true,
                },
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddMinutes(minutos),
                    RepeatType = NotificationRepeat.No
                }
            };

            await LocalNotificationCenter.Current.Show(request);
            await Shell.Current.DisplayAlert("¡Listo!",
                $"Te avisaremos en {tiempoElegido} sobre: {nombreAlarma}", "Entendido");
        }
    }
}