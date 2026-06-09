using MediSTI.Models;
using MediSTI.ViewModels;
using MediSTI.Views.Add; // Importante para reconocer AgregarMedicamentoPage

namespace MediSTI.Views;

public partial class MedicamentosPage : ContentPage
{
    private readonly Paciente _pacienteActual;
    private readonly MedicamentosViewModel _vm;
    public MedicamentosPage(Paciente paciente,MedicamentosViewModel vm)
	{
		InitializeComponent();
        _pacienteActual = paciente;
        _vm = vm;
        // Asignamos el ViewModel como fuente de datos para el CollectionView
        BindingContext = _vm;

        // AÑADE ESTA LÍNEA para que el nombre se vea en el diseño
        lblNombrePaciente.Text = _pacienteActual.Nombre.ToUpper();

        Title = $"Medicinas de {_pacienteActual.Nombre}";
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Usamos un pequeño delay para dejar que la animación de cierre termine
        await Task.Delay(100);

        if (_vm != null && _pacienteActual != null)
        {
            // Ejecutamos la carga de forma segura
            MainThread.BeginInvokeOnMainThread(async () => {
                await _vm.CargarMedicamentosAsync(_pacienteActual.Id);
            });
        }
    }

    private async void OnAgregarMedicinaClicked(object sender, EventArgs e)
    {
        // Usamos PushModalAsync para que la ventana aparezca desde abajo
        // Pasamos el ID del paciente actual y la instancia del ViewModel (_vm)
        await Navigation.PushModalAsync(new AgregarMedicamentoPage(_pacienteActual.Id, _vm));
    }

    private async void OnOpcionesMedicamentoTapped(object sender, TappedEventArgs e)
    {
        var medicamento = e.Parameter as Medicamento;
        if (medicamento == null) return;

        // CAMBIO: Solo bloqueamos si ya pasó el día de inicio
        // Si hoy es el día de inicio, todavía permitimos editar
        bool yaPasoElInicio = DateTime.Now.Date > medicamento.FechaInicio.Date;

        string accion;

        if (yaPasoElInicio)
        {
            // El tratamiento ya está en curso desde días anteriores
            accion = await DisplayActionSheet($"Opciones: {medicamento.Nombre}", "Cancelar", null, "Eliminar");
        }
        else
        {
            // Es hoy o una fecha futura, permitimos Editar
            accion = await DisplayActionSheet($"Opciones: {medicamento.Nombre}", "Cancelar", null, "Editar", "Eliminar");
        }

        switch (accion)
        {
            case "Editar":
                // Importante: Verifica que el orden de los parámetros coincida con tu constructor
                await Navigation.PushModalAsync(new AgregarMedicamentoPage(_pacienteActual.Id, _vm, medicamento));
                break;

            case "Eliminar":
                bool confirmar = await DisplayAlert("Eliminar", $"¿Estás seguro de eliminar {medicamento.Nombre}?", "Sí", "No");
                if (confirmar)
                {
                    await _vm.EliminarMedicamentoAsync(medicamento);
                }
                break;
        }
    }
}