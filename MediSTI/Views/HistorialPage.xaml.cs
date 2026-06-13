using MediSTI.ViewModels;

namespace MediSTI.Views
{
    public partial class HistorialPage : ContentPage
    {
        private readonly RegistrosViewModel _viewModel;

        public HistorialPage(RegistrosViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.CargarHistorialAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar historial: {ex.Message}");
            }
        }
    }
}