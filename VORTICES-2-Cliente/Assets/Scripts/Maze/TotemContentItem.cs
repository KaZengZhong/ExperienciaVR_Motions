using UnityEngine;

namespace Vortices
{
    /// <summary>
    /// Representa un ítem de contenido cargado desde content.json.
    /// </summary>
    [System.Serializable]
    public class TotemContentItem
    {
        public string id;
        public string headline;
        public bool   isReal;
        public string category;
        public string imageUrl;
        public string videoUrl;
        public string audioUrl;
        public string searchUrl;

        // Sprite cargado en tiempo de ejecución por ContentLoader
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