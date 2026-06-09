using MediSTI.ViewModels;
using MediSTI.Models;
namespace MediSTI.Views;

public partial class AgregarPacientesPage : ContentPage
{
	private readonly PacientesViewModel _viewModel;
    public AgregarPacientesPage(PacientesViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
	}
    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            await DisplayAlert("Error", "El nombre es obligatorio", "OK");
            return;
        }

        var nuevo = new Paciente
        {
            Nombre = txtNombre.Text,
            Telefono = txtTelefono.Text,
            FechaNacimiento = dtpFecha.Date
        };

        // Guardamos usando tu método simple
        await _viewModel.GuardarPacienteAsync(nuevo);

        // Cerramos la página modal para volver a la lista
        await Navigation.PopModalAsync();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        // Solo cerramos sin guardar nada
        await Navigation.PopModalAsync();
    }

}