using MediSTI.Data;
using MediSTI.Models;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using Plugin.LocalNotification.EventArgs;
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

            List<DayOfWeek> diasSeleccionados;
            if (med.DiasSemana.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                diasSeleccionados = new List<DayOfWeek>
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday,
                    DayOfWeek.Saturday,
                    DayOfWeek.Sunday
                };
            }
            else
            {
                diasSeleccionados = med.DiasSemana.Split(',')
                                                      .Select(d => EvaluarDia(d.Trim()))
                                                      .Where(d => d.HasValue)
                                                      .Select(d => d.Value)
                                                      .ToList();
            }

            if (!diasSeleccionados.Any())
                return;

            var listaIdsParaGuardar = new List<NotificacionId>();

            foreach (var h in horarios)
            {
                foreach (var dia in diasSeleccionados)
                {
                    DateTime fechaAlerta = CalcularPrimeraFecha(med.FechaInicio, dia, h.Hora);

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
                                Android = new AndroidOptions
                                {
                                    ChannelId = med.EsAlarma ? "medicamentos_channel_alarm_v3" : "medicamentos_channel_gentle_v3",
                                    IconSmallName = new AndroidIcon("pastillas"),
                                    Priority = med.EsAlarma ? AndroidPriority.Max : AndroidPriority.High,
                                    AutoCancel = true,
                                },
                                Schedule = new NotificationRequestSchedule
                                {
                                    NotifyTime = fechaAlerta,
                                    RepeatType = NotificationRepeat.No
                                },
                                ReturningData = $"medId={med.Id}&horarioId={h.Id}",
                                CategoryType = NotificationCategoryType.Reminder
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
                "Sa" or "Sá" => DayOfWeek.Saturday,
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

        public static void InicializarActionTapped()
        {
            // Nos desuscribimos primero para evitar suscripciones duplicadas
            LocalNotificationCenter.Current.NotificationActionTapped -= OnNotificationActionTapped;
            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
        }

        private static void OnNotificationActionTapped(NotificationActionEventArgs e)
        {
            if (e == null || e.Request == null) return;

            int actionId = e.ActionId;
            string returningData = e.Request.ReturningData;

            if (string.IsNullOrEmpty(returningData)) return;

            try
            {
                // El formato de los datos es: medId=X&horarioId=Y
                var parts = returningData.Split('&')
                                         .Select(p => p.Split('='))
                                         .ToDictionary(kv => kv[0], kv => kv[1]);

                if (parts.TryGetValue("medId", out string medIdStr) && parts.TryGetValue("horarioId", out string horarioIdStr))
                {
                    int medId = int.Parse(medIdStr);
                    int horarioId = int.Parse(horarioIdStr);

                    if (actionId == 101) // 🟢 TOMADO
                    {
                        // 1. Guardar toma directamente en la base de datos de forma asíncrona
                        Task.Run(async () =>
                        {
                            try
                            {
                                var db = new DatabaseService();
                                var registro = new Registro
                                {
                                    HorarioId = horarioId,
                                    FechaHora = DateTime.Now,
                                    Estado = "Tomado",
                                    Notas = "Registrado desde la barra de notificaciones"
                                };
                                await db.SaveRegistroAsync(registro);

                                // 2. Refrescar la pantalla de inicio si la app está en primer plano
                                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    if (Shell.Current?.CurrentPage is Views.HomePage homePage)
                                    {
                                        if (homePage.BindingContext is ViewModels.MainViewModel vm)
                                        {
                                            await vm.CargarTomasDeHoyAsync();
                                        }
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error al registrar toma desde notificación: {ex.Message}");
                            }
                        });
                    }
                    else if (actionId == 102) // ⏰ POSPONER 15M
                    {
                        // 1. Reprogramar una nueva notificación idéntica para 15 minutos más tarde
                        Task.Run(async () =>
                        {
                            try
                            {
                                var originalRequest = e.Request;
                                var request = new NotificationRequest
                                {
                                    NotificationId = originalRequest.NotificationId + 9999, // ID derivado y único
                                    Title = $"⏰ POSPONER - {originalRequest.Title}",
                                    Description = originalRequest.Description,
                                    ReturningData = returningData, // Mismos datos
                                    CategoryType = originalRequest.CategoryType, // Mismo tipo de categoría para conservar botones
                                    Android = originalRequest.Android,
                                    Schedule = new NotificationRequestSchedule
                                    {
                                        NotifyTime = DateTime.Now.AddMinutes(15),
                                        RepeatType = NotificationRepeat.No
                                    }
                                };
                                await LocalNotificationCenter.Current.Show(request);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error al posponer recordatorio: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al procesar acción de notificación: {ex.Message}");
            }
        }
    }
}
