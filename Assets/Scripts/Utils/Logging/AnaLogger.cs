using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class AnaLogger :  IDisposable
{
    private static AnaLogger _defaultInstance;
    private static readonly object _instanceLock = new object();
    
    private const string DefaultLogFileName = "default_log.log";
    public string LogFilePath { get; private set; }

    private readonly StreamWriter _writer;
    private readonly object _writeLock = new object();

    private static readonly ThreadLocal<StringBuilder> _batchBuilder = new ThreadLocal<StringBuilder>();
    // This is a private, empty struct just to make the 'using' pattern work.
    private struct BatchToken : IDisposable
    {
        // This is called by the 'using' block when it exits.
        public void Dispose()
        {
            AnaLogger.EndBatch();
        }
    }
    
    // A thread-safe queue that all threads can write to
    private static readonly ConcurrentQueue<string> _globalBuffer = new ConcurrentQueue<string>();

    // A switch to turn buffering on/off.
    private static volatile bool _isBuffering = false;

    private static AnaLogger DefaultInstance
    {
        get
        {
            if(_defaultInstance == null)
            {
                lock (_instanceLock)
                {
                    if (_defaultInstance == null)
                    {
                        _defaultInstance = new AnaLogger(DefaultLogFileName, true);

                        // Subscribe the new instance to the quit event
                        Application.quitting += _defaultInstance.Dispose;
                    }
                }
            }
            return _defaultInstance;
        }
    }

    public AnaLogger(string fileName, bool clearFileFirst = true)
    {
        try
        {
            LogFilePath = Path.Join(Application.dataPath, "Logging\\Dump", fileName);

            FileMode mode = clearFileFirst ? FileMode.Create : FileMode.Append;
            FileStream fileStream = new(LogFilePath, mode, FileAccess.Write, FileShare.Read);

            _writer = new StreamWriter(fileStream);
            _writer.AutoFlush = true;

            if (clearFileFirst)
            {
                _writer.WriteLine("FILE WAS CLEARED BY LOGGER");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnaLogger] FAILED TO INITIALIZE. Path: '{LogFilePath}'. Full Error: {e.ToString()}");
        }
    }

    public void Dispose()
    {
        lock (_writeLock)
        {
            try
            {
                _writer?.WriteLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] --- Application Quitting. Closing Log. ---");
                _writer?.Close();
                _writer?.Dispose();
            }
            catch (Exception)
            {
                // Can't do much here, so quit anyway
            }
        }

        // Unsubscribe to prevent memory 
        Application.quitting -= this.Dispose;

        lock (_instanceLock)
        {
            _defaultInstance = null;
        }
    }

    #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (_defaultInstance != null)
            {
                _defaultInstance.Dispose();
            }
        }
    #endif

    public static IDisposable BeginBatch()
    {
        // Start a new batch for this thread
        _batchBuilder.Value = new StringBuilder();
        _batchBuilder.Value.AppendLine(makeLogViableString("Beginning to present batch log:"));
        return new BatchToken();
    }

    private static void EndBatch()
    {
        var builder = _batchBuilder.Value;
        _batchBuilder.Value = null; // Exit batch mode for this thread

        if (builder != null && builder.Length > 0)
        {
            builder.AppendLine(makeLogViableString("Batch log finished!"));
            
            // The batch is complete.
            // Write the entire built string to the file at once.
            DefaultInstance.WriteRaw(builder.ToString());
        }
    }

    public static void StartBuffering()
    {
        _isBuffering = true;
    }

    public static void StopBufferingAndDump()
    {
        // Turn off buffering switch
        _isBuffering = false;

        // Build the batch string from the queue.
        // This is fast and should happen *before* we take the lock.
        StringBuilder batch = new StringBuilder();
        while (_globalBuffer.TryDequeue(out string message))
        {
            batch.Append(message);
        }

        // If we have anything, write it to the log file
        if (batch.Length > 0)
        {
            DefaultInstance.WriteRaw(batch.ToString());
        }
    }

    public static string makeLogViableString(string msg, bool newline = true)
    {
        string result = $"[{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {msg}";

        if (newline)
        {
            result += Environment.NewLine;
        }
        return result;
    }

    public static void Log(string message, bool newline = true)
    {
        // Format the message
        string finalMessage = makeLogViableString(message ?? "null", newline);

        // Check for global buffering
        if (_isBuffering)
        {
            // We are in global buffer mode.
            // Add to the queue. This is thread-safe and lock-free.
            _globalBuffer.Enqueue(finalMessage);
            
            return; 
        }

        // TODO: May remove
        // OLD: Check for THREAD-LOCAL batching
        var builder = _batchBuilder.Value;
        if (builder != null)
        {
            // We are in a thread-local batch. Append to builder.
            builder.Append(finalMessage);
            
            return;
        }

        // IMMEDIATE MODE
        // No buffers, no batches. Write to file now.
        DefaultInstance.WriteRaw(finalMessage);
    }

    public static void Log(Vector3[] message)
    {
        Log("--- Printing Vector3[] ---");
        for (int i = 0; i < message.Length; i++)
        {
            Log($"arr [{i}] = {message[i].ToString()}");
        }
        Log("--- End of Vector3[] ---");
    }

    public static void Log(List<int> message)
    {
        Log("--- Printing List<int> ---");
        for (int i = 0; i < message.Count; i++)
        {
            Log($"arr [{i}] = {message[i].ToString()}");
        }
        Log("--- End of List ---");
    }

    private void WriteRaw(string rawMessage)
    {
        if (_writer == null)
        {
            Debug.LogError($"[AnaLogger] Logger not initialized. Message lost.");
            return;
        }

        // This is the only lock in the hot-path.
        lock (_writeLock)
        {
            try
            {
                _writer.Write(rawMessage);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnaLogger] FAILED to write to log file. Error: {e.ToString()}");
            }
        }
    }
}