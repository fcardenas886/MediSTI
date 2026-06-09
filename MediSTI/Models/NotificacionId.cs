using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediSTI.Models
{
    [Table("NotificacionIds")]
    public class NotificacionId
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int MedicamentoId { get; set; }
        public int NotifId { get; set; }
    }
}
