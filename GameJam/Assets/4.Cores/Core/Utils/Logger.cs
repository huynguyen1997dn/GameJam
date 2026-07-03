using UnityEngine;

namespace Utils
{
    public static class Logger
    {
        public static void Info(string message)
        {
            Debug.Log($"[INFO] {message}");
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"[WARNING] {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"[ERROR] {message}");
        }
    }
}