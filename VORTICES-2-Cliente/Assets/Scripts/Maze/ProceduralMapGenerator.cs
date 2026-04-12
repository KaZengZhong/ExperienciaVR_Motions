using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vortices
{
    public class ProceduralMapGenerator : MonoBehaviour
    {
        [Header("Configuración del mapa")]
        public int minRooms = 5;
        public int maxRooms = 10;
        public float roomSize = 10f;
        public float wallHeight = 3f;

        [Header("Materiales")]
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;

        // Datos generados
        private List<Vector2Int> roomPositions = new List<Vector2Int>();
        private List<(Vector2Int, Vector2Int)> corridors = new List<(Vector2Int, Vector2Int)>();

        void Start()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            roomPositions.Clear();
            corridors.Clear();

            int roomCount = Random.Range(minRooms, maxRooms + 1);

            roomPositions.Add(Vector2Int.zero);

            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            int attempts = 0;
            while (roomPositions.Count < roomCount && attempts < 1000)
            {
                attempts++;
                Vector2Int existingRoom = roomPositions[Random.Range(0, roomPositions.Count)];
                Vector2Int direction = directions[Random.Range(0, directions.Length)];
                Vector2Int newRoom = existingRoom + direction;

                if (!roomPositions.Contains(newRoom))
                {
                    roomPositions.Add(newRoom);
                    corridors.Add((existingRoom, newRoom));
                }
            }
            // Posicionar jugador en el centro de la sala inicial
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = new Vector3(0, 1.6f, 0);
            }

            SpawnMap();
            Debug.Log($"[Maze] Mapa generado con {roomPositions.Count} salas y {corridors.Count} pasillos.");
        }

        private void SpawnMap()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            foreach (Vector2Int pos in roomPositions)
            {
                Vector3 worldPos = new Vector3(pos.x * roomSize, 0, pos.y * roomSize);
                SpawnRoom(pos, worldPos);
            }
        }

        private void SpawnRoom(Vector2Int gridPos, Vector3 worldPos)
        {
            GameObject room = new GameObject($"Room_{gridPos.x}_{gridPos.y}");
            room.transform.parent = transform;
            room.transform.position = worldPos;

            float w = roomSize;
            float h = wallHeight;
            float half = w / 2f;

            // Piso
            SpawnCube(room, "Floor", new Vector3(0, 0, 0), new Vector3(w, 0.1f, w), floorMaterial);
            // Techo
            SpawnCube(room, "Ceiling", new Vector3(0, h, 0), new Vector3(w, 0.1f, w), ceilingMaterial);

            // Paredes — solo donde NO hay pasillo
            bool openNorth = corridors.Contains((gridPos, gridPos + Vector2Int.up)) ||
                             corridors.Contains((gridPos + Vector2Int.up, gridPos));
            bool openSouth = corridors.Contains((gridPos, gridPos + Vector2Int.down)) ||
                             corridors.Contains((gridPos + Vector2Int.down, gridPos));
            bool openEast  = corridors.Contains((gridPos, gridPos + Vector2Int.right)) ||
                             corridors.Contains((gridPos + Vector2Int.right, gridPos));
            bool openWest  = corridors.Contains((gridPos, gridPos + Vector2Int.left)) ||
                             corridors.Contains((gridPos + Vector2Int.left, gridPos));

            if (!openNorth)
                SpawnCube(room, "Wall_N", new Vector3(0, h/2f, half), new Vector3(w, h, 0.2f), wallMaterial);
            else
                SpawnWallWithOpening(room, "Wall_N", new Vector3(0, h/2f, half), true, w, h, half);

            if (!openSouth)
                SpawnCube(room, "Wall_S", new Vector3(0, h/2f, -half), new Vector3(w, h, 0.2f), wallMaterial);
            else
                SpawnWallWithOpening(room, "Wall_S", new Vector3(0, h/2f, -half), true, w, h, half);

            if (!openEast)
                SpawnCube(room, "Wall_E", new Vector3(half, h/2f, 0), new Vector3(0.2f, h, w), wallMaterial);
            else
                SpawnWallWithOpening(room, "Wall_E", new Vector3(half, h/2f, 0), false, w, h, half);

            if (!openWest)
                SpawnCube(room, "Wall_W", new Vector3(-half, h/2f, 0), new Vector3(0.2f, h, w), wallMaterial);
            else
                SpawnWallWithOpening(room, "Wall_W", new Vector3(-half, h/2f, 0), false, w, h, half);
        }

        private void SpawnWallWithOpening(GameObject room, string name, Vector3 center, bool isNorthSouth, float w, float h, float half)
        {
            // Apertura de 2 unidades de ancho y altura completa
            float openingWidth = 2f;
            float sideWidth = (w - openingWidth) / 2f;

            if (isNorthSouth)
            {
                // Dos segmentos a los lados de la apertura
                SpawnCube(room, name + "_L", center + new Vector3(-half + sideWidth/2f, 0, 0),
                    new Vector3(sideWidth, h, 0.2f), wallMaterial);
                SpawnCube(room, name + "_R", center + new Vector3(half - sideWidth/2f, 0, 0),
                    new Vector3(sideWidth, h, 0.2f), wallMaterial);
            }
            else
            {
                SpawnCube(room, name + "_L", center + new Vector3(0, 0, -half + sideWidth/2f),
                    new Vector3(0.2f, h, sideWidth), wallMaterial);
                SpawnCube(room, name + "_R", center + new Vector3(0, 0, half - sideWidth/2f),
                    new Vector3(0.2f, h, sideWidth), wallMaterial);
            }
        }

        private void SpawnCube(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.parent = parent.transform;
            cube.transform.localPosition = localPos;
            cube.transform.localScale = scale;

            if (mat != null)
                cube.GetComponent<Renderer>().material = mat;
        }
    }
}