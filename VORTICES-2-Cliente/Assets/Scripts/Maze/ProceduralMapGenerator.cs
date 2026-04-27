using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class ProceduralMapGenerator : MonoBehaviour
    {
        [Header("Configuración del laberinto")]
        public int gridWidth = 7;
        public int gridHeight = 7;
        public float cellSize = 4f;
        public float wallHeight = 3f;
        public float wallThickness = 0.2f;

        [Header("Materiales")]
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;

        [Header("Tótems")]
        public GameObject totemPrefab;
        [Tooltip("Altura a la que se monta el tótem en la pared (en metros)")]
        public float totemWallHeight = 1.4f;
        [Tooltip("Separación del tótem respecto a la superficie de la pared")]
        public float totemWallOffset = 0.05f;

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

        void Start()
        {
            GenerateMap();
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

            RecursiveBacktrack(startCell.x, startCell.y);

            // Calcular la ruta completa una sola vez — sirve de referencia para el largo parcial
            // totalTotemCount se establece dentro de SpawnTotems según cuántos se logran colocar
            fullPath        = FindPath(startCell, goalCell);
            totalTotemCount = 1; // valor provisional, se actualiza en SpawnTotems

            SpawnMaze();
            SpawnTotems(); // actualiza totalTotemCount al final

            int stepsPerTotem = (fullPath != null && totalTotemCount > 0)
                ? fullPath.Count / totalTotemCount : 0;
            Debug.Log($"[Maze] {gridWidth}x{gridHeight} generado. " +
                      $"Ruta completa: {fullPath?.Count} celdas. " +
                      $"Tótems en intersecciones: {totalTotemCount}. " +
                      $"Pasos visibles por tótem: {stepsPerTotem}");

            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
                xrOrigin.transform.position = new Vector3(
                    startCell.x * cellSize + cellSize / 2f,
                    0,
                    startCell.y * cellSize + cellSize / 2f
                );
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

                    Material fm = floorMaterial;
                    if (x == goalCell.x && y == goalCell.y)
                    {
                        fm = new Material(floorMaterial != null ? floorMaterial : new Material(Shader.Find("Standard")));
                        fm.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                    }
                    SpawnCube(gameObject, $"Floor_{x}_{y}", cellCenter, new Vector3(cellSize, 0.1f, cellSize), fm);

                    SpawnCube(gameObject, $"Ceiling_{x}_{y}",
                        cellCenter + new Vector3(0, wallHeight, 0),
                        new Vector3(cellSize, 0.1f, cellSize), ceilingMaterial, false);

                    if (wallsHorizontal[x, y])
                    {
                        string key = $"WallS_{x}_{y}";
                        wallObjects[key] = SpawnCube(gameObject, key,
                            new Vector3(cellCenter.x, wallHeight / 2f, y * cellSize),
                            new Vector3(cellSize, wallHeight, wallThickness), wallMaterial);
                    }

                    if (wallsVertical[x, y])
                    {
                        string key = $"WallW_{x}_{y}";
                        wallObjects[key] = SpawnCube(gameObject, key,
                            new Vector3(x * cellSize, wallHeight / 2f, cellCenter.z),
                            new Vector3(wallThickness, wallHeight, cellSize), wallMaterial);
                    }
                }
            }

            for (int x = 0; x < gridWidth; x++)
            {
                string key = $"WallN_{x}";
                wallObjects[key] = SpawnCube(gameObject, key,
                    new Vector3(x * cellSize + cellSize / 2f, wallHeight / 2f, gridHeight * cellSize),
                    new Vector3(cellSize, wallHeight, wallThickness), wallMaterial);
            }

            for (int y = 0; y < gridHeight; y++)
            {
                string key = $"WallE_{y}";
                wallObjects[key] = SpawnCube(gameObject, key,
                    new Vector3(gridWidth * cellSize, wallHeight / 2f, y * cellSize + cellSize / 2f),
                    new Vector3(wallThickness, wallHeight, cellSize), wallMaterial);
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

            int placed = 0;
            foreach (Vector2Int cell in decisionPoints)
            {
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
                    totem.SetMapGenerator(this);

                placed++;
            }

            // Actualizar el contador real de tótems colocados
            // (puede ser menor al estimado si hay pocas intersecciones)
            totalTotemCount = Mathf.Max(1, placed);

            Debug.Log($"[Maze] {placed} tótems colocados en puntos de decisión.");
        }

        // ─── Detección de puntos de decisión ─────────────────────────────────────

        /// <summary>
        /// Devuelve las celdas de la ruta óptima que tienen 3 o más vecinos accesibles.
        /// Estos son los puntos donde el jugador realmente necesita decidir qué dirección tomar.
        /// </summary>
        private List<Vector2Int> FindDecisionPointsOnPath()
        {
            var points = new List<Vector2Int>();
            if (fullPath == null) return points;

            foreach (Vector2Int cell in fullPath)
                if (GetPassableNeighbors(cell).Count >= 3)
                    points.Add(cell);

            return points;
        }

        /// <summary>
        /// Versión de respaldo: todas las intersecciones del laberinto,
        /// no solo las que están en la ruta óptima.
        /// </summary>
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

        // ─── Utilidades ───────────────────────────────────────────────────────────

        private void Shuffle(List<Vector2Int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void Shuffle(List<string> list)
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
}