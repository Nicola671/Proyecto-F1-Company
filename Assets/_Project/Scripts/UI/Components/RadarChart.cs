// ============================================================
// F1 Career Manager — RadarChart.cs
// Gráfico hexagonal de stats — pixel art style
// ============================================================
// PREFAB: RadarChart_Prefab (Canvas con RawImage)
// Se dibuja proceduralmente con una Texture2D
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace F1CareerManager.UI.Components
{
    /// <summary>
    /// Gráfico radar hexagonal para stats de piloto o auto.
    /// 6 ejes: Velocidad, Consistencia, Lluvia, Salidas, Defensa, Ataque.
    /// Soporta dos overlays para comparación.
    /// Pixel art style con líneas y puntos cuadrados.
    /// Animación de entrada al mostrarse.
    /// </summary>
    public class RadarChart : MonoBehaviour
    {
        // ── Referencias UI ───────────────────────────────────
        [Header("Renderizado")]
        [SerializeField] private RawImage _chartImage;

        [Header("Labels (6 Text alrededor)")]
        [SerializeField] private Text[] _axisLabels = new Text[6];

        [Header("Configuración")]
        [SerializeField] private int _textureSize = 256;
        [SerializeField] private float _chartRadius = 100f;
        [SerializeField] private bool _animateOnShow = true;

        // ── Nombres de ejes por defecto ──────────────────────
        private static readonly string[] DEFAULT_AXIS_NAMES = {
            "VEL", "CON", "LLU", "SAL", "DEF", "ATA"
        };

        // ── Estado ───────────────────────────────────────────
        private int[] _primaryValues;   // Piloto principal
        private int[] _compareValues;   // Piloto de comparación (puede ser null)
        private Color _primaryColor = UITheme.AccentPrimary;
        private Color _compareColor = UITheme.AccentTertiary;
        private Texture2D _texture;
        private float _animProgress = 0f;

        // ══════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Configura el radar con los 6 stats del piloto principal.
        /// Valores 0-100 para cada eje.
        /// </summary>
        public void SetPrimaryStats(int[] values, Color color,
            string[] axisNames = null)
        {
            _primaryValues = values ?? new int[6];
            _primaryColor = color;

            // Labels de los ejes
            string[] names = axisNames ?? DEFAULT_AXIS_NAMES;
            for (int i = 0; i < 6 && i < _axisLabels.Length; i++)
            {
                if (_axisLabels[i] != null)
                {
                    _axisLabels[i].text = i < names.Length ? names[i] : "";
                    _axisLabels[i].color = UITheme.TextSecondary;
                    _axisLabels[i].fontSize = UITheme.FONT_SIZE_XS;
                }
            }

            if (_animateOnShow && gameObject.activeInHierarchy)
                StartAnimation();
            else
                DrawChart(1f);
        }

        /// <summary>
        /// Agrega un segundo overlay para comparar pilotos.
        /// </summary>
        public void SetCompareStats(int[] values, Color color)
        {
            _compareValues = values;
            _compareColor = color;
            DrawChart(_animProgress);
        }

        /// <summary>Quita la comparación</summary>
        public void ClearCompare()
        {
            _compareValues = null;
            DrawChart(1f);
        }

        // ══════════════════════════════════════════════════════
        // SETUP CON DATOS DE PILOTO
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Configura directamente desde datos de piloto.
        /// </summary>
        public void SetFromPilotData(Data.PilotData pilot, Color teamColor)
        {
            int[] stats = new int[] {
                pilot.speed,
                pilot.consistency,
                pilot.rainSkill,
                pilot.startSkill,
                pilot.defense,
                pilot.attack
            };
            SetPrimaryStats(stats, teamColor);
        }

        /// <summary>
        /// Versión de auto con ejes diferentes
        /// </summary>
        public void SetFromCarData(int aero, int engine, int chassis,
            int reliability, int pitStop, int overall, Color teamColor)
        {
            int[] stats = new int[] {
                aero, engine, chassis, reliability, pitStop, overall
            };
            string[] names = { "AERO", "MOT", "CHAS", "FIAB", "PIT", "GLOBAL" };
            SetPrimaryStats(stats, teamColor, names);
        }

        // ══════════════════════════════════════════════════════
        // ANIMACIÓN
        // ══════════════════════════════════════════════════════

        private void StartAnimation()
        {
            _animProgress = 0f;
            DrawChart(0f);
        }

        private void Update()
        {
            if (_animProgress < 1f && _primaryValues != null)
            {
                _animProgress += Time.deltaTime / UITheme.ANIM_SLOW;
                if (_animProgress > 1f) _animProgress = 1f;
                DrawChart(_animProgress);
            }
        }

        // ══════════════════════════════════════════════════════
        // DIBUJO PROCEDIMENTAL
        // ══════════════════════════════════════════════════════

        private void DrawChart(float progress)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(_textureSize, _textureSize,
                    TextureFormat.RGBA32, false);
                _texture.filterMode = FilterMode.Point; // Pixel art
                _texture.wrapMode = TextureWrapMode.Clamp;
            }

            // Limpiar con transparente
            Color[] clearPixels = new Color[_textureSize * _textureSize];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;
            _texture.SetPixels(clearPixels);

            int center = _textureSize / 2;
            float radius = _textureSize * 0.4f;

            // Dibujar guías hexagonales (3 niveles: 33%, 66%, 100%)
            for (int level = 1; level <= 3; level++)
            {
                float r = radius * level / 3f;
                DrawHexagon(center, center, r, UITheme.BorderSubtle);
            }

            // Dibujar ejes
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f - Mathf.PI / 2f;
                int endX = center + (int)(Mathf.Cos(angle) * radius);
                int endY = center + (int)(Mathf.Sin(angle) * radius);
                DrawLine(center, center, endX, endY, UITheme.BorderSubtle);
            }

            // Dibujar overlay de comparación (si existe, detrás)
            if (_compareValues != null)
            {
                DrawStatPolygon(center, radius, _compareValues, progress,
                    UITheme.WithAlpha(_compareColor, 0.3f),
                    UITheme.WithAlpha(_compareColor, 0.6f));
            }

            // Dibujar polígono principal
            if (_primaryValues != null)
            {
                DrawStatPolygon(center, radius, _primaryValues, progress,
                    UITheme.WithAlpha(_primaryColor, 0.25f),
                    _primaryColor);
            }

            // Dibujar puntos cuadrados (pixel art) en los vértices
            if (_primaryValues != null)
            {
                for (int i = 0; i < 6 && i < _primaryValues.Length; i++)
                {
                    float angle = i * Mathf.PI * 2f / 6f - Mathf.PI / 2f;
                    float normalizedVal = (_primaryValues[i] / 100f) * progress;
                    int px = center + (int)(Mathf.Cos(angle) * radius * normalizedVal);
                    int py = center + (int)(Mathf.Sin(angle) * radius * normalizedVal);
                    DrawSquarePoint(px, py, 3, _primaryColor);
                }
            }

            _texture.Apply();

            if (_chartImage != null)
                _chartImage.texture = _texture;
        }

        // ══════════════════════════════════════════════════════
        // PRIMITIVAS DE DIBUJO PIXEL
        // ══════════════════════════════════════════════════════

        private void DrawStatPolygon(int center, float radius,
            int[] values, float progress, Color fillColor, Color lineColor)
        {
            // Calcular puntos del polígono
            Vector2[] points = new Vector2[6];
            for (int i = 0; i < 6 && i < values.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f - Mathf.PI / 2f;
                float normalizedVal = (values[i] / 100f) * progress;
                points[i] = new Vector2(
                    center + Mathf.Cos(angle) * radius * normalizedVal,
                    center + Mathf.Sin(angle) * radius * normalizedVal);
            }

            // Rellenar el polígono (scanline simplificado)
            FillPolygon(points, fillColor);

            // Dibujar contorno
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                DrawLine((int)points[i].x, (int)points[i].y,
                    (int)points[next].x, (int)points[next].y, lineColor);
            }
        }

        private void DrawHexagon(int cx, int cy, float r, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle1 = i * Mathf.PI * 2f / 6f - Mathf.PI / 2f;
                float angle2 = (i + 1) * Mathf.PI * 2f / 6f - Mathf.PI / 2f;
                int x1 = cx + (int)(Mathf.Cos(angle1) * r);
                int y1 = cy + (int)(Mathf.Sin(angle1) * r);
                int x2 = cx + (int)(Mathf.Cos(angle2) * r);
                int y2 = cy + (int)(Mathf.Sin(angle2) * r);
                DrawLine(x1, y1, x2, y2, color);
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            // Algoritmo de Bresenham — pixel perfecto
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixelSafe(x0, y0, color);

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void DrawSquarePoint(int cx, int cy, int size, Color color)
        {
            for (int x = -size; x <= size; x++)
            {
                for (int y = -size; y <= size; y++)
                {
                    SetPixelSafe(cx + x, cy + y, color);
                }
            }
        }

        private void FillPolygon(Vector2[] vertices, Color color)
        {
            // Relleno simple por scanline
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in vertices)
            {
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }

            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                float minX = float.MaxValue, maxX = float.MinValue;
                int n = vertices.Length;

                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    Vector2 a = vertices[i], b = vertices[j];

                    if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                    {
                        float x = a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x);
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                    }
                }

                for (int x = (int)minX; x <= (int)maxX; x++)
                    SetPixelSafe(x, y, color);
            }
        }

        private void SetPixelSafe(int x, int y, Color color)
        {
            if (x >= 0 && x < _textureSize && y >= 0 && y < _textureSize)
                _texture.SetPixel(x, y, color);
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
