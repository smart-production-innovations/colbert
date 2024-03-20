using GLTFast;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

//management of environments (both integrated and from files)
//switch active environment
//determine locally available environments, and in multiplayer which environments are available across all clients
public class NetworkEnvironmentManager : NetworkBehaviour
{
    [HideInInspector]
    public List<string> environments = new List<string>(); //contains all locally available environments (name of the environment scene, or for external models the modelname)
    [HideInInspector]
    public List<string> commonEnvironments = new List<string>(); //contains only environments that are available for all clients

    private string environmentName = null; //name of the active environment, as in list above
    private NetworkVariable<NetworkString> environmentNameNet = new NetworkVariable<NetworkString>(NetworkString.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Scene loadedScene; //active environment scene

    public string ActiveEnvironment => environmentName;

    [SerializeField]
    [HideInInspector]
    private string[] environmentSceneNames = null;
    [SerializeField]
    [HideInInspector]
    private string environmentSceneExternalName = null;

    //show fields for scene assignment only in editor, copy names to hidden string variables for runtime loading
#if UNITY_EDITOR
    [SerializeField]
    private UnityEditor.SceneAsset[] environmentScenes;
    [SerializeField]
    private UnityEditor.SceneAsset environmentSceneExternal;
    private void OnValidate()
    {
        if (environmentScenes != null)
        {
            environmentSceneNames = new string[environmentScenes.Length];
            for (int i = 0; i < environmentScenes.Length; i++)
                environmentSceneNames[i] = environmentScenes[i].name;
        }
        environmentSceneExternalName = environmentSceneExternal.name;
    }
#endif

    [SerializeField]
    private string[] fileExtensions; //valid file extensions

    public UnityEvent listUpdatedEvent;

    //server only:
    private Dictionary<ulong, int[]> clientEnvironments = new Dictionary<ulong, int[]>();
    private List<ulong> requestingClients = new List<ulong>();


    private void Awake()
    {
        environmentNameNet.OnValueChanged += (NetworkString previousValue, NetworkString newValue) =>
        {
            ChangeToEnvironmentLocal(newValue.Value);
        };

        //determine active environment at start
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (environmentSceneNames.Contains(scene.name))
            {
                loadedScene = scene;
                environmentName = scene.name;
                return;
            }
        }

        //if no environment active -> load first environment
        ChangeToEnvironment(environmentSceneNames[0]);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //sync environments to host environment on spawn
        if (IsServer)
        {
            NetworkManager.SceneManager.VerifySceneBeforeLoading =
                (int sceneIndex, string sceneName, LoadSceneMode loadSceneMode) =>
                { return !environmentSceneNames.Contains(sceneName); };
            environmentNameNet.Value = new NetworkString(environmentName);
        }
        else
        {
            ChangeToEnvironmentLocal(environmentNameNet.Value.Value);
        }
    }

    //update 'environments' (list with available environments)
    public void UpdateAvailableEnvironments(bool silent = false)
    {
        if (IsSpawned && !silent)
        {
            RequestNetworkEnvironments();
            return;
        }

        //add integrated environments
        environments = new List<string>(environmentSceneNames);

        //add environments from files
        string environmentDirectory = ConfigHelper.Path(ConfigHelper.environmentDirectory);
        if (Directory.Exists(environmentDirectory))
        {
            foreach (string env in Directory.GetFiles(environmentDirectory))
            {
                //check file extension
                string extension = Path.GetExtension(env);
                foreach (string fileExtension in fileExtensions)
                    if (extension.Equals(fileExtension, StringComparison.InvariantCultureIgnoreCase))
                        environments.Add(Path.GetFileName(env));
            }
        }

        if (!silent)
        {
            listUpdatedEvent?.Invoke();
        }
    }

    //try to change environment (offline: change immediately, multiplayer: request change
    public void ChangeToEnvironment(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        if (IsSpawned)
            ChangeToEnvironmentServerRpc(sceneName);
        else
            ChangeToEnvironmentLocal(sceneName);
    }

    //send environment change request to server
    [ServerRpc(RequireOwnership = false)]
    private void ChangeToEnvironmentServerRpc(string sceneName)
    {
        environmentNameNet.Value = new NetworkString(sceneName);
    }

    //change environment now locally
    private async void ChangeToEnvironmentLocal(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (sceneName.Equals(environmentName))
            return;

        //unload old scene
        if (loadedScene != null && loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(loadedScene);
        }

        //update list of environments
        UpdateAvailableEnvironments();

        if (environmentSceneNames.Contains(sceneName))
        {
            Debug.Log($"load integrated environment '{sceneName}'...");
            loadedScene = SceneManager.LoadScene(sceneName, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive });
            environmentName = sceneName;
        }
        else
        {
            Debug.Log($"load external environment '{sceneName}'...");
            loadedScene = SceneManager.LoadScene(environmentSceneExternalName, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive });
            await Task.Yield();

