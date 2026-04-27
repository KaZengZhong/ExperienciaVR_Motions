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
            fullPath       = FindPath(startCell, goalCell);
            totalTotemCount = Mathf.Max(1, (gridWidth * gridHeight) / 10);

            SpawnMaze();
            SpawnTotems();

            int stepsPerTotem = fullPath != null ? fullPath.Count / totalTotemCount : 0;
            Debug.Log($"[Maze] {gridWidth}x{gridHeight} generado. " +
                      $"Ruta completa: {fullPath?.Count} celdas. " +
                      $"Tótems: {totalTotemCount}. " +
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

            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    if (!(x == startCell.x && y == startCell.y) && !(x == goalCell.x && y == goalCell.y))
                        candidates.Add(new Vector2Int(x, y));

            Shuffle(candidates);

            int placed = 0;
            foreach (Vector2Int cell in candidates)
            {
                if (placed >= totalTotemCount) break;

                Vector3 pos = new Vector3(
                    cell.x * cellSize + cellSize / 2f,
                    0,
                    cell.y * cellSize + cellSize / 2f
                );

                Vector3 center = new Vector3(gridWidth * cellSize / 2f, 0, gridHeight * cellSize / 2f);
                Quaternion rotation = Quaternion.Euler(0, Quaternion.LookRotation(center - pos).eulerAngles.y, 0);

                GameObject totemObj = Instantiate(totemPrefab, pos, rotation);
                InformationTotem totem = totemObj.GetComponent<InformationTotem>();
                if (totem != null)
                    totem.SetMapGenerator(this);

                placed++;
            }

            Debug.Log($"[Maze] {placed} tótems colocados.");
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

            // Cuántos pasos mostrar: largo del camino completo dividido por el número de tótems
            // Así el tramo visible escala con el tamaño del laberinto y la cantidad de tótems
            int stepsToShow = Mathf.Max(1, fullPath.Count / totalTotemCount);

            // Si el jugador está muy cerca de la meta, mostrar lo que queda
            int endIdx = Mathf.Min(stepsToShow, pathFromPlayer.Count);

            for (int i = 0; i < endIdx; i++)
            {
                Vector2Int cell       = pathFromPlayer[i];
                bool       isGoalCell = (cell == goalCell);

                // Dirección hacia la siguiente celda del camino
                Quaternion markerRotation = Quaternion.identity;
                if (i + 1 < pathFromPlayer.Count)
                {
                    Vector2Int next = pathFromPlayer[i + 1];
                    Vector3 dir = new Vector3(next.x - cell.x, 0f, next.y - cell.y);
                    markerRotation = Quaternion.LookRotation(dir);
                }

                pathMarkers.Add(CreatePathMarker(cell, markerRotation, isGoalCell));
            }

            Debug.Log($"[Maze] Mostrando {endIdx} de {pathFromPlayer.Count} pasos " +
                      $"desde celda {playerCell} (máx. permitido: {stepsToShow}).");
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

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = $"PathMarker_{cell.x}_{cell.y}";
            marker.transform.parent = transform;
            marker.transform.position = worldPos;
            marker.transform.rotation = Quaternion.Euler(90f, direction.eulerAngles.y, 0f);

            float w = isGoalCell ? cellSize * 0.6f : cellSize * 0.35f;
            float l = isGoalCell ? cellSize * 0.6f : cellSize * 0.70f;
            marker.transform.localScale = new Vector3(w, l, 1f);

            Destroy(marker.GetComponent<MeshCollider>());

            Renderer r = marker.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = isGoalCell ? goalColor : pathColor;
                r.material = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows    = false;
            }

            return marker;
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