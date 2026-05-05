using UnityEngine;

namespace Vortices
{
    /// <summary>
    /// Representa un ítem de contenido cargado desde content.json.
    /// Equivale al antiguo ScriptableObject NewsItem, pero se carga en tiempo de ejecución.
    /// </summary>
    [System.Serializable]
    public class TotemContentItem
    {
        public string id;
        public string headline;
        public bool   isReal;
        public string category;
        public string imageUrl;
        public string searchUrl;

        // Imagen cargada en tiempo de ejecución por ContentLoader
        [System.NonSerialized]
        public Sprite sprite;
    }

    /// <summary>
    /// Wrapper para deserializar el array raíz del JSON.
    /// </summary>
    [System.Serializable]
    public class TotemContentList
    {
        public TotemContentItem[] items;
    }
}