            bool success = await TryLoadEnvironmentFromFile(sceneName);
            if (!success)
                return;

            environmentName = sceneName;
        }

        //Place Players on middle of Scene
        PlayerManager playerManager = FindAnyObjectByType<PlayerManager>();
        playerManager.PlayerXR.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        playerManager.PlayerNonXR.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        listUpdatedEvent?.Invoke();
    }

    private async Task<bool> TryLoadEnvironmentFromFile(string sceneName)
    {
        string environmentDirectory = ConfigHelper.Path(ConfigHelper.environmentDirectory);
        string filePath = Path.Combine(environmentDirectory, sceneName);
        if (!File.Exists(filePath))
            return false;

        byte[] data = File.ReadAllBytes(filePath);
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data, new Uri(filePath));
        if (!success)
            return false;

        Environment env = loadedScene.GetRootGameObjects()[0].GetComponent<Environment>();

        success = await gltf.InstantiateSceneAsync(env.meshContainer.transform);
        env.Initialize(sceneName);

        return true;
    }

    //update 'commonEnvironments' (determine list with environments available on all connected clients) (only hashes of filenames are transmitted, no actual strings)
    //approach:
    //-client requests common list from server,
    //-server requests local list from each client,
    //-each client sends its local list to server,
    //-server determines common list after receiving local list from last client,
    //-server sends common list to requesting client
    #region network common environments

    //initiate request to server
    private void RequestNetworkEnvironments()
    {
        RequestNetworkEnvironmentsServerRpc(NetworkManager.LocalClientId);
    }

    //request to server
    [ServerRpc(RequireOwnership = false)]
    private void RequestNetworkEnvironmentsServerRpc(ulong clientId)
    {
        if (requestingClients.Count == 0)
            clientEnvironments.Clear();
        if (!requestingClients.Contains(clientId))
            requestingClients.Add(clientId);
        RequestNetworkEnvironmentsClientRpc();
    }

    //request to all clients
    [ClientRpc]
    private void RequestNetworkEnvironmentsClientRpc()
    {
        SendNetworkEnvironments();
    }

    //clients send their available environments to server
    private void SendNetworkEnvironments()
    {
        UpdateAvailableEnvironments(true);

        //collect hashes of locally available environments
        int[] hashes = new int[environments.Count];
        for (int i = 0; i < hashes.Length; i++)
            hashes[i] = GetEnvironmentHash(environments[i]);

        NetworkIntArray list = new NetworkIntArray();
        list.value = hashes;
        SendNetworkEnvironmentsServerRpc(NetworkManager.LocalClientId, list);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendNetworkEnvironmentsServerRpc(ulong clientId, NetworkIntArray hashesNet)
    {
        clientEnvironments[clientId] = hashesNet.value;

        if (clientEnvironments.Count == NetworkManager.ConnectedClientsIds.Count)
        {
            //all client have sent their available environments -> determine common environments
            NetworkIntArray commonHashList = new NetworkIntArray();
            commonHashList.value = GetCommonHashes();

            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = requestingClients } };
            SendNetworkEnvironmentsClientRpc(commonHashList, clientRpcParams);

            clientEnvironments.Clear();
            requestingClients.Clear();
        }
    }

    [ClientRpc]
    private void SendNetworkEnvironmentsClientRpc(NetworkIntArray hashesNet, ClientRpcParams clientRpcParams)
    {
        int[] hashes = hashesNet.value;

        //update list of common environments
        commonEnvironments.Clear();
        foreach (int hash in hashes)
        {
            foreach (string environment in environments)
            {
                if (GetEnvironmentHash(environment) == hash)
                    commonEnvironments.Add(environment);
            }
        }

        listUpdatedEvent?.Invoke();
    }

    //calculate hash of environment filename (to send hashes instead of filenames over network)
    private static int GetEnvironmentHash(string filename)
    {
        return filename.ToLowerInvariant().GetHashCode();
    }

    //on server: determine and return an array containing hashes of all environments that are available on all clients
    private int[] GetCommonHashes()
    {
        List<int> environments = null;
        foreach (var clientEnvironment in this.clientEnvironments)
        {
            ulong clientId = clientEnvironment.Key;
            if (!NetworkManager.ConnectedClientsIds.Contains(clientId))
                continue;

            int[] hashes = clientEnvironment.Value;
            if (environments == null)
            {
                environments = new List<int>();
                foreach (int hash in hashes)
                    environments.Add(hash);
            }
            else
            {
                for (int i = 0; i < environments.Count; i++)
                {
                    if (!hashes.Contains(environments[i]))
                    {
                        environments.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        return environments.ToArray();
    }

    #endregion

}
