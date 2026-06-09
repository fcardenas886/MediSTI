using MediSTI.Data;
using MediSTI.Models;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediSTI.Services
{
    public class NotificationService
    {
        public static async Task SolicitarPermisosAsync()
        {
            await LocalNotificationCenter.Current.RequestNotificationPermission();
        }

        public static async Task ProgramarNotificacionesMedicamentoAsync(Medicamento med, List<Horario> horarios, DatabaseService db)
        {
            if (horarios == null || !horarios.Any() || string.IsNullOrEmpty(med.DiasSemana))
                return;

            // 1. Limpiar notificaciones previas tanto del sistema como de la BD
            await CancelarNotificacionesMedicamentoAsync(med.Id, db);

            var diasSeleccionados = med.DiasSemana.Split(',')
                                                  .Select(d => EvaluarDia(d.Trim()))
                                                  .Where(d => d != null)
                                                  .ToList();

            var listaIdsParaGuardar = new List<NotificacionId>();

            foreach (var h in horarios)
            {
                foreach (var dia in diasSeleccionados)
                {
                    DateTime fechaAlerta = CalcularPrimeraFecha(med.FechaInicio, dia.Value, h.Hora);

                    while (fechaAlerta.Date <= med.FechaFin.Date)
                    {
                        if (fechaAlerta > DateTime.Now)
                        {
                            int notificationId = (med.Id * 1000000) + (fechaAlerta.DayOfYear * 1440) + (int)fechaAlerta.TimeOfDay.TotalMinutes;

                            var request = new NotificationRequest
                            {
                                NotificationId = notificationId,
                                Title = $"💊 MediSTI: {med.Nombre}",
                                Description = $"Dosis de {med.Dosis} . {h.Hora:hh\\:mm}",
                                BadgeNumber = 1,
                                // ESTA LÍNEA ES VITAL: Debe coincidir con el ID de MainActivity
                                Android = new AndroidOptions
                                {
                                    ChannelId = "medicamentos_channel_v6", // <--- AGREGAR ESTO
                                    IconSmallName = new AndroidIcon("pastillas"),
                                    Priority = AndroidPriority.Max,
                                    // Opcional: Reforzar que no use el sonido por defecto de la librería
                                    //Sound = "absolute"
                                    
                                    AutoCancel = true,
                                    

                                },
                                Schedule = new NotificationRequestSchedule
                                {
                                    NotifyTime = fechaAlerta,
                                   RepeatType = NotificationRepeat.No
                                }
                               

                            };

                            // Lanzamos la notificación al sistema
                            await LocalNotificationCenter.Current.Show(request);

                            // Agregamos a nuestra lista para la BD
                            listaIdsParaGuardar.Add(new NotificacionId
                            {
                                MedicamentoId = med.Id,
                                NotifId = notificationId
                            });
                        }
                        fechaAlerta = fechaAlerta.AddDays(7);
                    }
                }
            }

            // 2. GUARDADO POR LOTE: Un solo viaje a la base de datos
            if (listaIdsParaGuardar.Any())
            {
                await db.GuardarMuchasNotificacionesAsync(listaIdsParaGuardar);
            }
        }

        public static async Task CancelarNotificacionesMedicamentoAsync(int medicamentoId, DatabaseService db)
        {
            var ids = await db.GetNotificacionIdsAsync(medicamentoId);

            foreach (var n in ids)
            {
                LocalNotificationCenter.Current.Cancel(n.NotifId);
            }

            await db.EliminarNotificacionIdsAsync(medicamentoId);
        }

        private static DayOfWeek? EvaluarDia(string diaAbreviado)
        {
            return diaAbreviado switch
            {
                "Lu" => DayOfWeek.Monday,
                "Ma" => DayOfWeek.Tuesday,
                "Mi" => DayOfWeek.Wednesday,
                "Ju" => DayOfWeek.Thursday,
                "Vi" => DayOfWeek.Friday,
                "Sá" => DayOfWeek.Saturday,
                "Do" => DayOfWeek.Sunday,
                _ => null
            };
        }

        private static DateTime CalcularPrimeraFecha(DateTime inicioTratamiento, DayOfWeek diaObjetivo, TimeSpan hora)
        {
            DateTime fechaBase = inicioTratamiento.Date.Add(hora);
            int diasDiferencia = ((int)diaObjetivo - (int)fechaBase.DayOfWeek + 7) % 7;
            DateTime primeraToma = fechaBase.AddDays(diasDiferencia);

            if (primeraToma < DateTime.Now)
            {
                primeraToma = primeraToma.AddDays(7);
            }

            return primeraToma;
        }
    }
}
