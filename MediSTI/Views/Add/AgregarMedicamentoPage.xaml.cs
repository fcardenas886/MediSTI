using MediSTI.Models;
using MediSTI.Services;
using MediSTI.ViewModels;
using MediSTI.Views;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediSTI.Views.Add;

public partial class AgregarMedicamentoPage : ContentPage
{
    private readonly int? _pacienteId;
    private readonly MedicamentosViewModel _viewModel;

    // Listas temporales para almacenar datos antes del guardado final
    private List<Horario> _horariosTemporales = new List<Horario>();
    private List<string> _diasSeleccionados = new List<string> { "Lu", "Ma", "Mi", "Ju", "Vi" };

    // Bandera de seguridad para evitar crashes durante la inicialización
    private bool _isLoaded = false;


    // Agregamos una variable para saber si estamos editando
    private Medicamento _medicamentoAEditar;




    public AgregarMedicamentoPage(int? pacienteId, MedicamentosViewModel viewModel, Medicamento medicamento = null)
    {
        InitializeComponent();

        _pacienteId = pacienteId;
        _viewModel = viewModel;

        _medicamentoAEditar = medicamento; // Guardamos el medicamento si viene uno

        // Configuración inicial de fechas
        dpFin.Date = DateTime.Now.AddDays(7);

        // Permitimos que los eventos de UI ejecuten lógica de aquí en adelante
        _isLoaded = true;

        // Si estamos editando, llenamos los campos con la info existente
        if (_medicamentoAEditar != null)
        {
            LlenarCamposParaEdicion();
        }


        if (tpPrimeraToma != null)
        {
            ActualizarHorariosAutomaticos();
        }

    }


    private void LlenarCamposParaEdicion()
    {
        txtNombre.Text = _medicamentoAEditar.Nombre;
        txtDosis.Text = _medicamentoAEditar.Dosis;
        txtFrecuencia.Text = _medicamentoAEditar.Frecuencia;
        dpInicio.Date = _medicamentoAEditar.FechaInicio;
        dpFin.Date = _medicamentoAEditar.FechaFin;

        // Aquí podrías marcar los botones de los días según _medicamentoAEditar.DiasSemana
        // Y cargar sus horarios en _horariosTemporales
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        // Bloqueamos el botón para evitar múltiples clics accidentales
        var btn = (Button)sender;
        btn.IsEnabled = false;

        try
        {
            // Ejecutamos todo el procesamiento pesado en un hilo secundario
            await Task.Run(async () =>
            {
                if (_medicamentoAEditar != null)
                {
                    // --- MODO ACTUALIZAR ---
                    _medicamentoAEditar.Nombre = txtNombre.Text;
                    _medicamentoAEditar.Dosis = txtDosis.Text;
                    _medicamentoAEditar.Frecuencia = txtFrecuencia.Text;
                    _medicamentoAEditar.DiasSemana = obtenerDiasSeleccionados();
                    _medicamentoAEditar.FechaInicio = dpInicio?.Date ?? DateTime.Now;
                    _medicamentoAEditar.FechaFin = dpFin?.Date ?? DateTime.Now;

                    // Actualiza en BD y re-programa notificaciones
                    await _viewModel.ActualizarMedicamentoAsync(_medicamentoAEditar, _horariosTemporales);
                }
                else
                {
                    // --- MODO NUEVO ---
                    if (string.IsNullOrWhiteSpace(txtNombre.Text))
                    {
                        // Volvemos al hilo principal solo para mostrar la alerta
                        MainThread.BeginInvokeOnMainThread(async () => {
                            await DisplayAlert("Error", "El nombre es obligatorio", "OK");
                        });
                        return;
                    }

                    var nuevoMed = new Medicamento
                    {
                        PacienteId = _pacienteId,
                        Nombre = txtNombre.Text,
                        Dosis = txtDosis.Text,
                        DiasSemana = obtenerDiasSeleccionados(),
                        Frecuencia = txtFrecuencia.Text,
                        FechaInicio = dpInicio?.Date ?? DateTime.Now,
                        FechaFin = dpFin?.Date ?? DateTime.Now
                    };

                    // 1. Guardar Medicamento y obtener su ID
                    int medId = await _viewModel.GuardarMedicamentoAsync(nuevoMed);
                    nuevoMed.Id = medId; // Aseguramos el ID para las notificaciones

                    // 2. Guardar Horarios vinculados
                    foreach (var h in _horariosTemporales)
                    {
                        h.MedicamentoId = medId;
                        h.Activo = true;
                        //await _viewModel.GuardarHorarioAsync(h);
                        h.Id = await _viewModel.GuardarHorarioAsync(h);
                    }

                    // 3. Programar Notificaciones usando el nuevo servicio con BD
                    // Pasamos _viewModel.Database para que pueda guardar los IDs por lote
                    await NotificationService.ProgramarNotificacionesMedicamentoAsync(nuevoMed, _horariosTemporales, _viewModel.Database);
                }
            });

            // Una vez terminada la carga pesada, cerramos la pantalla de forma fluida
            MainThread.BeginInvokeOnMainThread(async () =>
            {
            txtNombre?.Unfocus();
            await Task.Delay(100); // Pequeño margen para la UI
            await Navigation.PopModalAsync();
        });
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error STI", ex.Message, "OK");
    btn.IsEnabled = true;
    }
}
    
