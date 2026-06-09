using System;
using System.Collections.Generic;
using System.Text;

namespace MediSTI.Models
{
    public class FarmaciaTurno
    {
        public string local_nombre { get; set; }
        public string local_direccion { get; set; }
        public string comuna_nombre { get; set; }
        public string local_telefono { get; set; }
        public string local_lat { get; set; }
        public string local_lng { get; set; }

        public string funcionamiento_hora_apertura { get; set; }
        public string funcionamiento_hora_cierre { get; set; }
    }
}
