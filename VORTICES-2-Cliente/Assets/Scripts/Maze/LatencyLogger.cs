using UnityEngine;
using Mirror;
using TMPro;
using System.IO;
using System;

namespace Vortices
{
    public class LatencyLogger : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text latencyText;

        [Header("Log")]
        [SerializeField] private float logInterval = 5f;

        private string filePath;
        private float nextLogTime;

        private void Start()
        {
            StartCoroutine(InitWhenConnected());
        }

        private System.Collections.IEnumerator InitWhenConnected()
        {
            yield return new WaitUntil(() => NetworkClient.active);

            string dir = Path.Combine(Application.persistentDataPath, "Results", "Performance");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string basePath = Path.Combine(dir, "Latency");
            filePath = basePath + ".csv";
            int i = 0;
            while (File.Exists(filePath))
                filePath = $"{basePath} ({i++}).csv";

            File.WriteAllText(filePath, "Timestamp;Latencia_ms\n");
            nextLogTime = Time.time + logInterval;
        }

        private void Update()
        {
            if (!NetworkClient.active || string.IsNullOrEmpty(filePath)) return;

            int ms = Mathf.RoundToInt((float)(NetworkTime.rtt * 1000)); // RTT en ms

            if (latencyText != null)
                latencyText.text = $"Ping: {ms} ms";

            if (Time.time >= nextLogTime)
            {
                try { File.AppendAllText(filePath, $"{DateTime.Now:HH:mm:ss};{ms}\n"); }
                catch (Exception e) { Debug.LogWarning("[LatencyLogger] Error escribiendo CSV: " + e.Message); }
                nextLogTime = Time.time + logInterval;
            }
        }
    }
}
