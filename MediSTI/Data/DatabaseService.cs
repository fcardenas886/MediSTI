
using MediSTI.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediSTI.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "MediSTI.db3");
        }

        private async Task Init()
        {
            if (_db != null) return;

            _db = new SQLiteAsyncConnection(_dbPath);

            // Crea las tablas. SQLite detectará automáticamente nuevas columnas 
            // como 'DiasSemana' si ya desinstalaste la app previa.
            await _db.CreateTableAsync<Paciente>();
            await _db.CreateTableAsync<Medicamento>();
            await _db.CreateTableAsync<Horario>();
            await _db.CreateTableAsync<Registro>();
            // En Init() agregar:
            await _db.CreateTableAsync<NotificacionId>();
        }

        // --- MÉTODOS PARA PACIENTES ---
        public async Task<List<Paciente>> GetPacientesAsync()
        {
            await Init();
            return await _db.Table<Paciente>().ToListAsync();
        }

        public async Task<int> SavePacienteAsync(Paciente paciente)
        {
            await Init();
            return paciente.Id != 0 ? await _db.UpdateAsync(paciente) : await _db.InsertAsync(paciente);
        }

        public async Task<int> DeletePacienteAsync(Paciente paciente)
        {
            await Init();
            return await _db.DeleteAsync(paciente);
        }


        // ── MEDICAMENTOS ───────────────────────────
        public async Task<List<Medicamento>> GetMedicamentosAsync(int pacienteId)
        {
            await Init();
            return await _db.Table<Medicamento>()
                .Where(m => m.PacienteId == pacienteId)
                .ToListAsync();
        }

        public async Task<int> SaveMedicamentoAsync(Medicamento m)
        {
            await Init();
            // Al retornar el ID después de insertar, permites que el ViewModel
            // sepa a qué ID vincular los nuevos horarios.
            if (m.Id == 0)
            {
                await _db.InsertAsync(m);
                return m.Id;
            }
            else
            {
                return await _db.UpdateAsync(m);
            }
        }

        public async Task<int> DeleteMedicamentoAsync(Medicamento m)
        {
            await Init();
            return await _db.DeleteAsync(m);
        }

        // ── HORARIOS ───────────────────────────────
        public async Task<List<Horario>> GetHorariosByMedicamentoAsync(int medicamentoId)
        {
            await Init(); // IMPORTANTE: Agregado para evitar errores de conexión
            return await _db.Table<Horario>()
                            .Where(h => h.MedicamentoId == medicamentoId)
                            .ToListAsync();
        }

        public async Task<int> SaveHorarioAsync(Horario h)
        {
            await Init();
            return h.Id == 0 ? await _db.InsertAsync(h) : await _db.UpdateAsync(h);
        }

        public async Task<int> DeleteHorarioAsync(Horario h)
        {
            await Init();
            return await _db.DeleteAsync(h);
        }

        // ── REGISTROS ──────────────────────────────
        public async Task<int> SaveRegistroAsync(Registro r)
        {
            await Init();
            return r.Id == 0 ? await _db.InsertAsync(r) : await _db.UpdateAsync(r);
        }

        // notificaciones 

        // Métodos nuevos al final de la clase:
        public async Task GuardarNotificacionIdAsync(NotificacionId notif)
        {
            await Init();
            await _db.InsertAsync(notif);
        }

        public async Task<List<NotificacionId>> GetNotificacionIdsAsync(int medicamentoId)
        {
            await Init();
            return await _db.Table<NotificacionId>()
                .Where(n => n.MedicamentoId == medicamentoId)
                .ToListAsync();
        }

        public async Task EliminarNotificacionIdsAsync(int medicamentoId)
        {
            await Init();
            await _db.Table<NotificacionId>()
                .Where(n => n.MedicamentoId == medicamentoId)
                .DeleteAsync();
        }

        // Dentro de tu clase DatabaseService
        public async Task<int> GuardarMuchasNotificacionesAsync(List<NotificacionId> listaNotificaciones)
        {
            // Verificamos que la lista no esté vacía
            if (listaNotificaciones == null || !listaNotificaciones.Any())
                return 0;

            try
            {
                // InsertAllAsync es el método de lote de SQLite. 
                // Es muchísimo más rápido que InsertAsync en un bucle.
                return await _db.InsertAllAsync(listaNotificaciones);
            }
            catch (Exception ex)
            {
                // Log del error si es necesario
                System.Diagnostics.Debug.WriteLine($"Error STI en inserción por lote: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<Medicamento>> GetAllMedicamentosActivosAsync()
        {
            await Init();

            var hoy = DateTime.Today;

            var todos = await _db.Table<Medicamento>().ToListAsync();
            return todos
                .Where(m => m.FechaInicio.Date <= hoy && m.FechaFin.Date >= hoy)
                .ToList();
        }

        // Opcional: Método para obtener todos los registros de hoy (para estadísticas)
        public async Task<List<Registro>> GetRegistrosDeHoyAsync()
        {
            await Init();
            var hoy = DateTime.Today;

            return await _db.Table<Registro>()
                             .Where(r => r.FechaHora.Date == hoy)
                             .ToListAsync();
        }

        public async Task<List<RegistroDetalle>> GetHistorialCompletoAsync()
        {
            await Init();

            var registros = await _db.Table<Registro>()
                                     .OrderByDescending(r => r.FechaHora)
                                     .ToListAsync();

            var historial = new List<RegistroDetalle>();

            foreach (var r in registros)
            {
                var horario = await _db.Table<Horario>()
                                       .Where(h => h.Id == r.HorarioId)
                                       .FirstOrDefaultAsync();

                if (horario != null)
                {
                    var med = await _db.Table<Medicamento>()
                                       .Where(m => m.Id == horario.MedicamentoId)
                                       .FirstOrDefaultAsync();

                    if (med != null)
                    {
                        var pac = await _db.Table<Paciente>()
                                           .Where(p => p.Id == med.PacienteId)
                                           .FirstOrDefaultAsync();

                        historial.Add(new RegistroDetalle
                        {
                            Id = r.Id,
                            HorarioId = r.HorarioId,
                            FechaHora = r.FechaHora,
                            Estado = r.Estado,
                            Notas = r.Notas,
                            MedicamentoNombre = med.Nombre ?? "Sin nombre",
                            Dosis = med.Dosis ?? "",
                            HoraProgramada = horario.Hora,
                            PacienteNombre = pac?.Nombre ?? "Sin paciente"
                        });
                    }
                }
            }

            return historial;
        }

        /// <summary>
        /// Obtiene todas las tomas programadas para HOY (medicamento + horario)
        /// </summary>
        public async Task<List<TomaDelDia>> GetTomasDeHoyAsync()
        {
            await Init();

            var hoy = DateTime.Today;
            var ahora = DateTime.Now.TimeOfDay;

            // Día actual en formato abreviado
            var diaHoy = hoy.DayOfWeek switch
            {
                DayOfWeek.Monday => "Lu",
                DayOfWeek.Tuesday => "Ma",
                DayOfWeek.Wednesday => "Mi",
                DayOfWeek.Thursday => "Ju",
                DayOfWeek.Friday => "Vi",
                DayOfWeek.Saturday => "Sá",
                DayOfWeek.Sunday => "Do",
                _ => ""
            };

            // 1 — Obtener todos los medicamentos y filtrar de forma robusta por fecha en memoria
            var todosMedicamentos = await _db.Table<Medicamento>().ToListAsync();
            var medicamentos = todosMedicamentos
                .Where(m => m.FechaInicio.Date <= hoy && m.FechaFin.Date >= hoy)
                .ToList();

            // 2 — Filtrar por día de la semana
            medicamentos = medicamentos
                .Where(m => string.IsNullOrEmpty(m.DiasSemana) ||
                            m.DiasSemana == "Todos" ||
                            m.DiasSemana.Contains(diaHoy))
                .ToList();

            var tomas = new List<TomaDelDia>();

            foreach (var med in medicamentos)
            {
                // 3 — Traer paciente
                var paciente = await _db.Table<Paciente>()
                    .Where(p => p.Id == med.PacienteId)
                    .FirstOrDefaultAsync();

                // 4 — Traer horarios activos
                var horarios = await _db.Table<Horario>()
                    .Where(h => h.MedicamentoId == med.Id && h.Activo)
                    .ToListAsync();

                foreach (var h in horarios)
                {
                    // Evitar mostrar tomas anteriores al inicio real del tratamiento
                    var fechaHoraToma = hoy.Add(h.Hora);
                    if (fechaHoraToma < med.FechaInicio)
                        continue;

                    // 5 — Verificar si ya fue tomada hoy
                    var registros = await _db.Table<Registro>()
                        .Where(r => r.HorarioId == h.Id)
                        .ToListAsync();

                    var registroHoy = registros
                        .FirstOrDefault(r => r.FechaHora.Date == hoy);

                    tomas.Add(new TomaDelDia
                    {
                        MedicamentoId = med.Id,
                        HorarioId = h.Id,
                        PacienteId = med.PacienteId ?? 0,
                        NombrePaciente = paciente?.Nombre ?? "Sin paciente",
                        MedicamentoNombre = med.Nombre ?? "Sin nombre",
                        Dosis = med.Dosis ?? "",
                        Hora = h.Hora,
                        YaTomada = registroHoy != null,
                        FechaHoraTomada = registroHoy?.FechaHora
                    });
                }
            }

            // 6 — Ordenar: primero pendientes, luego por hora
            return tomas
                .OrderBy(t => t.YaTomada)
                .ThenBy(t => t.Hora)
                .ToList();
        }



        // Método auxiliar (puedes ponerlo en la misma clase)
        private bool EsDiaValidoHoy(string diasSemana)
        {
            if (string.IsNullOrWhiteSpace(diasSemana)) return true;

            var hoy = DateTime.Today.DayOfWeek;
            var dias = diasSemana.Split(',').Select(d => d.Trim().ToUpper()).ToList();

            return dias.Any(d =>
                (d.Contains("LU") && hoy == DayOfWeek.Monday) ||
                (d.Contains("MA") && hoy == DayOfWeek.Tuesday) ||
                (d.Contains("MI") && hoy == DayOfWeek.Wednesday) ||
                (d.Contains("JU") && hoy == DayOfWeek.Thursday) ||
                (d.Contains("VI") && hoy == DayOfWeek.Friday) ||
                (d.Contains("SA") && hoy == DayOfWeek.Saturday) ||
                (d.Contains("DO") && hoy == DayOfWeek.Sunday));
        }
    }
}