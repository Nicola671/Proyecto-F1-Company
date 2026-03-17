// ============================================================
// F1 Career Manager — JsonHelper.cs
// Serialización y carga de archivos JSON
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using System;

namespace F1CareerManager.Utils
{
    public static class JsonHelper
    {
        public static T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        public static string ToJson<T>(T obj, bool prettyPrint = false)
        {
            return JsonUtility.ToJson(obj, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }

        public static T[] FromJsonArray<T>(string json)
        {
            string newJson = "{ \"items\": " + json + " }";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.items;
        }
    }
}
