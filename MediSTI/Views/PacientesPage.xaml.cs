using MediSTI.ViewModels;
using MediSTI.Models;	

namespace MediSTI.Views;

public partial class PacientesPage : ContentPage
{
	private readonly PacientesViewModel _viewModel;
    private readonly MedicamentosViewModel _medViewModel;
    public PacientesPage( PacientesViewModel viewModel, MedicamentosViewModel medViewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        _medViewModel = medViewModel;

        // Asignamos el ViewModel como fuente de datos para el CollectionView
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.CargarPacientesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar pacientes: {ex.Message}");
        }
    }

    private async void OnAgregarPacienteClicked(object sender, EventArgs e)
    {
        // Lanzamos la página como MODAL (aparece desde abajo)
        await Navigation.PushModalAsync(new AgregarPacientesPage(_viewModel));
    }

    private async void OnPacienteTapped(object sender, EventArgs e)
    {
        // 1. Identificamos qué tarjeta se tocó
        var border = (Border)sender;

        // 2. Obtenemos el objeto Paciente que tiene esa tarjeta (su BindingContext)
        var pacienteSeleccionado = (Paciente)border.BindingContext;

        if (pacienteSeleccionado != null)
        {
            // 3. Navegamos a la página de detalles/medicamentos
            // Pasamos el paciente seleccionado al constructor de la nueva página
            await Navigation.PushAsync(new MedicamentosPage(pacienteSeleccionado, _medViewModel));
        }
    }

    private async void OnEditarPacienteInvoked(object sender, EventArgs e)
    {
        var swipeItem = (SwipeItem)sender;
        var paciente = (Paciente)swipeItem.CommandParameter;

        if (paciente != null)
        {
            await Navigation.PushModalAsync(new AgregarPacientesPage(_viewModel, paciente));
        }
    }

    private async void OnEliminarPacienteInvoked(object sender, EventArgs e)
    {
        var swipeItem = (SwipeItem)sender;
        var paciente = (Paciente)swipeItem.CommandParameter;

        if (paciente != null)
        {
            bool confirm = await DisplayAlert("Confirmar", $"¿Estás seguro de que deseas eliminar a {paciente.Nombre}?", "Sí", "No");
            if (confirm)
            {
                await _viewModel.EliminarPacienteAsync(paciente);
            }
        }
    }
}
