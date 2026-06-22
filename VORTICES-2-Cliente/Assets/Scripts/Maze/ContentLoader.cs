using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Vortices
{
    /// <summary>
    /// Carga content.json desde una URL externa (GitHub raw u otro host) al iniciar la escena
    /// y pone los ítems disponibles para ProceduralMapGenerator.
    ///
    /// Uso: ContentLoader.Instance.Items
    ///
    /// Coloca este script en un GameObject vacío llamado "ContentLoader" en la escena.
    /// ProceduralMapGenerator esperará a que termine de cargar antes de generar el mapa.
    /// </summary>
    public class ContentLoader : MonoBehaviour
    {
        public static ContentLoader Instance { get; private set; }

        [Header("URLs externas")]
        [Tooltip("URL raw del JSON, ej: https://raw.githubusercontent.com/usuario/repo/main/content.json")]
        public string contentJsonUrl = "https://raw.githubusercontent.com/TU_USUARIO/TU_REPO/main/content.json";

        [Tooltip("URL base para las imágenes, ej: https://raw.githubusercontent.com/usuario/repo/main/images")]
        public string imagesBaseUrl  = "https://raw.githubusercontent.com/TU_USUARIO/TU_REPO/main/images";

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
            jsonRequest.timeout = 10;
            yield return jsonRequest.SendWebRequest();

            if (jsonRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ContentLoader] No se pudo cargar el JSON: {jsonRequest.error}");
                IsReady = true;
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

            // Cargar imagen principal de cada ítem
            foreach (TotemContentItem item in contentList.items)
            {
                if (!string.IsNullOrEmpty(item.imageUrl))
                {
                    string imgUrl = ResolveUrl(item.imageUrl);
                    yield return LoadSprite(imgUrl, sprite => item.sprite = sprite);
                }

                // Resolver URLs de video y audio para que InformationTotem tenga la URL completa
                if (!string.IsNullOrEmpty(item.videoUrl))
                    item.videoUrl = ResolveUrl(item.videoUrl);
                if (!string.IsNullOrEmpty(item.audioUrl))
                    item.audioUrl = ResolveUrl(item.audioUrl);

                Items.Add(item);
            }

            Debug.Log($"[ContentLoader] {Items.Count} ítems cargados correctamente.");
            IsReady = true;
        }

        /// <summary>
        /// Si la URL ya es absoluta la devuelve tal cual.
        /// Si no, la concatena con imagesBaseUrl.
        /// </summary>
        private string ResolveUrl(string url)
        {
            return url.StartsWith("http")
                ? url
                : $"{imagesBaseUrl.TrimEnd('/')}/{url}";
        }

        /// <summary>
        /// Descarga una imagen desde una URL y la convierte en Sprite.
        /// </summary>
        private IEnumerator LoadSprite(string url, System.Action<Sprite> onLoaded)
        {
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                Sprite sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                onLoaded(sprite);
            }
            else
            {
                Debug.LogWarning($"[ContentLoader] No se pudo cargar imagen '{url}': {req.error}");
                onLoaded(null);
            }
        }

        /// <summary>
        /// Al cerrar el juego borra los archivos de video y audio descargados en la carpeta temporal.
        /// </summary>
        void OnApplicationQuit()
        {
            string[] extensions = { "*.mp4", "*.webm", "*.mp3", "*.wav", "*.ogg" };
            foreach (string ext in extensions)
            {
                string[] files = System.IO.Directory.GetFiles(Application.temporaryCachePath, ext);
                foreach (string file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        Debug.Log($"[ContentLoader] Archivo temporal eliminado: {file}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[ContentLoader] No se pudo eliminar '{file}': {e.Message}");
                    }
                }
            }
        }
    }
}