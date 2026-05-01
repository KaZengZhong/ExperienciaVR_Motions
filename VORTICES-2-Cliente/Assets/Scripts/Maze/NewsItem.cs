using UnityEngine;

namespace Vortices
{
    /// <summary>
    /// ScriptableObject que representa una noticia que puede ser real o falsa.
    /// Crea uno por cada tótem: Assets → Create → Vortices → News Item
    /// </summary>
    [CreateAssetMenu(fileName = "NewsItem", menuName = "Vortices/News Item")]
    public class NewsItem : ScriptableObject
    {
        [Header("Contenido de la noticia")]
        [Tooltip("Titular que se muestra en el panel del tótem")]
        public string headline = "Titular de la noticia";

        [TextArea(3, 5)]
        [Tooltip("Descripción breve o cuerpo de la noticia (opcional)")]
        public string body = "";

        [Tooltip("Imagen de la noticia que se muestra en el panel")]
        public Sprite image;

        [Header("Respuesta correcta")]
        [Tooltip("¿La noticia es real? Si es false, es falsa.")]
        public bool isReal = true;

        [Header("Navegador")]
        [Tooltip("URL que abre el navegador al investigar. Por defecto Google.")]
        public string searchUrl = "https://www.google.com";
    }
}