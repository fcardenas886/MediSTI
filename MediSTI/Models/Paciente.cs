using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace MediSTI.Models
{
    public class Paciente
    {
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public DateTime ? FechaNacimiento { get; set; }
    }
}
