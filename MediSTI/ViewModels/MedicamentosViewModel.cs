
using MediSTI.Data;
using MediSTI.Models;
using MediSTI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MediSTI.ViewModels
{
    public class MedicamentosViewModel
    {
        private readonly DatabaseService _db;

        public DatabaseService Database => _db;
        public ObservableCollection<Medicamento> Medicamentos { get; set; } = new();

        public MedicamentosViewModel(DatabaseService db)
        {
            _db = db;
        }

        public async Task CargarMedicamentosAsync(int pacienteId)
        {
            // 1. Obtenemos la lista básica de medicamentos desde SQLite
            var lista = await _db.GetMedicamentosAsync(pacienteId);

            Medicamentos.Clear();

            foreach (var med in lista)
            {
                // 2. Buscamos sus horarios vinculados
                var horarios = await _db.GetHorariosByMedicamentoAsync(med.Id);

                // Asignamos la lista para que el FlexLayout dibuje las burbujas
                med.ListaHorarios = horarios;

                // 3. VALIDACIÓN DE DÍAS (IMPORTANTE):
                // Si el campo DiasSemana está vacío en la base de datos, 
                // le asignamos un valor por defecto para verificar que el XAML lo pinte.
                if (string.IsNullOrWhiteSpace(med.DiasSemana))
                {
                    med.DiasSemana = "Días no especificados"; // Esto confirmará si el Binding funciona
                }

                Medicamentos.Add(med);
            }
        }

        //public async Task GuardarMedicamentoAsync(Medicamento m)
        //{
        //    await _db.SaveMedicamentoAsync(m);
        //    await CargarMedicamentosAsync(m.PacienteId ?? 0);
        //}

        // En MedicamentosViewModel.cs

        // 1. Guardar el medicamento y retornar su ID
        // Método para guardar el medicamento
        // Método para guardar el medicamento y devolver su ID
        public async Task<int> GuardarMedicamentoAsync(Medicamento medicamento)
        {
            // Llamamos al servicio que nos pasaste
            await _db.SaveMedicamentoAsync(medicamento);

            // SQLite-net-PCL actualiza automáticamente la propiedad .Id del objeto 
            // después del InsertAsync. Lo devolvemos para los horarios.
            return medicamento.Id;
        }

        // Método para guardar el horario
        public async Task<int> GuardarHorarioAsync(Horario horario)
        {
            return await _db.SaveHorarioAsync(horario);
        }
        public async Task EliminarMedicamentoAsync(Medicamento m)
        {
            // 1. CANCELAR ALERTAS: Antes de borrar el medicamento, limpiamos el sistema
            await NotificationService.CancelarNotificacionesMedicamentoAsync(m.Id, _db);

            // 2. BORRAR HORARIOS: Limpiamos las dependencias
            var horarios = await _db.GetHorariosByMedicamentoAsync(m.Id);
            foreach (var h in horarios) await _db.DeleteHorarioAsync(h);

            await _db.DeleteMedicamentoAsync(m);
            await CargarMedicamentosAsync(m.PacienteId ?? 0);
        }
    

    public async Task ActualizarMedicamentoAsync(Medicamento med, List<Horario> nuevosHorarios)
        {
            //actualiza los datos del medicamento incluyendo dia semana
            // 1. Usamos SaveMedicamentoAsync (que ya detecta si es Update o Insert)
            await _db.SaveMedicamentoAsync(med);

            // 2. Manejo de Horarios: Buscamos los actuales
            var horariosViejos = await _db.GetHorariosByMedicamentoAsync(med.Id);

            foreach (var h in horariosViejos)
            {
               
                // Usamos el método específico de tu servicio para borrar
                await _db.DeleteHorarioAsync(h);
            }

            // 3. Aquí puedes proceder a insertar los nuevos horarios si la frecuencia cambió
            // 3. Guarda los nuevos horarios generados en la edición
            foreach (var h in nuevosHorarios)
            {
                h.MedicamentoId = med.Id;
                h.Activo = true;
                await _db.SaveHorarioAsync(h);
            }

            // 3. ¡LA CONEXIÓN! Programar las alertas en el teléfono
            await NotificationService.ProgramarNotificacionesMedicamentoAsync(med, nuevosHorarios,_db);


        }

        // Agrega esto en MedicamentosViewModel.cs
        
    } }
