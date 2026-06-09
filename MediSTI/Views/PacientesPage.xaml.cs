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
        await _viewModel.CargarPacientesAsync();
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
}
