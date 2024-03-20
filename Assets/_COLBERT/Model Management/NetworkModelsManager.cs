using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

//manage loading/deleting of models
public class NetworkModelsManager : NetworkBehaviour
{
    [SerializeField]
    private PlayerManager playerManager;
    [SerializeField]
    private NetworkFileManager fileManager;
    [SerializeField]
    private NetworkModel networkModelPrefab;
    [SerializeField]
    private float spawnDistance = 1.5f;
    [SerializeField]
    private string[] modelFileExtensions;
    [SerializeField]
    private string[] metadataFileExtensions;

    private List<NetworkModel> loadedModels = new List<NetworkModel>();
    private List<ModelFileData> availableModels = new List<ModelFileData>();
    private bool isloading = false;
    public NetworkFileManager FileManager => fileManager;
    public List<NetworkModel> LoadedModels => loadedModels;

    public UnityEvent<List<ModelFileData>> listUpdatedEvent;

    private void Awake()
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        UpdateList(); //update once at start to initialize FileManager (or else filters are not set in viewer build when files are requested)
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    public bool IsLoaded(string modelName, out NetworkModel model)
    {
        foreach (NetworkModel m in loadedModels)
        {
            if (m.ModelName == modelName)
            {
                model = m;
                return true;
            }
        }
        model = null;
        return false;
    }

    #region load model

    public async Task LoadModel(string modelName, string metadataName, Player player)
    {
        if (player != null && player == playerManager.PlayerNonXR)
            await LoadModel(modelName, metadataName, false, NetworkManager.LocalClientId);
        else if (player != null && player == playerManager.PlayerXR)
            await LoadModel(modelName, metadataName, true, NetworkManager.LocalClientId);
    }

    private async Task LoadModel(string modelName, string metadataName, bool xr, ulong clientId)
    {
        if (isloading)
            return;

        if (IsSpawned && !IsServer)
        {
            LoadModelServerRpc(NetworkManager.LocalClientId, xr, modelName, metadataName);
            return;
        }

        if (IsLoaded(modelName, out _))
        {
            Debug.LogWarning($"could not load file - already loaded");
            return;
        }

        //load the model
        isloading = true;
        float starttime = Time.time;
        NetworkModel model = await NetworkModel.Create(networkModelPrefab, modelName, metadataName);
        isloading = false;
        Debug.Log($"loaded model '{modelName}' with metadata '{metadataName}' in {Time.time - starttime} seconds");

        //place the model
        NetworkModelInteractable interactable = model.Interactables[0];
        if (clientId != NetworkManager.LocalClientId) //place on client (multiplayer)
        {
            interactable.NetworkObject.ChangeOwnership(clientId);
            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
            LoadModelClientRpc(new NetworkObjectReference(interactable.NetworkObject), xr, clientRpcParams);
            return;
        }
        PlaceObject(interactable, xr);
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadModelServerRpc(ulong clientId, bool xr, string modelName, string metadataName)
    {
#pragma warning disable CS4014
        LoadModel(modelName, metadataName, xr, clientId);
#pragma warning restore CS4014
    }

    [ClientRpc]
    private void LoadModelClientRpc(NetworkObjectReference interactableReference, bool xr, ClientRpcParams clientRpcParams)
    {
        if (interactableReference.TryGet(out NetworkObject no))
            PlaceObject(no.GetComponent<NetworkModelInteractable>(), xr);
    }

    private async void PlaceObject(NetworkModelInteractable interactable, bool xr)
    {
        while (!interactable.Initialized)
            await Task.Yield();

        Player player = xr ? playerManager.PlayerXR : playerManager.PlayerNonXR;

        interactable.Node.CalculateBounds();
        float diagonal = interactable.Node.Bounds.size.magnitude;
        Vector3 startPosition = player.Camera.transform.position + spawnDistance * player.Camera.transform.forward;
        Quaternion startRotation = Quaternion.identity;
        ConstrainedGrabTransformer transformer = interactable.GetComponent<ConstrainedGrabTransformer>();
        LayerMask everythingMask = ~0;
        Bounds bounds = transformer.Bounds;

        startRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(player.Camera.transform.forward, Vector3.up).normalized);
        startRotation = startRotation * interactable.transform.rotation;

        if (Physics.Raycast(new Vector3(startPosition.x, player.Camera.transform.position.y, startPosition.z), -Vector3.up, out RaycastHit hit, 100, everythingMask.value, QueryTriggerInteraction.Ignore))
        {
            Vector3 newCenter = startPosition + startRotation * bounds.center;
            float bottomPos = newCenter.y - Mathf.Abs((startRotation * bounds.extents).y); //lowest point of bounding box
            float overlap = hit.point.y - bottomPos;

            if (overlap > 0 || diagonal > 1.5f) //if interactable intersects floor or interactable is big -> place on floor
            {
                startPosition.y += hit.point.y - bottomPos; //move object up so it does not intersect with floor
            }
        }

        interactable.transform.rotation = startRotation;

        Vector3 centerOffset = startRotation * bounds.center;
        centerOffset.y = 0f;
        interactable.transform.position = startPosition - centerOffset;

        //close tablet
        TabletManagerPC tab1 = player.GetComponentInChildren<TabletManagerPC>();
        tab1?.CloseTablet();
        TabletManagerXR tab2 = player.GetComponentInChildren<TabletManagerXR>();
        tab2?.gameObject.SetActive(false);
    }

