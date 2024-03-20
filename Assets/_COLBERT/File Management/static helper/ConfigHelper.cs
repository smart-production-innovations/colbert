using System;
using System.Collections.Generic;
using UnityEngine;

//contains paths that are used for loading data/config files
//and functions for reading config files
public static class ConfigHelper
{
    public const string serverConfigFile = "_config/server.txt";
    public const string proxyConfigFile = "_config/proxy.txt";
    public const string physicsConfigFile = "_config/physics.txt";
    public const string translationConfigFile = "_config/translation.txt";
    public const string metadataConfigFile = "_config/metadata.txt";
    public const string logoFileA = "_config/logo.png";
    public const string logoFileB = "_config/logo.jpg";

    public const string modelDirectory = "_userdata";
    public const string snapshotDirectory = "_userdata/snapshots";
    public const string environmentDirectory = "_userdata/environments";

    public static string[] ReadLines(string file)
    {
        string path = Path(file);
        if (System.IO.File.Exists(path))
        {
            return System.IO.File.ReadAllLines(path);
        }
        else
        {
            return null;
        }
    }

    public static byte[] ReadBytes(string file)
    {
        string path = Path(file);
        if (System.IO.File.Exists(path))
        {
            return System.IO.File.ReadAllBytes(path);
        }
        else
        {
            return null;
        }
    }

    public static Dictionary<string, string> ReadVariables(string file)
    {
        string[] lines = ReadLines(file);
        if (lines == null)
            return null;

        Dictionary<string, string> variables = new Dictionary<string, string>();
        foreach (string line in lines)
        {
            string[] var = line.Split(new[] { '=' }, 2);
            if (var.Length == 2)
                variables.Add(var[0].Trim(), var[1].Trim());
        }
        return variables;
    }

    public static string Path(string file)
    {
        return System.IO.Path.Combine(AbsLocalPath, file);
    }

    public static string AbsLocalPath
    {
        get
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Environment.CurrentDirectory;
#elif UNITY_ANDROID
            return Application.persistentDataPath;
#elif UNITY_WSA
            return Application.persistentDataPath;
#elif UNITY_WEBGL
            return Application.persistentDataPath;
#else
            
#endif
        }
    }
}
