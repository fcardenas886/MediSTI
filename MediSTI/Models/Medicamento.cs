using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediSTI.Models
{
    //cada medicina asignada a un paciente, con dosis, frecuencia y el período de tratamiento.
    public class Medicamento
    {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int? PacienteId { get; set; }
        public string Nombre { get; set; }
        public string Dosis { get; set; }
        public string Frecuencia { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public string DiasSemana { get; set; }

        [Ignore] // Esto le dice a SQLite que no cree una columna para esto
        public List<Horario> ListaHorarios { get; set; } = new List<Horario>();
    }
}
