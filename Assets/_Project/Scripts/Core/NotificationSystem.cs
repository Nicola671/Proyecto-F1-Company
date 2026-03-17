// ============================================================
// F1 Career Manager — NotificationSystem.cs
// Sistema de notificaciones locales y push (móvil)
// Avisa de próxima carrera, fichajes, eventos, etc.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using Unity.Notifications.Android;
using Unity.Notifications.iOS;
#endif
using F1CareerManager.Core;

namespace F1CareerManager.Core
{
    public class NotificationSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static NotificationSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool notificationsEnabled = true;
        [SerializeField] private string channelId = "f1_career_man_reminders";

        // ══════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ══════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeChannels();
        }

        private void InitializeChannels()
        {
#if UNITY_ANDROID
            var c = new AndroidNotificationChannel()
            {
                Id = channelId,
                Name = "Recordatorios de Carrera",
                Importance = Importance.High,
                Description = "Notificaciones para informarte sobre próximas carreras y fichajes.",
            };
            AndroidNotificationCenter.RegisterNotificationChannel(c);
#endif
        }

        // ══════════════════════════════════════════════════════
        // PUSH NOTIFICATIONS (FUERA DEL JUEGO)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Programa una notificación para avisar de la próxima carrera
        /// </summary>
        public void ScheduleRaceReminder(string circuitName, DateTime raceDate)
        {
            if (!notificationsEnabled) return;

            string title = "🏎️ ¡Día de Gran Premio!";
            string body = $"El GP de {circuitName} está por comenzar. Los motores ya están calientes.";

            ScheduleLocalNotification(title, body, raceDate.AddMinutes(-10)); // 10 min antes
        }

        /// <summary>
        /// Avisa que un rival ha fichado a un piloto importante
        /// </summary>
        public void SendRivalTransferPush(string pilotName, string teamName)
        {
            if (!notificationsEnabled) return;

            string title = "⚠️ Bombazo en el Mercado";
            string body = $"{pilotName} acaba de fichar por {teamName}. ¡El paddock está en shock!";

            ScheduleLocalNotification(title, body, DateTime.Now.AddMinutes(1)); // Inmediato
        }

        private void ScheduleLocalNotification(string title, string body, DateTime deliveryTime)
        {
#if UNITY_ANDROID
            var notification = new AndroidNotification();
            notification.Title = title;
            notification.Text = body;
            notification.FireTime = deliveryTime;
            notification.SmallIcon = "icon_0";
            notification.LargeIcon = "icon_1";

            AndroidNotificationCenter.SendNotification(notification, channelId);
            Debug.Log($"[NotificationSystem] Notificación programada p/ {deliveryTime}: {title}");
#elif UNITY_IOS
            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = (deliveryTime - DateTime.Now).TotalSeconds > 0 ? (deliveryTime - DateTime.Now) : TimeSpan.FromSeconds(1),
                Repeats = false
            };

            var notification = new iOSNotification()
            {
                Identifier = Guid.NewGuid().ToString(),
                Title = title,
                Body = body,
                Trigger = timeTrigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
            Debug.Log($"[NotificationSystem] Notificación programada p/ {deliveryTime}: {title}");
#else
            Debug.Log($"[NotificationSystem] [SIMULADO] {title}: {body} en {deliveryTime}");
#endif
        }

        // ══════════════════════════════════════════════════════
        // IN-APP NOTIFICATIONS (TOASTS) - YA CREADO EN UI
        // ══════════════════════════════════════════════════════

        public void SendInAppToast(string title, string message, string type = "INFO")
        {
            // El UIManager suele tener un ToastQueue
            // UIManager.Instance.ShowToast(title, message, type);
            Debug.Log($"[NotificationSystem] Toast: {title} - {message}");
        }

        // ══════════════════════════════════════════════════════
        // API PÚBLICA
        // ══════════════════════════════════════════════════════

        public void ClearAllNotifications()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
            Debug.Log("[NotificationSystem] Notificaciones canceladas");
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            notificationsEnabled = enabled;
            if (!enabled) ClearAllNotifications();
        }
    }
}
