using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediSTI.Models
{

    //Registro es el historial real de cada toma.
    //Es la tabla más importante para saber si el paciente está cumpliendo con sus medicamentos.
    public class Registro
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int HorarioId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string Notas { get; set; }
    }

    public class RegistroDetalle
    {
        public int Id { get; set; }
        public int HorarioId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string Notas { get; set; }

        // Detalles relacionados
        public string MedicamentoNombre { get; set; }
        public string Dosis { get; set; }
        public TimeSpan HoraProgramada { get; set; }
        public string PacienteNombre { get; set; }

        // Propiedad formateadora para la vista
        public string FechaHoraFormateada => FechaHora.ToString("dd/MM/yyyy HH:mm");
        public string HoraProgramadaFormateada => HoraProgramada.ToString(@"hh\:mm");
    }
}
