using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

//files from local file system, only from folder 'directory'
public class LocalFileSource : FileSource
{
    private string directory => ConfigHelper.modelDirectory;

    public override async Task<bool> FileExist(string path, string fileName)
    {
        string absolutePath = Path.Combine(GetAbsolutePath(path, false), fileName);
        return await Task.Run(() =>
        {
            return File.Exists(absolutePath);
        });
    }

    public override async Task<bool> PathExist(string path)
    {
        string absolutePath = GetAbsolutePath(path, false);
        return await Task.Run(() =>
        {
            return Directory.Exists(absolutePath);
        });
    }

    public override async Task<List<string>> ListFiles(string path)
    {
        string absolutePath = GetAbsolutePath(path, false);
        return await Task.Run(() =>
        {
            if (!Directory.Exists(absolutePath))
            {
                Debug.LogWarning($"could not list files in directory '{absolutePath}'; directory does not exist");
                return null;
            }
            string[] filePaths = Directory.GetFiles(absolutePath);
            List<string> files = new List<string>();
            for (int i = 0; i < filePaths.Length; i++)
            {
                //Debug.Log("File: " + filePaths[i] + " is in List - Local");
                files.Add(Path.GetFileName(filePaths[i]));
                //fileList.Add(file.Replace(this.AbsPath, "").TrimStart('/', '\\'));
            }
            return files;
        });
    }

    public override async Task<byte[]> GetFile(string path, string fileName)
    {
        string absolutePath = GetAbsolutePath(path, false);
        return await Task.Run(() =>
        {
            absolutePath = Path.Combine(absolutePath, fileName);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"could not get file '{absolutePath}'; file does not exist");
                return null;
            }
            else
            {
                return File.ReadAllBytes(absolutePath);
            }
        });
    }

    public override async Task SetFile(string path, string fileName, byte[] bytes, bool allowOverride, bool createDirectory)
    {
        string absolutePath = GetAbsolutePath(path, createDirectory);
        await Task.Run(() =>
        {
            absolutePath = Path.Combine(absolutePath, fileName);
            string absoluteDirectory = Path.GetDirectoryName(absolutePath);
            if (!Directory.Exists(absoluteDirectory))
            {
                Debug.LogWarning($"could not save file '{absolutePath}'; directory does not exist");
                return;
            }
            if (File.Exists(absolutePath) && !allowOverride)
            {
                Debug.LogWarning($"file '{absolutePath}' already exists; override not allowed");
                return;
            }
            else
            {
                File.WriteAllBytes(absolutePath, bytes);
            }
        });
    }

    public override async Task DeleteFile(string path, string fileName)
    {
        string absolutePath = GetAbsolutePath(path, false);
        await Task.Run(() =>
        {
            absolutePath = Path.Combine(absolutePath, fileName);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
            else
            {
                Debug.LogWarning($"could not delete file'{absolutePath}'; file does not exist");
            }
        });
    }

    private string GetAbsolutePath(string path, bool createPath)
    {
        string absolutePath = Path.Combine(ConfigHelper.AbsLocalPath, directory, path);
        if (createPath)
        {
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }
        }
        else
        {
            string p = Path.Combine(ConfigHelper.AbsLocalPath, directory);
            if (!Directory.Exists(p))
            {
                Directory.CreateDirectory(p);
            }
        }
        return absolutePath;
    }

    public override bool IsAvailable()
    {
        if (!Directory.Exists(ConfigHelper.AbsLocalPath))
            return false;

        string p = Path.Combine(ConfigHelper.AbsLocalPath, directory);

        if (!Directory.Exists(p))
        {
            try
            {
                Directory.CreateDirectory(p);
            }
            catch { }
        }

        return Directory.Exists(p);
    }
}
