using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediSTI.Models
{
    public class Horario
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int? MedicamentoId { get; set; }
        public TimeSpan Hora { get; set; }
       
        public bool Activo { get; set; }
    }
}
