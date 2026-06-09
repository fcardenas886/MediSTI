using System;
using System.Collections.Generic;
using System.Text;

namespace MediSTI.Models
{
    
        public class TomaDelDia : BindableObject
        {
            public int MedicamentoId { get; set; }
            public int HorarioId { get; set; }
            public int PacienteId { get; set; }
            public string NombrePaciente { get; set; } = string.Empty;
            public string MedicamentoNombre { get; set; } = string.Empty;
            public string Dosis { get; set; } = string.Empty;
            public TimeSpan Hora { get; set; }
            public string HoraFormateada => Hora.ToString(@"hh\:mm");
            public DateTime? FechaHoraTomada { get; set; }

            // YaTomada con notificación
            private bool _yaTomada = false;
            public bool YaTomada
            {
                get => _yaTomada;
                set
                {
                    _yaTomada = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Estado));
                    OnPropertyChanged(nameof(ColorEstado));
                    OnPropertyChanged(nameof(IconoEstado));
                    OnPropertyChanged(nameof(EstaVencida));
                }
            }

            // TiempoRestante con notificación
            private string _tiempoRestante = "";
            public string TiempoRestante
            {
                get => _tiempoRestante;
                set
                {
                    _tiempoRestante = value;
                    OnPropertyChanged();
                }
            }

            // Propiedades calculadas
            public bool EstaVencida => !YaTomada && Hora < DateTime.Now.TimeOfDay;
            public string Estado => YaTomada ? "Tomado" : EstaVencida ? "Vencida" : "Pendiente";
            public string ColorEstado => YaTomada ? "#27AE60" : EstaVencida ? "#E74C3C" : "#185FA5";
            public string IconoEstado => YaTomada ? "✅" : EstaVencida ? "❌" : "⏰";
        }
    
}
