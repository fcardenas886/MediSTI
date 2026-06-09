using MediSTI.Data;
using MediSTI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MediSTI.ViewModels
{
    public class HorariosViewModel
    {
        private readonly DatabaseService _db;

        public ObservableCollection<Horario> Horarios { get; set; } = new();

        public HorariosViewModel(DatabaseService db)
        {
            _db = db;
        }

        public async Task CargarHorariosAsync(int medicamentoId)
        {
            var lista = await _db.GetHorariosByMedicamentoAsync(medicamentoId);
            Horarios.Clear();
            foreach (var h in lista)
                Horarios.Add(h);
        }

        public async Task GuardarHorarioAsync(Horario h)
        {
            await _db.SaveHorarioAsync(h);
            await CargarHorariosAsync(h.MedicamentoId ?? 0);
        }

        public async Task EliminarHorarioAsync(Horario h)
        {
            await _db.DeleteHorarioAsync(h);
            await CargarHorariosAsync(h.MedicamentoId ?? 0);
        }
    }
}
