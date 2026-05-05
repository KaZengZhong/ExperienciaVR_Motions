using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Vortices
{
    /// <summary>
    /// Carga content.json desde una URL externa (p. ej. GitHub raw) al iniciar la escena
    /// y pone los ítems disponibles para ProceduralMapGenerator.
    ///
    /// Uso: ContentLoader.Instance.Items
    ///
    /// Coloca este script en un GameObject vacío llamado "ContentLoader" en la escena.
    /// ProceduralMapGenerator esperará a que termine de cargar antes de generar el mapa.
    ///
    /// — contentJsonUrl  : URL raw del archivo content.json en GitHub (u otro host)
    /// — imagesBaseUrl   : URL base donde están las imágenes (sin slash final).
    ///                     Si imageUrl en el JSON ya es una URL completa (empieza con http),
    ///                     se usa directamente y se ignora imagesBaseUrl.
    /// </summary>
    public class ContentLoader : MonoBehaviour
    {
        public static ContentLoader Instance { get; private set; }

        [Header("URLs externas")]
        [Tooltip("URL raw del JSON, ej: https://raw.githubusercontent.com/usuario/repo/main/content.json")]
        public string contentJsonUrl = "https://raw.githubusercontent.com/TU_USUARIO/TU_REPO/main/content.json";

        [Tooltip("URL base para las imágenes, ej: https://raw.githubusercontent.com/usuario/repo/main/images")]
        public string imagesBaseUrl  = "https://raw.githubusercontent.com/TU_USUARIO/TU_REPO/main/images";

        // Lista de ítems listos para usar (con sprites cargados)
        public List<TotemContentItem> Items { get; private set; } = new List<TotemContentItem>();
        public bool IsReady { get; private set; } = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            StartCoroutine(LoadContent());
        }

        private IEnumerator LoadContent()
        {
            Debug.Log($"[ContentLoader] Descargando JSON desde: {contentJsonUrl}");

            UnityWebRequest jsonRequest = UnityWebRequest.Get(contentJsonUrl);
            yield return jsonRequest.SendWebRequest();

            if (jsonRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ContentLoader] No se pudo cargar el JSON: {jsonRequest.error}");
                IsReady = true; // continuar aunque no haya contenido
                yield break;
            }

            string json = jsonRequest.downloadHandler.text;
            TotemContentList contentList = JsonUtility.FromJson<TotemContentList>(json);

            if (contentList == null || contentList.items == null || contentList.items.Length == 0)
            {
                Debug.LogWarning("[ContentLoader] El JSON está vacío o mal formado.");
                IsReady = true;
                yield break;
            }

            // Cargar imágenes para cada ítem
            foreach (TotemContentItem item in contentList.items)
            {
                if (!string.IsNullOrEmpty(item.imageUrl))
                {
                    // Si imageUrl ya es una URL completa, usarla directamente
                    string imgUrl = item.imageUrl.StartsWith("http")
                        ? item.imageUrl
                        : $"{imagesBaseUrl.TrimEnd('/')}/{item.imageUrl}";

                    Debug.Log($"[ContentLoader] Descargando imagen: {imgUrl}");

                    UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(imgUrl);
                    yield return imgRequest.SendWebRequest();

                    if (imgRequest.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D tex = DownloadHandlerTexture.GetContent(imgRequest);
                        item.sprite = Sprite.Create(tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f));
                    }
                    else
                    {
                        Debug.LogWarning($"[ContentLoader] No se pudo cargar imagen '{imgUrl}': {imgRequest.error}");
                    }
                }

                Items.Add(item);
            }

            Debug.Log($"[ContentLoader] {Items.Count} ítems cargados correctamente.");
            IsReady = true;
        }
    }
}