// ============================================================
// F1 Career Manager — CircuitDataEditor.cs
// Custom Editor para visualizar y editar circuitos en el Inspector
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using F1CareerManager.Data;

namespace F1CareerManager.Editor
{
    [CustomEditor(typeof(CircuitData))]
    public class CircuitDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CircuitData c = (CircuitData)target;

            EditorGUILayout.LabelField("FICHA DEL CIRCUITO", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            c.name = EditorGUILayout.TextField("Nombre GP", c.name);
            c.shortName = EditorGUILayout.TextField("Nombre Corto", c.shortName);
            c.country = EditorGUILayout.TextField("País", c.country);
            c.round = EditorGUILayout.IntField("Ronda n°", c.round);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("DATOS TÉCNICOS", EditorStyles.helpBox);
            
            c.laps = EditorGUILayout.IntField("Vueltas", c.laps);
            c.circuitLength = EditorGUILayout.FloatField("Longitud (km)", c.circuitLength);
            c.raceDistance = EditorGUILayout.FloatField("Distancia Carrera (km)", c.raceDistance);
            c.isSprint = EditorGUILayout.Toggle("¿Es fin de semana Sprint?", c.isSprint);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(c);
            }
        }
    }
}
#endif
