using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{
    static readonly Dictionary<string, int> records = new Dictionary<string, int>();
    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }
    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
    }
    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }
    public static void LogRecord(string recordKey, int count)
    {
        if (records.ContainsKey(recordKey))
        {
            records[recordKey] += count;
        }
        else
        {
            records.Add(recordKey, count);
        }
    }

    public static void EmptyLogRecord(string recordKey)
    {
        if (records.ContainsKey(recordKey))
        {
            records.Remove(recordKey);
        }
    }

    public static void PrintRecords()
    {
        var s = "Found " + records.Count + " records:";
        foreach (var record in records.ToList())
        {
            s += "\n" + record.Key + ":  " + record.Value;
        }
        Log(s);
    }
}
