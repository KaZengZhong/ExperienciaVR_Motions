using NUnit.Framework;
using System.IO;
using UnityEngine;

public class TestMazeMetricsLogger
{
    [Test]
    public void Initialize_CreaArchivoCSV()
    {
        string dir = Path.Combine(Application.persistentDataPath, "Results", "Maze");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, "Maze_Metrics_Test.csv");
        if (File.Exists(filePath)) File.Delete(filePath);

        using (var tw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
        {
            tw.WriteLine("# Laberinto 5x5");
            tw.WriteLine("Timestamp;Fecha;Tipo;Titular;RespuestaJugador;RespuestaCorrecta;Correcto;UsoInvestigar;TiempoRespuesta_s;TiempoTotal_s");
        }

        Assert.IsTrue(File.Exists(filePath));

        string[] lines = File.ReadAllLines(filePath);
        Assert.AreEqual(2, lines.Length);

        File.Delete(filePath);
    }
}
