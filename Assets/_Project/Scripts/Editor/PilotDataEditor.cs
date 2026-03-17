// ============================================================
// F1 Career Manager — PilotDataEditor.cs
// Custom Editor para visualizar y editar pilotos en el Inspector
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using F1CareerManager.Data;

namespace F1CareerManager.Editor
{
    [CustomEditor(typeof(PilotData))]
    public class PilotDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PilotData p = (PilotData)target;

            EditorGUILayout.LabelField("FICHA TÉCNICA DEL PILOTO", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            p.fullName = EditorGUILayout.TextField("Nombre Full", p.fullName);
            p.shortName = EditorGUILayout.TextField("Abreviatura (VER, HAM)", p.shortName);
            p.teamId = EditorGUILayout.TextField("Equipo Actual ID", p.teamId);
            p.stars = EditorGUILayout.IntSlider("Estrellas (1-5)", p.stars, 1, 5);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ESTADÍSTICAS", EditorStyles.helpBox);
            
            p.stats.speed = EditorGUILayout.IntSlider("Velocidad", p.stats.speed, 0, 100);
            p.stats.consistency = EditorGUILayout.IntSlider("Consistencia", p.stats.consistency, 0, 100);
            p.stats.rain = EditorGUILayout.IntSlider("Lluvia", p.stats.rain, 0, 100);
            p.stats.starts = EditorGUILayout.IntSlider("Salidas", p.stats.starts, 0, 100);
            p.stats.defense = EditorGUILayout.IntSlider("Defensa", p.stats.defense, 0, 100);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(p);
            }
        }
    }
}
#endif