    #endregion

    #region delete model

    [ServerRpc(RequireOwnership = false)]
    private void DeleteModelServerRpc(NetworkObjectReference modelReference)
    {
        if (!modelReference.TryGet(out NetworkObject no))
            return;
        DeleteModel(no.GetComponent<NetworkModel>());
    }

    public void DeleteModel(NetworkModel model)
    {
        if (IsSpawned && IsClient && !IsServer)
        {
            DeleteModelServerRpc(new NetworkObjectReference(model.NetworkObject));
            return;
        }
        NetworkModel.Delete(model);
    }

    public void DeleteModel(string modelName)
    {
        foreach (NetworkModel model in loadedModels)
        {
            if (model.ModelName == modelName)
            {
                DeleteModel(model);
                return;
            }
        }
        Debug.Log($"could not delete model with name '{modelName}' - model not found");
    }

    #endregion

    #region model file list

    //determine list of available models from filelists from FileManager
    public void OnFilesUpdated(List<string> allFiles, List<string> commonFiles)
    {
        availableModels.Clear();
        //get model files
        foreach (var file in allFiles)
        {
            string extension = Path.GetExtension(file);
            foreach (string modelExt in modelFileExtensions)
            {
                if (extension.Equals(modelExt, StringComparison.InvariantCultureIgnoreCase))
                {
                    availableModels.Add(new ModelFileData { modelName = file });
                }
            }
        }
        //add metadata files
        foreach (var file in allFiles)
        {
            string extension = Path.GetExtension(file);
            foreach (string metaExt in metadataFileExtensions)
            {
                if (extension.Equals(metaExt, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var modeldata in availableModels)
                    {
                        if (Path.GetFileNameWithoutExtension(modeldata.modelName).Equals(Path.GetFileNameWithoutExtension(file), StringComparison.InvariantCultureIgnoreCase))
                            modeldata.metadataName = file;
                    }
                }
            }
        }
        //check for common models on all clients
        foreach (var modeldata in availableModels)
        {
            bool sameModel = false;
            bool sameMetadata = false;
            foreach (string file in commonFiles)
            {
                if (file.Equals(modeldata.modelName, StringComparison.InvariantCultureIgnoreCase))
                    sameModel = true;
                
                if (file.Equals(modeldata.metadataName, StringComparison.InvariantCultureIgnoreCase))
                    sameMetadata = true;
            }
            if (sameModel && (sameMetadata || string.IsNullOrEmpty(modeldata.metadataName)))
                modeldata.everyone = true;
        }
        listUpdatedEvent?.Invoke(availableModels);
    }

    //request model list update from filemanager
    public async Task UpdateList()
    {
        string[] allExtensions = new string[modelFileExtensions.Length + metadataFileExtensions.Length];
        for (int i = 0; i < modelFileExtensions.Length; i++)
            allExtensions[i] = modelFileExtensions[i];
        for (int i = 0; i < metadataFileExtensions.Length; i++)
            allExtensions[i + modelFileExtensions.Length] = metadataFileExtensions[i];

        fileManager.SetFilter(allExtensions);
        await fileManager.UpdateFileList();
    }


    #endregion
}
