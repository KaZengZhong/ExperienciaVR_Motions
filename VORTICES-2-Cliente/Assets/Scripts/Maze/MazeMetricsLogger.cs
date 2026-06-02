using System;
using System.IO;
using UnityEngine;

namespace Vortices
{
    /// <summary>
    /// Registra métricas del laberinto en un CSV separado:
    ///   - Respuesta por tótem (titular, respuesta, corrección, tiempo de respuesta)
    ///   - Tiempo total de completación del laberinto
    ///
    /// Añade este script a un GameObject en la escena del laberinto.
    /// ProceduralMapGenerator lo inicializa automáticamente al generar el mapa.
    /// </summary>
    public class MazeMetricsLogger : MonoBehaviour
    {
        public static MazeMetricsLogger Instance { get; private set; }

        private string filePath;
        private float  mazeStartTime;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Crea el archivo CSV y registra el inicio del laberinto.
        /// Llamado desde ProceduralMapGenerator al generar el mapa.
        /// </summary>
        public void Initialize(int gridW, int gridH)
        {
            mazeStartTime = Time.time;

            string dir = Path.Combine(Application.dataPath, "Results", "Maze");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string baseFile = Path.Combine(dir, "Maze_Metrics");
            filePath = baseFile + ".csv";
            int copy = 0;
            while (File.Exists(filePath))
                filePath = $"{baseFile} ({copy++}).csv";

            using (TextWriter tw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                tw.WriteLine($"# Laberinto {gridW}x{gridH} — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                tw.WriteLine("Timestamp;Fecha;Tipo;Titular;RespuestaJugador;RespuestaCorrecta;Correcto;UsoInvestigar;TiempoRespuesta_s;TiempoTotal_s");
            }

            Debug.Log($"[MazeMetrics] Log iniciado: {filePath}");
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

            DateTime now = DateTime.Now;
            using (TextWriter tw = new StreamWriter(filePath, true, System.Text.Encoding.UTF8))
                tw.WriteLine($"{now:HH:mm:ss};{now:yyyy-MM-dd};TotemRespuesta;" +
                             $"{headline};{playerAnswer};{correctAnswer};{isCorrect};{usedInvestigar};{timeToAnswer:F1};");

            Debug.Log($"[MazeMetrics] '{headline}' — {playerAnswer} ({(isCorrect ? "Correcto" : "Incorrecto")}) Investigó:{usedInvestigar} en {timeToAnswer:F1}s");
        }

        /// <summary>
        /// Registra que el jugador completó el laberinto (llegó a la celda meta).
        /// </summary>
        public void LogMazeCompleted()
        {
            if (string.IsNullOrEmpty(filePath)) return;

            float totalTime = Time.time - mazeStartTime;
            DateTime now = DateTime.Now;
            using (TextWriter tw = new StreamWriter(filePath, true, System.Text.Encoding.UTF8))
                tw.WriteLine($"{now:HH:mm:ss};{now:yyyy-MM-dd};LaberintoCompletado;;;;;;;{totalTime:F1}");

            Debug.Log($"[MazeMetrics] Laberinto completado en {totalTime:F1}s");
        }
    }
}
