using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

//base class for file managing/loading (e.g. files from local file system folder, from cloud service...)
public abstract class FileSource : MonoBehaviour
{
    public abstract bool IsAvailable();
    public abstract Task<bool> FileExist(string path, string fileName);
    public abstract Task<bool> PathExist(string path);
    public abstract Task<List<string>> ListFiles(string path);
    public abstract Task<byte[]> GetFile(string path, string fileName);
    public abstract Task SetFile(string path, string fileName, byte[] bytes, bool allowOverride, bool createDirectory);
    public abstract Task DeleteFile(string path, string fileName);
}
