using System;
using System.IO;
using UnityEngine;

namespace Vortices
{
    /// <summary>
    /// Registra métricas del laberinto en un CSV separado:
    ///   - Respuesta por tótem (titular, respuesta, corrección, tiempo de respuesta)
    ///   - Recorrido del jugador (cambios de celda y muestreo periódico de posición)
    ///   - Tiempo total de completación del laberinto
    ///
    /// Añade este script a un GameObject en la escena del laberinto.
    /// ProceduralMapGenerator lo inicializa automáticamente al generar el mapa.
    /// </summary>
    public class MazeMetricsLogger : MonoBehaviour
    {
        public static MazeMetricsLogger Instance { get; private set; }

        [Header("Muestreo de posición")]
        [Tooltip("Segundos entre muestras mientras el jugador permanece en la misma celda.")]
        public float intervaloMuestreo = 5f;

        private string filePath;
        private float  mazeStartTime;

        // Seguimiento espacial
        private Transform xrOrigin;
        private float     cellSize    = 1f;
        private int       userId;
        private string    sessionName = "offline";

        private Vector2Int celdaActual = new Vector2Int(int.MinValue, int.MinValue);
        private float      proximoMuestreo;
        private bool       trackingActivo;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Crea el archivo CSV, registra el inicio del laberinto y activa el seguimiento
        /// espacial. Llamado desde ProceduralMapGenerator al generar el mapa.
        /// </summary>
        public void Initialize(int gridW, int gridH, Transform origin, float cell, int user, string session)
        {
            mazeStartTime = Time.time;

            xrOrigin    = origin;
            cellSize    = cell > 0f ? cell : 1f;
            userId      = user;
            sessionName = string.IsNullOrEmpty(session) ? "offline" : session;

            string dir = Path.Combine(Application.persistentDataPath, "Results", "Maze");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string baseFile = Path.Combine(dir, "Maze_Metrics");
            filePath = baseFile + ".csv";
            int copy = 0;
            while (File.Exists(filePath))
                filePath = $"{baseFile} ({copy++}).csv";

            using (TextWriter tw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                tw.WriteLine($"# Laberinto {gridW}x{gridH} — Sesión {sessionName} — Usuario {userId} — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                tw.WriteLine("Timestamp;Fecha;TiempoSesion_s;Sesion;UserId;Tipo;CeldaX;CeldaZ;Titular;RespuestaJugador;RespuestaCorrecta;Correcto;UsoInvestigar;TiempoRespuesta_s;TiempoTotal_s");
            }

            trackingActivo  = xrOrigin != null;
            proximoMuestreo = Time.time;

            if (!trackingActivo)
                Debug.LogWarning("[MazeMetrics] Sin referencia al XR Origin: no se registrará el recorrido.");

            Debug.Log($"[MazeMetrics] Log iniciado: {filePath}");
        }

        void Update()
        {
            if (!trackingActivo) return;

            Vector2Int celda = CeldaDelJugador();

            if (celda != celdaActual)
            {
                celdaActual     = celda;
                proximoMuestreo = Time.time + intervaloMuestreo;
                WriteRow("CambioCelda", celda);
                return;
            }

            if (Time.time >= proximoMuestreo)
            {
                proximoMuestreo = Time.time + intervaloMuestreo;
                WriteRow("Posicion", celda);
            }
        }

        /// <summary>
        /// Convierte la posición del jugador a coordenadas de celda.
        /// Usa el mismo cálculo que InformationTotem.SendNetworkAnswer para que
        /// las celdas del recorrido coincidan con las de los eventos de tótem.
        /// </summary>
        private Vector2Int CeldaDelJugador()
        {
            return new Vector2Int(
                Mathf.FloorToInt(xrOrigin.position.x / cellSize),
                Mathf.FloorToInt(xrOrigin.position.z / cellSize)
            );
        }

        /// <summary>
        /// Registra la respuesta del jugador a un tótem.
        /// </summary>
        /// <param name="headline">Titular de la noticia mostrada.</param>
        /// <param name="playerAnsweredReal">true si el jugador dijo "Real", false si dijo "Falso".</param>
        /// <param name="isReal">Respuesta correcta según el contenido.</param>
        /// <param name="timeToAnswer">Segundos desde que el jugador se acercó hasta que respondió.</param>
        public void LogTotemAnswer(string headline, bool playerAnsweredReal, bool isReal, bool usedInvestigar, float timeToAnswer)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            string playerAnswer  = playerAnsweredReal ? "Real"  : "Falsa";
            string correctAnswer = isReal             ? "Real"  : "Falsa";
            bool   isCorrect     = playerAnsweredReal == isReal;

            Vector2Int celda = trackingActivo ? CeldaDelJugador() : celdaActual;

            WriteRow("TotemRespuesta", celda,
                     titular:          headline,
                     respuestaJugador: playerAnswer,
                     respuestaCorrecta: correctAnswer,
                     correcto:         isCorrect.ToString(),
                     usoInvestigar:    usedInvestigar.ToString(),
                     tiempoRespuesta:  $"{timeToAnswer:F1}");

            Debug.Log($"[MazeMetrics] '{headline}' — {playerAnswer} ({(isCorrect ? "Correcto" : "Incorrecto")}) Investigó:{usedInvestigar} en {timeToAnswer:F1}s");
        }

        /// <summary>
        /// Registra que el jugador completó el laberinto (llegó a la celda meta).
        /// </summary>
        public void LogMazeCompleted()
        {
            if (string.IsNullOrEmpty(filePath)) return;

            float totalTime = Time.time - mazeStartTime;
            Vector2Int celda = trackingActivo ? CeldaDelJugador() : celdaActual;

            WriteRow("LaberintoCompletado", celda, tiempoTotal: $"{totalTime:F1}");

            trackingActivo = false;
            Debug.Log($"[MazeMetrics] Laberinto completado en {totalTime:F1}s");
        }

        /// <summary>
        /// Escribe una fila del CSV. Centraliza el orden de las columnas para que
        /// todos los tipos de evento compartan el mismo formato.
        /// </summary>
        private void WriteRow(string tipo, Vector2Int celda,
                              string titular = "", string respuestaJugador = "",
                              string respuestaCorrecta = "", string correcto = "",
                              string usoInvestigar = "", string tiempoRespuesta = "",
                              string tiempoTotal = "")
        {
            if (string.IsNullOrEmpty(filePath)) return;

            DateTime now = DateTime.Now;
            float    t   = Time.time - mazeStartTime;

            string row = $"{now:HH:mm:ss};{now:yyyy-MM-dd};{t:F1};{Sanitize(sessionName)};{userId};{tipo};" +
                         $"{celda.x};{celda.y};{Sanitize(titular)};{respuestaJugador};{respuestaCorrecta};" +
                         $"{correcto};{usoInvestigar};{tiempoRespuesta};{tiempoTotal}";

            try
            {
                using (TextWriter tw = new StreamWriter(filePath, true, System.Text.Encoding.UTF8))
                    tw.WriteLine(row);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MazeMetrics] No se pudo escribir la fila: {e.Message}");
            }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace(";", ",").Replace("\n", " ").Replace("\r", " ").Trim();
        }
    }
}