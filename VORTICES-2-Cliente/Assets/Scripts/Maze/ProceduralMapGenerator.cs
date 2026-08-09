using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Vortices
{
    public class ProceduralMapGenerator : MonoBehaviour
    {
        [Header("Configuración del laberinto")]
        public int gridWidth = 10;
        public int gridHeight = 10;
        public float cellSize = 4f;
        public float wallHeight = 3f;
        public float wallThickness = 0.2f;

        [Header("Materiales (Skin por defecto)")]
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;

        [Header("Skins")]
        public SkinDefinition[] skins;

        [Header("Tótems")]
        public GameObject totemPrefab;
        [Tooltip("Altura a la que se monta el tótem en la pared (en metros)")]
        public float totemWallHeight = 1.4f;
        [Tooltip("Separación del tótem respecto a la superficie de la pared")]
        public float totemWallOffset = 0.05f;
        [Tooltip("Máximo de tótems a colocar (0 = sin límite)")]
        public int maxTotems = -1;

        [Header("Debug")]
        [Tooltip("Sobreescribe el skin del parameters.json solo en el editor")]
        public string debugSkin = "";

        [Header("Ruta")]
        [Tooltip("Color de los marcadores de ruta")]
        public Color pathColor = new Color(0.2f, 0.9f, 0.3f, 1f);
        [Tooltip("Color del marcador en la celda meta")]
        public Color goalColor = new Color(1f, 0.3f, 0.3f, 1f);

        // Grid de celdas — true = hay pared
        private bool[,] visited;
        private bool[,] wallsHorizontal;
        private bool[,] wallsVertical;

        private Vector2Int startCell;
        private Vector2Int goalCell;
        public Vector2Int GoalCell => goalCell;

        // Paredes instanciadas
        private Dictionary<string, GameObject> wallObjects = new Dictionary<string, GameObject>();

        // Ruta completa inicio→meta, calculada una sola vez al generar
        // Se usa como referencia para saber cuántos pasos mostrar por tótem
        private List<Vector2Int> fullPath;
        private int totalTotemCount;

        // Marcadores de ruta activos
        private List<GameObject> pathMarkers = new List<GameObject>();

        // Detección de meta
        private Transform  xrOriginTransform;
        private bool       mazeCompleted;
        private float      mazeStartTime;
        private int        playerUserId;
        private string     sessionName = "";

        // Skin activo
        private string   currentSkin        = "Castillo";
        private Material skinWallMaterial;
        private Material skinFloorMaterial;
        private bool     skinNoCeiling;
        private Material skinCeilingMaterial;

        private GameObject loadingCanvas;

        void Start()
        {
            LoadParameters();

            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
                xrOriginObj.transform.position = new Vector3(cellSize / 2f, 0f, cellSize / 2f);

            CreateLoadingIndicator(xrOriginObj);
            StartCoroutine(WaitAndGenerate());
        }

        private void CreateLoadingIndicator(GameObject xrOriginObj)
        {
            loadingCanvas = new GameObject("LoadingCanvas");
            Canvas canvas = loadingCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            loadingCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();

            RectTransform rt = loadingCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 0.5f);

            // Posicionar frente al jugador
            if (xrOriginObj != null)
            {
                loadingCanvas.transform.position = xrOriginObj.transform.position + Vector3.forward * 3f + Vector3.up * 1.6f;
                loadingCanvas.transform.rotation = Quaternion.LookRotation(loadingCanvas.transform.position - xrOriginObj.transform.position);
            }
            else
            {
                loadingCanvas.transform.position = new Vector3(cellSize / 2f, 1.6f, cellSize / 2f + 3f);
            }
            loadingCanvas.transform.localScale = Vector3.one * 0.01f;

            GameObject textObj = new GameObject("LoadingText");
            textObj.transform.SetParent(loadingCanvas.transform, false);
            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "Generando laberinto...";
            tmp.fontSize = 48;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(200f, 50f);
            textRt.anchoredPosition = Vector2.zero;
        }

        void Update()
        {
            if (mazeCompleted || xrOriginTransform == null) return;

            if (xrOriginTransform.position.z >= gridHeight * cellSize - 0.3f)
            {
                mazeCompleted = true;
                MazeMetricsLogger.Instance?.LogMazeCompleted();
                ShowCompletionMessage();
            }
        }

        private void LoadParameters()
        {
            string path = PlatformPaths.ConfigJson;
            if (!File.Exists(path))
            {
                if (!string.IsNullOrEmpty(debugSkin)) currentSkin = debugSkin;
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                MazeParameters p = JsonUtility.FromJson<MazeParameters>(json);
                if (p.gridWidth > 0)   gridWidth   = p.gridWidth;
                if (p.gridHeight > 0)  gridHeight  = p.gridHeight;
                if (p.maxTotems >= -1) maxTotems   = p.maxTotems;
                if (!string.IsNullOrEmpty(p.skinName)) currentSkin = p.skinName;
                if (p.noCeiling) skinNoCeiling = true;
                Debug.Log($"[Maze] Parámetros cargados: {gridWidth}x{gridHeight}, maxTotems={maxTotems}, skin={currentSkin}, noCeiling={skinNoCeiling}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Maze] Error leyendo parameters.json: {e.Message}");
            }

            if (!string.IsNullOrEmpty(debugSkin)) currentSkin = debugSkin;
        }

        [System.Serializable]
        private class MazeParameters
        {
            public int    gridWidth  = 0;
            public int    gridHeight = 0;
            public int    maxTotems  = 0;
            public string skinName   = "";
            public bool   noCeiling  = false;
        }

        // Lee session.json y usa el sessionName como semilla determinista.
        // Ambos clientes con el mismo sessionName generarán el mismo laberinto.
        private void InitMazeSeed()
        {
            string path = PlatformPaths.ConfigJson;
            if (!File.Exists(path)) return;

            try
            {
                MazeSeedData data = JsonUtility.FromJson<MazeSeedData>(File.ReadAllText(path));
                if (!string.IsNullOrEmpty(data.sessionName))
                {
                    int seed = DeterministicHash(data.sessionName);
                    Random.InitState(seed);
                    Debug.Log($"[Maze] Semilla de sesión '{data.sessionName}': {seed}");
                }
                playerUserId = Mathf.Abs(data.userId);
                sessionName  = data.sessionName;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Maze] No se pudo leer semilla de session.json: {e.Message}");
            }
        }

        [System.Serializable]
        private class MazeSeedData { public string sessionName = ""; public int userId = 0; }

        private IEnumerator WaitAndGenerate()
        {
            float elapsed = 0f;
            const float timeout = 15f;

            Debug.Log("[Maze] Esperando ContentLoader...");

            while ((ContentLoader.Instance == null || !ContentLoader.Instance.IsReady) && elapsed < timeout)
            {
                if (elapsed == 0f)
                    Debug.Log($"[Maze] ContentLoader.Instance = {ContentLoader.Instance}");
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (ContentLoader.Instance == null)
                Debug.LogError("[Maze] ContentLoader.Instance es NULL tras esperar — ¿falta el GameObject en la escena?");
            else if (!ContentLoader.Instance.IsReady)
                Debug.LogWarning($"[Maze] Timeout ({timeout}s) — ContentLoader aún no está listo. Generando mapa sin contenido.");
            else
                Debug.Log($"[Maze] ContentLoader listo tras {elapsed:F1}s. Ítems cargados: {ContentLoader.Instance.Items.Count}");

            // Esperar a MazeOnlineConnector para que el joiner tenga los params del servidor
            float netTimeout = 10f;
            while (!Vortices.MazeOnlineConnector.IsReady && netTimeout > 0f)
            {
                netTimeout -= Time.deltaTime;
                yield return null;
            }
            if (!Vortices.MazeOnlineConnector.IsReady)
                Debug.LogWarning("[Maze] MazeOnlineConnector no respondió a tiempo — usando parámetros locales.");

            // Re-leer config.json (puede haber sido actualizado por MazeOnlineConnector)
            LoadParameters();

            try
            {
                GenerateMap();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Maze] Excepción en GenerateMap: {e}");
            }

            if (loadingCanvas != null)
                Destroy(loadingCanvas);

            // Re-aplicar posición y altura de cámara durante 3 frames por si el sistema XR
            // inicializa tarde y sobreescribe la posición seteada en GenerateMap.
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                if (xrOriginTransform != null)
                {
                    Vector3 target = new Vector3(
                        startCell.x * cellSize + cellSize / 2f + playerUserId * 0.4f,
                        0f,
                        startCell.y * cellSize + cellSize / 2f
                    );
                    xrOriginTransform.position = target;

                    Camera cam = Camera.main;
                    if (cam != null && cam.transform.position.y < 0.5f)
                    {
                        Vector3 lp = cam.transform.localPosition;
                        cam.transform.localPosition = new Vector3(lp.x, 1.6f, lp.z);
                    }
                }
            }
        }

        public void GenerateMap()
        {
            visited         = new bool[gridWidth, gridHeight];
            wallsHorizontal = new bool[gridWidth, gridHeight + 1];
            wallsVertical   = new bool[gridWidth + 1, gridHeight];
            wallObjects.Clear();
            ClearPathMarkers();

            for (int x = 0; x <= gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    wallsVertical[x, y] = true;

            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y <= gridHeight; y++)
                    wallsHorizontal[x, y] = true;

            startCell = Vector2Int.zero;
            goalCell  = new Vector2Int(gridWidth - 1, gridHeight - 1);

            InitMazeSeed();
            RecursiveBacktrack(startCell.x, startCell.y);

            // Calcular la ruta completa una sola vez — sirve de referencia para el largo parcial
            // totalTotemCount se establece dentro de SpawnTotems según cuántos se logran colocar
            fullPath        = FindPath(startCell, goalCell);
            totalTotemCount = 1; // valor provisional, se actualiza en SpawnTotems

            ApplySkin();
            SpawnMaze();
            SpawnTotems();
            SpawnGoalMarker();

            int stepsPerTotem = (fullPath != null && totalTotemCount > 0)
                ? fullPath.Count / totalTotemCount : 0;
            Debug.Log($"[Maze] {gridWidth}x{gridHeight} generado. " +
                      $"Ruta completa: {fullPath?.Count} celdas. " +
                      $"Tótems en intersecciones: {totalTotemCount}. " +
                      $"Pasos visibles por tótem: {stepsPerTotem}");

            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
            {
                xrOriginObj.transform.position = new Vector3(
                    startCell.x * cellSize + cellSize / 2f + playerUserId * 0.4f,
                    0,
                    startCell.y * cellSize + cellSize / 2f
                );
                xrOriginTransform = xrOriginObj.transform;

                // Si la cámara queda por debajo de 0.5m (modo desktop sin headset activo),
                // aplicar altura de ojos por defecto para que el jugador vea el laberinto.
                Camera cam = Camera.main;
                if (cam != null && cam.transform.position.y < 0.5f)
                {
                    Vector3 lp = cam.transform.localPosition;
                    cam.transform.localPosition = new Vector3(lp.x, 1.6f, lp.z);
                }
            }

            mazeCompleted = false;
            mazeStartTime = Time.time;
            MazeMetricsLogger.Instance?.Initialize(gridWidth, gridHeight, xrOriginTransform, cellSize, playerUserId, sessionName);
        }

        // ─── Skins ───────────────────────────────────────────────────────────────

        private void ApplySkin()
        {
            // Buscar skin por nombre en el array del Inspector
            SkinDefinition match = null;
            if (skins != null)
                foreach (SkinDefinition s in skins)
                    if (s.skinName == currentSkin) { match = s; break; }

            // Usar materiales del skin encontrado, o los materiales por defecto si no hay coincidencia
            skinWallMaterial    = (match?.wallMaterial    != null ? match.wallMaterial    : wallMaterial);
            skinFloorMaterial   = (match?.floorMaterial   != null ? match.floorMaterial   : floorMaterial);
            skinCeilingMaterial = (match?.ceilingMaterial != null ? match.ceilingMaterial : ceilingMaterial);
            skinNoCeiling       = skinNoCeiling || (match?.noCeiling ?? false);
        }

        // ─── Generación del laberinto ─────────────────────────────────────────────

        private void RecursiveBacktrack(int x, int y)
        {
            visited[x, y] = true;

            List<Vector2Int> directions = new List<Vector2Int>
            {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };
            Shuffle(directions);

            foreach (Vector2Int dir in directions)
            {
                int nx = x + dir.x;
                int ny = y + dir.y;

                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight && !visited[nx, ny])
                {
                    if (dir == Vector2Int.right) wallsVertical[x + 1, y]   = false;
                    if (dir == Vector2Int.left)  wallsVertical[x, y]       = false;
                    if (dir == Vector2Int.up)    wallsHorizontal[x, y + 1] = false;
                    if (dir == Vector2Int.down)  wallsHorizontal[x, y]     = false;

                    RecursiveBacktrack(nx, ny);
                }
            }
        }

        private void SpawnMaze()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 cellCenter = new Vector3(
                        x * cellSize + cellSize / 2f,
                        0,
                        y * cellSize + cellSize / 2f
                    );

                    var floorTile = SpawnCube(gameObject, $"Floor_{x}_{y}", cellCenter, new Vector3(cellSize, 0.1f, cellSize), skinFloorMaterial);
                    if (PlayerPrefs.GetInt("movementMode", 0) == 1)
                        floorTile.AddComponent<UnityEngine.XR.Interaction.Toolkit.TeleportationArea>();

                    if (!skinNoCeiling)
                        SpawnCube(gameObject, $"Ceiling_{x}_{y}",
                            cellCenter + new Vector3(0, wallHeight, 0),
                            new Vector3(cellSize, 0.1f, cellSize), skinCeilingMaterial, false);

                    if (wallsHorizontal[x, y])
                    {
                        string key = $"WallS_{x}_{y}";
                        wallObjects[key] = SpawnCube(gameObject, key,
                            new Vector3(cellCenter.x, wallHeight / 2f, y * cellSize),
                            new Vector3(cellSize, wallHeight, wallThickness), skinWallMaterial);
                    }

                    if (wallsVertical[x, y])
                    {
                        string key = $"WallW_{x}_{y}";
                        wallObjects[key] = SpawnCube(gameObject, key,
                            new Vector3(x * cellSize, wallHeight / 2f, cellCenter.z),
                            new Vector3(wallThickness, wallHeight, cellSize), skinWallMaterial);
                    }
                }
            }

            for (int x = 0; x < gridWidth; x++)
            {
                string key = $"WallN_{x}";
                wallObjects[key] = SpawnCube(gameObject, key,
                    new Vector3(x * cellSize + cellSize / 2f, wallHeight / 2f, gridHeight * cellSize),
                    new Vector3(cellSize, wallHeight, wallThickness), skinWallMaterial);
            }

            for (int y = 0; y < gridHeight; y++)
            {
                string key = $"WallE_{y}";
                wallObjects[key] = SpawnCube(gameObject, key,
                    new Vector3(gridWidth * cellSize, wallHeight / 2f, y * cellSize + cellSize / 2f),
                    new Vector3(wallThickness, wallHeight, cellSize), skinWallMaterial);
            }
        }

        private void SpawnTotems()
        {
            if (totemPrefab == null)
            {
                Debug.LogWarning("[Maze] No hay prefab de tótem asignado.");
                return;
            }

            // Buscar puntos de decisión: cualquier celda del laberinto con 3+ vecinos accesibles
            // No es necesario que estén en la ruta óptima
            List<Vector2Int> decisionPoints = FindAllDecisionPoints();

            // Excluir inicio y meta
            decisionPoints.RemoveAll(c => c == startCell || c == goalCell);
            Shuffle(decisionPoints);

            if (maxTotems == 0) { totalTotemCount = 0; return; }

            int placed = 0;
            for (int i = 0; i < decisionPoints.Count; i++)
            {
                if (maxTotems > 0 && placed >= maxTotems) break;
                Vector2Int cell = decisionPoints[i];
                // Buscar una cara de pared disponible en esta celda para montar el tótem
                WallFace? face = FindAdjacentWallFace(cell);
                if (face == null) continue;

                // Posición: superficie de la pared de la intersección
                // El prefab tiene el pivot en el punto de montaje (bracket de la pared),
                // así que lo colocamos EN la pared y el cuerpo se extiende hacia el interior de la celda.
                // totemWallOffset separa el pivot de la geometría de la pared para evitar z-fighting.
                Vector3 totemPos = new Vector3(
                    face.Value.position.x + face.Value.inwardNormal.x * totemWallOffset,
                    totemWallHeight,
                    face.Value.position.z + face.Value.inwardNormal.z * totemWallOffset
                );

                // Rota para que la cara del tótem mire hacia el interior de la celda
                // (usando la normal de la pared más cercana como referencia de orientación)
                Quaternion totemRot = Quaternion.LookRotation(face.Value.inwardNormal, Vector3.up);

                // Instanciar primero en el origen para calcular bounds sin rotación
                GameObject totemObj = Instantiate(totemPrefab, totemPos, totemRot);
                totemObj.name = $"Totem_{cell.x}_{cell.y}";

                // Compensar el desfase interno del prefab:
                // el mesh 3D puede no estar centrado en el pivot del prefab.
                // Calculamos el centro real del visual (XZ) y corregimos la posición.
                Renderer[] totemRenderers = totemObj.GetComponentsInChildren<Renderer>();
                bool hasNonCanvasRenderer = false;
                Bounds totemBounds = new Bounds(totemPos, Vector3.zero);
                foreach (Renderer r in totemRenderers)
                {
                    // Ignorar renderers de Canvas (UI) — solo nos importa el mesh 3D
                    if (r.GetComponent<UnityEngine.UI.Graphic>() != null) continue;
                    if (r.GetComponentInParent<Canvas>() != null) continue;
                    if (!hasNonCanvasRenderer) { totemBounds = r.bounds; hasNonCanvasRenderer = true; }
                    else totemBounds.Encapsulate(r.bounds);
                }
                if (hasNonCanvasRenderer)
                {
                    // Mover el tótem para que el centro XZ del mesh quede en totemPos
                    Vector3 correction = totemBounds.center - totemPos;
                    correction.y = 0f; // solo corregir en el plano horizontal
                    totemObj.transform.position -= correction;
                }

                InformationTotem totem = totemObj.GetComponent<InformationTotem>();
                if (totem != null)
                {
                    totem.SetMapGenerator(this);

                    // Asignar contenido desde ContentLoader (cicla si hay más tótems que ítems)
                    var items = ContentLoader.Instance?.Items;
                    if (items != null && items.Count > 0)
                        totem.SetContent(items[placed % items.Count]);
                }

                placed++;
            }

            // Actualizar el contador real de tótems colocados
            // (puede ser menor al estimado si hay pocas intersecciones)
            totalTotemCount = Mathf.Max(1, placed);

            Debug.Log($"[Maze] {placed} tótems colocados en puntos de decisión.");
        }

        // ─── Detección de puntos de decisión ─────────────────────────────────────

        private List<Vector2Int> FindAllDecisionPoints()
        {
            var points = new List<Vector2Int>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var cell = new Vector2Int(x, y);
                    int count = GetPassableNeighbors(cell).Count;
                    if (count >= 3)
                    {
                        points.Add(cell);
                    }
                }
            }
            return points;
        }

        // ─── Montaje en pared ─────────────────────────────────────────────────────

        /// <summary>
        /// Cara de pared: posición en el borde de la celda + normal apuntando hacia el interior.
        /// </summary>
        private struct WallFace
        {
            public Vector3 position;      // centro del borde de la celda (en suelo, y=0)
            public Vector3 inwardNormal;  // dirección que apunta hacia el centro de la celda
        }

        /// <summary>
        /// Busca una cara de pared disponible adyacente a <paramref name="cell"/>.
        /// Prefiere las paredes internas del laberinto (más naturales visualmente)
        /// antes que los bordes exteriores.
        /// </summary>
        private WallFace? FindAdjacentWallFace(Vector2Int cell)
        {
            int x = cell.x, y = cell.y;
            float cx = x * cellSize + cellSize / 2f;
            float cz = y * cellSize + cellSize / 2f;

            // Candidatos: (¿hay pared?, posición en el borde, normal hacia adentro)
            var candidates = new (bool hasWall, Vector3 pos, Vector3 normal)[]
            {
                // Sur
                (wallsHorizontal[x, y],     new Vector3(cx, 0f, y * cellSize),           Vector3.forward),
                // Norte
                (wallsHorizontal[x, y + 1], new Vector3(cx, 0f, (y + 1) * cellSize),     Vector3.back),
                // Oeste
                (wallsVertical[x, y],       new Vector3(x * cellSize, 0f, cz),            Vector3.right),
                // Este
                (wallsVertical[x + 1, y],   new Vector3((x + 1) * cellSize, 0f, cz),     Vector3.left),
            };

            // Primero buscar paredes internas (no de borde del laberinto)
            foreach (var c in candidates)
            {
                if (!c.hasWall) continue;
                bool isBorder = (c.normal == Vector3.forward  && y == 0)           ||
                                (c.normal == Vector3.back     && y == gridHeight-1) ||
                                (c.normal == Vector3.right    && x == 0)            ||
                                (c.normal == Vector3.left     && x == gridWidth-1);
                if (!isBorder)
                    return new WallFace { position = c.pos, inwardNormal = c.normal };
            }

            // Si solo hay bordes, usar cualquier pared disponible
            foreach (var c in candidates)
                if (c.hasWall)
                    return new WallFace { position = c.pos, inwardNormal = c.normal };

            return null;
        }

        // ─── Ruta parcial desde la posición del jugador ───────────────────────────

        /// <summary>
        /// Calcula la ruta desde <paramref name="playerCell"/> hasta la meta
        /// y muestra solo los primeros (fullPath.Count / totalTotemCount) pasos.
        /// Reemplaza cualquier ruta visible anterior.
        /// </summary>
        public void ShowPartialPathFrom(Vector2Int playerCell)
        {
            ClearPathMarkers();

            if (fullPath == null || fullPath.Count == 0)
            {
                Debug.LogWarning("[Maze] No hay ruta de referencia calculada.");
                return;
            }

            // Ruta desde donde está el jugador ahora
            List<Vector2Int> pathFromPlayer = FindPath(playerCell, goalCell);
            if (pathFromPlayer == null || pathFromPlayer.Count == 0)
            {
                Debug.LogWarning("[Maze] No se encontró ruta desde la posición del jugador.");
                return;
            }

            // Mostrar solo la dirección inmediata: la siguiente celda en el camino
            // El jugador ve hacia dónde tiene que ir desde donde está, nada más
            if (pathFromPlayer.Count < 2)
            {
                // El jugador ya está en la meta o adyacente — no hay más que indicar
                Debug.Log("[Maze] El jugador ya está en la meta o muy cerca.");
                return;
            }

            // pathFromPlayer[0] = celda actual del jugador
            // pathFromPlayer[1] = siguiente celda a la que debe ir
            Vector2Int currentCell = pathFromPlayer[0];
            Vector2Int nextCell    = pathFromPlayer[1];
            bool isGoalNext        = (nextCell == goalCell);

            Vector3 direction = new Vector3(nextCell.x - currentCell.x, 0f, nextCell.y - currentCell.y);
            Quaternion arrowRotation = Quaternion.LookRotation(direction);

            // Colocar el marcador en la celda actual apuntando hacia la siguiente
            pathMarkers.Add(CreatePathMarker(currentCell, arrowRotation, isGoalNext));

            Debug.Log($"[Maze] Dirección indicada: desde {currentCell} hacia {nextCell}" +
                      (isGoalNext ? " (¡es la meta!)" : ""));
        }

        /// <summary>
        /// Elimina todos los marcadores de ruta activos.
        /// </summary>
        public void ClearPathMarkers()
        {
            foreach (GameObject marker in pathMarkers)
                if (marker != null) Destroy(marker);
            pathMarkers.Clear();
        }

        // ─── Pathfinding (BFS) ────────────────────────────────────────────────────

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            var queue    = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(start);
            cameFrom[start] = start;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == end) break;

                foreach (Vector2Int neighbor in GetPassableNeighbors(current))
                {
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (!cameFrom.ContainsKey(end))
                return null;

            var path = new List<Vector2Int>();
            Vector2Int node = end;
            while (node != start)
            {
                path.Add(node);
                node = cameFrom[node];
            }
            path.Add(start);
            path.Reverse();
            return path;
        }

        private List<Vector2Int> GetPassableNeighbors(Vector2Int cell)
        {
            var neighbors = new List<Vector2Int>();
            int x = cell.x, y = cell.y;

            if (x + 1 < gridWidth  && !wallsVertical[x + 1, y])   neighbors.Add(new Vector2Int(x + 1, y));
            if (x > 0              && !wallsVertical[x, y])         neighbors.Add(new Vector2Int(x - 1, y));
            if (y + 1 < gridHeight && !wallsHorizontal[x, y + 1])  neighbors.Add(new Vector2Int(x, y + 1));
            if (y > 0              && !wallsHorizontal[x, y])       neighbors.Add(new Vector2Int(x, y - 1));

            return neighbors;
        }

        // ─── Creación de marcadores ───────────────────────────────────────────────

        private GameObject CreatePathMarker(Vector2Int cell, Quaternion direction, bool isGoalCell)
        {
            Vector3 worldPos = new Vector3(
                cell.x * cellSize + cellSize / 2f,
                0.06f,
                cell.y * cellSize + cellSize / 2f
            );

            Color color = isGoalCell ? goalColor : pathColor;
            float yRot  = direction.eulerAngles.y;
            float s     = cellSize;

            // Contenedor vacío — la posición es correcta, la rotación la manejan los hijos
            GameObject arrow = new GameObject($"PathMarker_{cell.x}_{cell.y}");
            arrow.transform.parent   = transform;
            arrow.transform.position = worldPos;

            // Rotación de referencia para desplazar las alas en el plano XZ
            Quaternion yQ = Quaternion.Euler(0f, yRot, 0f);

            // ── Tronco ──────────────────────────────────────────────────────
            // Rectángulo angosto que apunta en la dirección de avance
            AddFlatQuad(arrow.transform, "Stem",
                localPos: Vector3.zero,
                scale:    new Vector3(s * 0.15f, s * 0.50f, 1f),
                worldRot: Quaternion.Euler(90f, yRot, 0f),
                color:    color);

            // ── Punta de flecha (dos alas en "V") ───────────────────────────
            // Cada ala va desde la punta delantera hacia la cola lateral-trasera.
            // Ala izquierda: yRot-135° (atrás-izquierda visto desde la punta)
            AddFlatQuad(arrow.transform, "WingL",
                localPos: yQ * new Vector3(-s * 0.10f, 0f,  s * 0.10f),
                scale:    new Vector3(s * 0.10f, s * 0.32f, 1f),
                worldRot: Quaternion.Euler(90f, yRot - 135f, 0f),
                color:    color);

            // Ala derecha: yRot+135° (atrás-derecha visto desde la punta)
            AddFlatQuad(arrow.transform, "WingR",
                localPos: yQ * new Vector3( s * 0.10f, 0f,  s * 0.10f),
                scale:    new Vector3(s * 0.10f, s * 0.32f, 1f),
                worldRot: Quaternion.Euler(90f, yRot + 135f, 0f),
                color:    color);

            return arrow;
        }

        private void AddFlatQuad(Transform parent, string name, Vector3 localPos, Vector3 scale, Quaternion worldRot, Color color)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.parent      = parent;
            q.transform.localPosition = localPos;
            q.transform.localScale  = scale;
            q.transform.rotation    = worldRot;   // rotación en espacio mundo
            Destroy(q.GetComponent<MeshCollider>());
            Renderer r = q.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                r.material = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows    = false;
            }
        }

        // ─── Mensaje de completación ──────────────────────────────────────────────

        private void ShowCompletionMessage()
        {
            float elapsed = Time.time - mazeStartTime;
            int   minutes = (int)(elapsed / 60);
            int   seconds = (int)(elapsed % 60);

            float northZ     = gridHeight * cellSize;
            float gapCenterX = goalCell.x * cellSize + cellSize / 2f;

            GameObject canvas = new GameObject("CompletionCanvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 70f);

            // Afuera del laberinto, 3m más allá de la pared norte, mirando hacia el jugador
            canvas.transform.position = new Vector3(gapCenterX, 1.6f, northZ + 3f);
            canvas.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            canvas.transform.localScale = Vector3.one * 0.01f;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(canvas.transform, false);
            var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = $"¡Llegaste al final!\n{minutes:00}:{seconds:00}";
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 6;
            tmp.fontSizeMax = 20;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(190f, 60f);
            textRt.anchoredPosition = Vector2.zero;
        }

        // ─── Marcador de meta ─────────────────────────────────────────────────────

        private void SpawnGoalMarker()
        {
            float gapWidth    = cellSize * 0.65f;
            float segWidth    = (cellSize - gapWidth) / 2f;
            float northZ      = gridHeight * cellSize;
            float goalOriginX = goalCell.x * cellSize;
            float gapCenterX  = goalOriginX + cellSize / 2f;

            // Eliminar la pared norte completa del goal y dejar un hueco central
            string northKey = $"WallN_{goalCell.x}";
            if (wallObjects.ContainsKey(northKey) && wallObjects[northKey] != null)
            {
                Destroy(wallObjects[northKey]);
                wallObjects.Remove(northKey);
            }

            // Segmentos laterales de pared (material normal del laberinto)
            SpawnCube(gameObject, "GoalWallL",
                new Vector3(goalOriginX + segWidth / 2f, wallHeight / 2f, northZ),
                new Vector3(segWidth, wallHeight, wallThickness), skinWallMaterial);

            SpawnCube(gameObject, "GoalWallR",
                new Vector3(goalOriginX + cellSize - segWidth / 2f, wallHeight / 2f, northZ),
                new Vector3(segWidth, wallHeight, wallThickness), skinWallMaterial);

            // Marco de puerta — color neutro apagado que combina con el laberinto
            Material frameMat = new Material(Shader.Find("Standard"));
            frameMat.color = new Color(0.62f, 0.58f, 0.52f);

            float jambW = 0.3f;
            float lintH = 0.3f;
            float depth = wallThickness * 2.5f;

            // Pared invisible 0.6m más allá — el jugador cruza la puerta pero no cae al vacío
            GameObject stopWall = new GameObject("DoorStopWall");
            stopWall.transform.parent = transform;
            stopWall.transform.position = new Vector3(gapCenterX, wallHeight / 2f, northZ + 0.6f);
            BoxCollider stopCol = stopWall.AddComponent<BoxCollider>();
            stopCol.size = new Vector3(gapWidth, wallHeight, 0.1f);

            // Jamba izquierda
            SpawnCube(gameObject, "DoorJambaL",
                new Vector3(gapCenterX - gapWidth / 2f, wallHeight / 2f, northZ),
                new Vector3(jambW, wallHeight, depth), frameMat, false);

            // Jamba derecha
            SpawnCube(gameObject, "DoorJambaR",
                new Vector3(gapCenterX + gapWidth / 2f, wallHeight / 2f, northZ),
                new Vector3(jambW, wallHeight, depth), frameMat, false);

            // Dintel
            SpawnCube(gameObject, "DoorLintel",
                new Vector3(gapCenterX, wallHeight - lintH / 2f, northZ),
                new Vector3(gapWidth + jambW, lintH, depth), frameMat, false);

        }

        // ─── Utilidades ───────────────────────────────────────────────────────────

        public static int DeterministicHash(string s)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in s)
                    hash = hash * 31 + c;
                return Mathf.Abs(hash);
            }
        }

        private void Shuffle(List<Vector2Int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private GameObject SpawnCube(GameObject parent, string name, Vector3 worldPos, Vector3 scale, Material mat, bool hasCollider = true)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.parent = parent.transform;
            cube.transform.position = worldPos;
            cube.transform.localScale = scale;
            if (mat != null)
                cube.GetComponent<Renderer>().material = mat;
            if (!hasCollider)
                Destroy(cube.GetComponent<BoxCollider>());
            if (name.StartsWith("Ceiling"))
                cube.isStatic = true;
            return cube;
        }
    }

    [System.Serializable]
    public class SkinDefinition
    {
        public string   skinName;
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;
        public bool     noCeiling;
    }
}