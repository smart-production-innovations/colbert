using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

//files stored in memory (e.g. for webgl which cannot access the local file system)
public class MemoryFileSource : FileSource
{
    private Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

    private string FilePath(string path, string fileName)
    {
        return Path.Combine(path, fileName);
    }

    public override Task DeleteFile(string path, string fileName)
    {
        string key = Path.Combine(path, fileName);

        if (files.ContainsKey(key))
            files.Remove(key);

        return Task.CompletedTask;
    }

#pragma warning disable 1998
    public override async Task<bool> FileExist(string path, string fileName)
    {
        return files.ContainsKey(Path.Combine(path, fileName));
    }

    public override async Task<byte[]> GetFile(string path, string fileName)
    {
        string key = Path.Combine(path, fileName);

        if (files.ContainsKey(key))
            return files[key];
        else
            return null;
    }

    public override async Task<List<string>> ListFiles(string path)
    {
        List<string> list = new List<string>();
        foreach (var key in files.Keys)
        {
            if (Path.GetDirectoryName(key) == path)
                list.Add(key);
        }
        return list;
    }

    public override async Task<bool> PathExist(string path)
    {
        return true;
    }

    public override async Task SetFile(string path, string fileName, byte[] bytes, bool allowOverride, bool createDirectory)
    {
        string key = Path.Combine(path, fileName);

        if (files.ContainsKey(key) && !allowOverride)
            return;

        files[key] = bytes;
    }
#pragma warning restore 1998

    public override bool IsAvailable()
    {
        return true;
    }

}
