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
}
