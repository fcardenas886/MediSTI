using MediSTI.ViewModels;
using MediSTI.Models;
namespace MediSTI.Views;

public partial class AgregarPacientesPage : ContentPage
{
    private readonly PacientesViewModel _viewModel;
    private readonly Paciente _pacienteAEditar;

    public AgregarPacientesPage(PacientesViewModel viewModel, Paciente paciente = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _pacienteAEditar = paciente;

        if (_pacienteAEditar != null)
        {
            lblTitulo.Text = "👤 Editar Paciente";
            txtNombre.Text = _pacienteAEditar.Nombre;
            txtTelefono.Text = _pacienteAEditar.Telefono;
            dtpFecha.Date = _pacienteAEditar.FechaNacimiento;
            btnGuardar.Text = "Guardar Cambios";
        }
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            await DisplayAlert("Error", "El nombre es obligatorio", "OK");
            return;
        }

        if (_pacienteAEditar != null)
        {
            // MODO EDITAR: Actualizar campos del objeto existente
            _pacienteAEditar.Nombre = txtNombre.Text;
            _pacienteAEditar.Telefono = txtTelefono.Text;
            _pacienteAEditar.FechaNacimiento = dtpFecha.Date;

            await _viewModel.GuardarPacienteAsync(_pacienteAEditar);
        }
        else
        {
            // MODO NUEVO: Crear un nuevo objeto
            var nuevo = new Paciente
            {
                Nombre = txtNombre.Text,
                Telefono = txtTelefono.Text,
                FechaNacimiento = dtpFecha.Date
            };

            await _viewModel.GuardarPacienteAsync(nuevo);
        }

        // Cerramos la página modal para volver a la lista
        await Navigation.PopModalAsync();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        // Solo cerramos sin guardar nada
        await Navigation.PopModalAsync();
    }
}