    private string obtenerDiasSeleccionados()
    {
        if (_diasSeleccionados == null || !_diasSeleccionados.Any())
            return "No seleccionados";

        // Retorna algo como "Lu, Ma, Mi"
        return string.Join(", ", _diasSeleccionados);
    }

    private void OnFrecuenciaChanged(object sender, FocusEventArgs e)
    {
        ActualizarHorariosAutomaticos();
    }

    private void OnPrimeraTomaChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Solo disparamos la lógica si cambió la propiedad 'Time'
        if (e.PropertyName == TimePicker.TimeProperty.PropertyName)
        {
            ActualizarHorariosAutomaticos();
        }
    }

    private void ActualizarHorariosAutomaticos()
    {
        // FIREWALL: Si la página no ha cargado o faltan controles, abortamos para evitar el crash
        if (!_isLoaded || flexHoras == null || tpPrimeraToma == null || txtFrecuencia == null)
            return;

        flexHoras.Children.Clear();
        _horariosTemporales.Clear();

        int intervalo = ExtraerHoras(txtFrecuencia.Text);
        if (intervalo <= 0) return;

        TimeSpan horaInicio = tpPrimeraToma?.Time ?? TimeSpan.Zero;

        // Calculamos las tomas en un ciclo de 24 horas basándonos en la frecuencia
        for (int i = 0; i < 24; i += intervalo)
        {
            TimeSpan proximaHora = horaInicio.Add(TimeSpan.FromHours(i));

            // Si sobrepasa las 24 horas, aplicamos el residuo para mantenerlo en formato reloj
            if (proximaHora.TotalHours >= 24)
                proximaHora = TimeSpan.FromHours(proximaHora.TotalHours % 24);

            var h = new Horario { Hora = proximaHora, Activo = true };
            _horariosTemporales.Add(h);

            AgregarChipVisual(proximaHora);
        }
    }

    private void AgregarChipVisual(TimeSpan hora)
    {
        // Formato visual 24h (HH:mm)
        string horaFormateada = DateTime.Today.Add(hora).ToString("HH:mm");

        var chip = new Frame
        {
            BackgroundColor = Color.FromArgb("#E8F2FB"),
            BorderColor = Color.FromArgb("#D1E4F7"),
            CornerRadius = 15,
            Padding = new Thickness(10, 5),
            Margin = new Thickness(0, 0, 5, 5),
            Content = new Label
            {
                Text = horaFormateada,
                TextColor = Color.FromArgb("#1967B3"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12
            }
        };

        flexHoras.Children.Add(chip);
    }

    private void OnDayButtonClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        string dia = button.Text;

        if (_diasSeleccionados.Contains(dia))
        {
            _diasSeleccionados.Remove(dia);
            button.BackgroundColor = Colors.White;
            button.TextColor = Colors.Gray;
        }
        else
        {
            _diasSeleccionados.Add(dia);
            button.BackgroundColor = Color.FromArgb("#1967B3");
            button.TextColor = Colors.White;
        }
    }

    private int ExtraerHoras(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return 0;

        // Expresión regular para capturar solo los números del texto
        var match = Regex.Match(texto, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    // Cambiamos 'FocusEventArgs' por 'EventArgs' para que sirva tanto para Unfocused como para TextChanged
    private void OnFrecuenciaChanged(object sender, EventArgs e)
    {
        ActualizarHorariosAutomaticos();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}