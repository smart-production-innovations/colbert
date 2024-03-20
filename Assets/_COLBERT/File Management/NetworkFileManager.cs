using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetworkFileManager : NetworkBehaviour
{
    [SerializeField]
    private FileSource fileSource;

    private string[] extensionFilter = new string[0];

    private List<string> files = new List<string>();
    private List<string> commonFiles = new List<string>();
    private bool listTask = false;

    public FileSource Filesource => fileSource;
    public UnityEvent<List<string>, List<string>> fileListUpdatedEvent;

    //server only:
    private Dictionary<ulong, int[]> clientFiles = new Dictionary<ulong, int[]>();
    private List<ulong> requestingClients = new List<ulong>();


    public void SetFilter(string[] extensions)
    {
        extensionFilter = extensions;
    }

    public async Task UpdateFileList(bool silent = false)
    {
        if (IsSpawned && !silent)
        {
            RequestNetworkFiles();
            return;
        }

        if (listTask)
        {
            Debug.LogWarning("already listing");
            return;
        }
        try
        {
            listTask = true;
            files = await fileSource.ListFiles("");
            await FilterList();
            await SortList();
        }
        finally
        {
            listTask = false;
        }

        if (!silent)
        {
            fileListUpdatedEvent?.Invoke(files, commonFiles);
        }
    }

#pragma warning disable CS1998
    public async Task<string> FindFile(string path, string fileName)
#pragma warning disable CS1998
    {
        //await UpdateFileList(); //TODO: maybe only update list, if list does not contain the file, then check again?
        string foundFile = null;
        foreach (string file in files)
        {
            if (fileName.Equals(file, System.StringComparison.InvariantCultureIgnoreCase))
            {
                foundFile = file;
                break;
            }
        }
        return foundFile;
    }

    public async Task<byte[]> GetFile(string fileName)
    {
        return await fileSource.GetFile("", fileName);
    }

    private async Task FilterList()
    {
        await Task.Run(() =>
        {
            for (int i = 0; i < files.Count; i++)
            {
                string extension = Path.GetExtension(files[i]);
                bool validExtension = false;
                foreach (var ext in extensionFilter)
                    if (extension.Equals(ext, System.StringComparison.InvariantCultureIgnoreCase))
                        validExtension = true;
                
                if (!validExtension)
                    files.RemoveAt(i);
            }
        });
    }

    private async Task SortList()
    {
        await Task.Run(() =>
        {
            files.Sort(SortByNameAscending);
        });
    }

    private static int SortByNameAscending(string x, string y)
    {
        return x.CompareTo(y);
    }

    #region network file list

    private void RequestNetworkFiles()
    {
        RequestNetworkFilesServerRpc(NetworkManager.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNetworkFilesServerRpc(ulong clientId)
    {
        if (requestingClients.Count == 0)
            clientFiles.Clear();
        if (!requestingClients.Contains(clientId))
            requestingClients.Add(clientId);
        RequestNetworkFilesClientRpc();
    }

    [ClientRpc]
    private void RequestNetworkFilesClientRpc()
    {
        SendNetworkFiles();
    }

    private async void SendNetworkFiles()
    {
        await UpdateFileList(true);

        int[] hashes = new int[files.Count];
        for (int i = 0; i < hashes.Length; i++)
            hashes[i] = GetFileHash(files[i]);

        NetworkIntArray list = new NetworkIntArray();
        list.value = hashes;
        SendNetworkFilesServerRpc(NetworkManager.LocalClientId, list);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendNetworkFilesServerRpc(ulong clientId, NetworkIntArray hashesNet)
    {
        clientFiles[clientId] = hashesNet.value;

        if (clientFiles.Count == NetworkManager.ConnectedClientsIds.Count)
        {
            NetworkIntArray commonHashList = new NetworkIntArray();
            commonHashList.value = GetCommonHashes();

            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = requestingClients } };
            SendNetworkFilesClientRpc(commonHashList, clientRpcParams);

            clientFiles.Clear();
            requestingClients.Clear();
        }
    }

    [ClientRpc]
    private void SendNetworkFilesClientRpc(NetworkIntArray hashesNet, ClientRpcParams clientRpcParams)
    {
        int[] hashes = hashesNet.value;

        commonFiles.Clear();

        if (IsSpawned)
        {
            foreach (int hash in hashes)
            {
                foreach (string file in files)
                {
                    if (GetFileHash(file) == hash)
                        commonFiles.Add(file);
                }
            }
        }

        fileListUpdatedEvent?.Invoke(files, commonFiles);
    }

    private static int GetFileHash(string filename)
    {
        return filename.ToLowerInvariant().GetHashCode();
    }

    private int[] GetCommonHashes()
    {
        List<int> files = null;
        foreach (var clientFiles in this.clientFiles)
        {
            ulong clientId = clientFiles.Key;
            if (!NetworkManager.ConnectedClientsIds.Contains(clientId))
                continue;

            int[] hashes = clientFiles.Value;
            if (files == null)
            {
                files = new List<int>();
                foreach (int hash in hashes)
                    files.Add(hash);
            }
            else
            {
                for (int i = 0; i < files.Count; i++)
                {
                    if (!hashes.Contains(files[i]))
                    {
                        files.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        return files.ToArray();
    }

    #endregion
}
