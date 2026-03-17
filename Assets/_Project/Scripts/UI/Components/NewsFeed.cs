// ============================================================
// F1 Career Manager — NewsFeed.cs
// Feed de noticias scroll infinito — urgentes arriba
// ============================================================
// PREFAB: NewsFeed_Prefab (ScrollView con Content)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using F1CareerManager.Core;
using F1CareerManager.AI.PressAI;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Feed central del Hub. Scroll infinito de tarjetas de noticias.
    /// Las urgentes siempre se muestran arriba.
    /// Escucha EventBus.OnNewsGenerated para agregar en tiempo real.
    /// </summary>
    public class NewsFeed : MonoBehaviour
    {
        // ── Referencias UI ───────────────────────────────────
        [Header("ScrollView")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;

        [Header("Prefab")]
        [SerializeField] private GameObject _newsItemPrefab;

        [Header("Configuración")]
        [SerializeField] private int _maxVisibleItems = 30;
        [SerializeField] private float _itemSpacing = 8f;

        // ── Estado ───────────────────────────────────────────
        private List<GeneratedNews> _allNews = new List<GeneratedNews>();
        private List<NewsItem> _activeItems = new List<NewsItem>();
        private System.Action<GeneratedNews> _onNewsAction;

        // ══════════════════════════════════════════════════════
        // INICIALIZACIÓN
        // ══════════════════════════════════════════════════════

        private void OnEnable()
        {
            // Escuchar nuevas noticias del EventBus
            EventBus.Instance.OnNewsGenerated += HandleNewsGenerated;
        }

        private void OnDisable()
        {
            EventBus.Instance.OnNewsGenerated -= HandleNewsGenerated;
        }

        /// <summary>
        /// Configura el feed con noticias existentes y callback de acción
        /// </summary>
        public void Initialize(List<GeneratedNews> existingNews,
            System.Action<GeneratedNews> onAction = null)
        {
            _onNewsAction = onAction;
            _allNews.Clear();

            if (existingNews != null)
                _allNews.AddRange(existingNews);

            RefreshFeed();
        }

        // ══════════════════════════════════════════════════════
        // GESTIÓN DEL FEED
        // ══════════════════════════════════════════════════════

        /// <summary>Agrega una noticia nueva con animación</summary>
        public void AddNews(GeneratedNews news)
        {
            if (news == null) return;

            // Insertar según prioridad (urgentes primero)
            if (news.RequiresAction)
            {
                _allNews.Insert(0, news);
            }
            else
            {
                // Después de las urgentes, al inicio de las normales
                int insertIndex = 0;
                for (int i = 0; i < _allNews.Count; i++)
                {
                    if (!_allNews[i].RequiresAction)
                    {
                        insertIndex = i;
                        break;
                    }
                    insertIndex = i + 1;
                }
                _allNews.Insert(insertIndex, news);
            }

            RefreshFeed();

            // Animar la nueva noticia
            if (_activeItems.Count > 0)
            {
                var newItem = news.RequiresAction
                    ? _activeItems[0]
                    : _activeItems.Find(item => item != null);

                if (newItem != null)
                {
                    StartCoroutine(AnimateNewEntry(newItem, news.RequiresAction));
                }
            }
        }

        /// <summary>Refresca toda la lista visual</summary>
        public void RefreshFeed()
        {
            // Limpiar items existentes
            foreach (var item in _activeItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _activeItems.Clear();

            // Ordenar: urgentes primero, luego por fecha
            _allNews.Sort((a, b) =>
            {
                if (a.RequiresAction && !b.RequiresAction) return -1;
                if (!a.RequiresAction && b.RequiresAction) return 1;
                return b.Week.CompareTo(a.Week); // Más recientes primero
            });

            // Limitar cantidad visible
            int count = Mathf.Min(_allNews.Count, _maxVisibleItems);

            // Calcular altura del content
            if (_content != null)
            {
                float totalHeight = count * (UITheme.NEWS_CARD_HEIGHT + _itemSpacing)
                    + UITheme.PADDING_MD;
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);
            }

            // Crear items
            for (int i = 0; i < count; i++)
            {
                CreateNewsItem(_allNews[i], i);
            }
        }

        /// <summary>Archiva (elimina) noticias leídas</summary>
        public void ArchiveReadNews()
        {
            _allNews.RemoveAll(n => n.IsRead && !n.RequiresAction);
            RefreshFeed();
        }

        // ══════════════════════════════════════════════════════
        // CREACIÓN DE ITEMS
        // ══════════════════════════════════════════════════════

        private void CreateNewsItem(GeneratedNews news, int index)
        {
            GameObject itemObj;
            if (_newsItemPrefab != null)
            {
                itemObj = Instantiate(_newsItemPrefab, _content);
            }
            else
            {
                // Crear item por código como fallback
                itemObj = new GameObject($"NewsItem_{index}",
                    typeof(RectTransform), typeof(Image));
                itemObj.transform.SetParent(_content, false);

                Image bg = itemObj.GetComponent<Image>();
                bg.color = UITheme.BackgroundCard;
            }

            // Posicionar
            RectTransform rt = itemObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-UITheme.PADDING_MD * 2,
                UITheme.NEWS_CARD_HEIGHT);
            rt.anchoredPosition = new Vector2(0f,
                -(index * (UITheme.NEWS_CARD_HEIGHT + _itemSpacing))
                - UITheme.PADDING_SM);

            // Configurar NewsItem
            NewsItem newsItem = itemObj.GetComponent<NewsItem>();
            if (newsItem == null)
                newsItem = itemObj.AddComponent<NewsItem>();

            newsItem.Setup(news, OnNewsActionClicked, OnNewsDismissed);
            _activeItems.Add(newsItem);
        }

        // ══════════════════════════════════════════════════════
        // CALLBACKS
        // ══════════════════════════════════════════════════════

        private void HandleNewsGenerated(object sender,
            EventBus.NewsGeneratedArgs args)
        {
            // Crear GeneratedNews desde los args del evento
            var news = new GeneratedNews
            {
                NewsId = args.NewsId,
                Headline = args.Headline,
                Body = args.Body,
                CardType = ParseCardType(args.Type),
                MediaSource = args.MediaOutlet,
                RequiresAction = args.Type == "Urgent",
                IsRead = false,
                RelatedPilotIds = args.RelatedPilotIds ?? new List<string>(),
                RelatedTeamIds = args.RelatedTeamIds ?? new List<string>()
            };

            AddNews(news);
        }

        private void OnNewsActionClicked(GeneratedNews news)
        {
            _onNewsAction?.Invoke(news);
            NotificationToast.ShowInfo($"Atendiendo: {news.Headline}");
        }

        private void OnNewsDismissed(GeneratedNews news)
        {
            _allNews.Remove(news);
            RefreshFeed();
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIONES
        // ══════════════════════════════════════════════════════

        private IEnumerator AnimateNewEntry(NewsItem item, bool isUrgent)
        {
            yield return StartCoroutine(
                item.SlideInAnimation(0f));

            if (isUrgent)
            {
                yield return new WaitForSeconds(0.1f);
                yield return StartCoroutine(item.ShakeAnimation());
            }
        }

        // ══════════════════════════════════════════════════════
        // UTILIDADES
        // ══════════════════════════════════════════════════════

        /// <summary>Cantidad de noticias no leídas</summary>
        public int GetUnreadCount()
        {
            return _allNews.FindAll(n => !n.IsRead).Count;
        }

        /// <summary>¿Hay noticias urgentes pendientes?</summary>
        public bool HasUrgentNews()
        {
            return _allNews.Exists(n => n.RequiresAction && !n.IsRead);
        }

        private NewsCardType ParseCardType(string type)
        {
            switch (type)
            {
                case "Urgent": return NewsCardType.Urgent;
                case "Important": return NewsCardType.Important;
                case "News": return NewsCardType.News;
                case "Rumor": return NewsCardType.Rumor;
                case "Positive": return NewsCardType.Positive;
                case "Rival": return NewsCardType.Rival;
                default: return NewsCardType.News;
            }
        }
    }
}
