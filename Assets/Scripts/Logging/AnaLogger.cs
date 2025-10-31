using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AnaLogger
{
    private static AnaLogger _defaultInstance;
    private const string DefaultLogFileName = "default_log.log";

    private static AnaLogger DefaultInstance
    {
        get
        {
            if (_defaultInstance == null)
            {
                _defaultInstance = new AnaLogger(DefaultLogFileName, true);
            }
            return _defaultInstance;
        }
    }

    public static void Log(string message)
    {
        DefaultInstance.WriteLog(message);
    }

    public static void Log(Vector3[] message)
    {
        DefaultInstance.WriteLog("Printing array: ");
        for(int i = 0; i < message.Length; i++)
        {
            DefaultInstance.WriteLog($"arr [{i}] = {message[i].ToString()}");
        }
    }

    public static void Log(List<int> message)
    {
        DefaultInstance.WriteLog("Printing List<int>: ");
        for (int i = 0; i < message.Count; i++)
        {
            DefaultInstance.WriteLog($"arr [{i}] = {message[i].ToString()}");
        }
    }

    public string LogFilePath { get; private set; }

    public AnaLogger(string fileName, bool clearFileFirst = true)
    {
        LogFilePath = Path.Join(Application.dataPath, "Logging\\Dump", fileName);

        if (clearFileFirst)
        {
            this.ClearFile();
        }
    }

    public void WriteLog(string message, bool jumpLine = true)
    {
        try
        {
            StreamWriter writer = new StreamWriter(LogFilePath, true);

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (jumpLine)
            {
                writer.WriteLine($"[{timestamp}] {message}");
            }
            else
            {
                writer.Write(message);
            }

            writer.Close();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AnaLogger] FAILED to write to log file. Path: '{LogFilePath}'. Full Error: {e.ToString()}");
        }
    }

    public void ClearFile()
    {
        StreamWriter writer = new StreamWriter(LogFilePath, false);
        writer.WriteLine("FILE WAS CLEARED BY LOGGER");
        writer.Close();
    }
}