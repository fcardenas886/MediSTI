using MediSTI.Data;
using MediSTI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MediSTI.ViewModels
{
    public class RegistrosViewModel
    {
        private readonly DatabaseService _db;

        public ObservableCollection<RegistroDetalle> Registros { get; set; } = new();

        public RegistrosViewModel(DatabaseService db)
        {
            _db = db;
        }

        public async Task CargarHistorialAsync()
        {
            var lista = await _db.GetHistorialCompletoAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Registros.Clear();
                foreach (var r in lista)
                {
                    Registros.Add(r);
                }
            });
        }
    }
}
