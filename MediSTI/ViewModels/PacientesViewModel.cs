using MediSTI.Data;
using MediSTI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MediSTI.ViewModels
{
    public class PacientesViewModel
    {
        private readonly DatabaseService _db;

        public ObservableCollection<Paciente> Pacientes { get; set; } = new();

        public PacientesViewModel(DatabaseService db)
        {
            _db = db;
        }

        public async Task CargarPacientesAsync()
        {
            var lista = await _db.GetPacientesAsync();
            Pacientes.Clear();
            foreach (var p in lista)
                Pacientes.Add(p);
        }

        public async Task GuardarPacienteAsync(Paciente p)
        {
            await _db.SavePacienteAsync(p);
            await CargarPacientesAsync();
        }

        public async Task EliminarPacienteAsync(Paciente p)
        {
            await _db.DeletePacienteAsync(p);
            await CargarPacientesAsync();
        }
    }
}
