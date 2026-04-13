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

        // Grid de celdas — true = visitada
        private bool[,] visited;
        // Paredes — horizontal[x,y] = pared entre (x,y) y (x,y+1)
        //         — vertical[x,y]   = pared entre (x,y) y (x+1,y)
        private bool[,] wallsHorizontal; // pared norte de celda (x,y)
        private bool[,] wallsVertical;   // pared este de celda (x,y)

        private Vector2Int startCell;
        private Vector2Int goalCell;

        void Start()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            // Inicializar grids
            visited = new bool[gridWidth, gridHeight];
            wallsHorizontal = new bool[gridWidth, gridHeight + 1];
            wallsVertical = new bool[gridWidth + 1, gridHeight];

            // Todas las paredes activas al inicio
            for (int x = 0; x <= gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    wallsVertical[x, y] = true;

            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y <= gridHeight; y++)
                    wallsHorizontal[x, y] = true;

            // Celda de inicio: esquina (0,0)
            startCell = Vector2Int.zero;
            // Celda meta: esquina opuesta
            goalCell = new Vector2Int(gridWidth - 1, gridHeight - 1);

            // Recursive Backtracker DFS
            RecursiveBacktrack(startCell.x, startCell.y);

            SpawnMaze();

            Debug.Log($"[Maze] Laberinto {gridWidth}x{gridHeight} generado. Inicio: {startCell}, Meta: {goalCell}");

            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
                xrOrigin.transform.position = new Vector3(
                    startCell.x * cellSize + cellSize / 2f,
                    0,
                    startCell.y * cellSize + cellSize / 2f
                );
        }

        private void RecursiveBacktrack(int x, int y)
        {
            visited[x, y] = true;

            // Direcciones aleatorias
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
                    // Derribar pared entre (x,y) y (nx,ny)
                    if (dir == Vector2Int.right) wallsVertical[x + 1, y] = false;
                    if (dir == Vector2Int.left)  wallsVertical[x, y] = false;
                    if (dir == Vector2Int.up)    wallsHorizontal[x, y + 1] = false;
                    if (dir == Vector2Int.down)  wallsHorizontal[x, y] = false;

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

                    // Piso
                    Material fm = floorMaterial;
                    if (x == goalCell.x && y == goalCell.y)
                    {
                        fm = new Material(floorMaterial != null ? floorMaterial : new Material(Shader.Find("Standard")));
                        fm.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                    }
                    SpawnCube(gameObject, $"Floor_{x}_{y}", cellCenter, new Vector3(cellSize, 0.1f, cellSize), fm);

                    // Techo
                    SpawnCube(gameObject, $"Ceiling_{x}_{y}",
                        cellCenter + new Vector3(0, wallHeight, 0),
                        new Vector3(cellSize, 0.1f, cellSize), ceilingMaterial, false);

                    // Pared sur (y=0 de cada celda)
                    if (wallsHorizontal[x, y])
                        SpawnCube(gameObject, $"WallS_{x}_{y}",
                            new Vector3(cellCenter.x, wallHeight / 2f, y * cellSize),
                            new Vector3(cellSize, wallHeight, wallThickness), wallMaterial);

                    // Pared oeste (x=0 de cada celda)
                    if (wallsVertical[x, y])
                        SpawnCube(gameObject, $"WallW_{x}_{y}",
                            new Vector3(x * cellSize, wallHeight / 2f, cellCenter.z),
                            new Vector3(wallThickness, wallHeight, cellSize), wallMaterial);
                }
            }

            // Paredes del borde norte
            for (int x = 0; x < gridWidth; x++)
                SpawnCube(gameObject, $"WallN_{x}",
                    new Vector3(x * cellSize + cellSize / 2f, wallHeight / 2f, gridHeight * cellSize),
                    new Vector3(cellSize, wallHeight, wallThickness), wallMaterial);

            // Paredes del borde este
            for (int y = 0; y < gridHeight; y++)
                SpawnCube(gameObject, $"WallE_{y}",
                    new Vector3(gridWidth * cellSize, wallHeight / 2f, y * cellSize + cellSize / 2f),
                    new Vector3(wallThickness, wallHeight, cellSize), wallMaterial);

            StaticBatchingUtility.Combine(gameObject);
        }

        private void Shuffle(List<Vector2Int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2Int temp = list[i];
                list[i] = list[j];
                list[j] = temp;
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
            if (name.StartsWith("Wall") || name.StartsWith("Ceiling"))
                cube.isStatic = true;
            return cube;
        }
    }
}