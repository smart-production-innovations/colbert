using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

//manages one loaded model and its belonging interactables
public class NetworkModel : NetworkBehaviour
{
    [SerializeField]
    private NetworkModelInteractable interactablePrefab;
    [SerializeField]
    private GameObject nodePrefab;
    [SerializeField]
    private MeshFilter meshNodePrefab;
    [SerializeField]
    private Material material;
    [SerializeField]
    private Material materialDouble;

    private int nodeCount;
    private int meshCount;
    private int vertexCount;
    private int triangleCount;

    private string modelName = null;
    private string metadataName = null;
    private ModelNode rootNode = null;
    private List<NetworkModelInteractable> interactables = new List<NetworkModelInteractable>();
    private bool withPhysics = false;
    private NetworkModelsManager modelsManager = null;

    public NetworkModelInteractable InteractablePrefab => interactablePrefab;
    public List<NetworkModelInteractable> Interactables => interactables;
    public ModelNode RootNode => rootNode;
    public bool Initialized => rootNode != null;
    public string ModelName => modelName;
    public bool WithPhysics => withPhysics;
    public int TriangleCount => triangleCount;

    [HideInInspector]
    public UnityEvent initializedEvent;

    private void Awake()
    {
        modelsManager = FindAnyObjectByType<NetworkModelsManager>();
    }

    private void OnEnable()
    {
        LoadConfig();
    }

    private void OnDisable()
    {
        if (modelsManager.LoadedModels.Contains(this))
            modelsManager.LoadedModels.Remove(this);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            InitializeServerRpc(NetworkManager.LocalClientId);
    }

    public bool Equals(string model, string metadata)
    {
        return this.modelName.Equals(model, StringComparison.InvariantCultureIgnoreCase) && this.metadataName.Equals(metadata, StringComparison.InvariantCultureIgnoreCase);
    }

    #region static create/delete

    public static async Task<NetworkModel> Create(NetworkModel prefab, string modelName, string metadataName)
    {
        NetworkModel model = Instantiate(prefab);
        if (NetworkManager.Singleton.IsServer)  //do not use model.IsServer here, it is always false until model is spawned!
            model.NetworkObject.Spawn();
        await model.Initialize(modelName, metadataName);
        return model;
    }

    public static void Delete(NetworkModel model)
    {
        if (!model.Initialized)
        {
            Debug.LogWarning($"could not delete model - model is loading");
            return;
        }

        if (model.IsSpawned && model.IsServer)
            model.NetworkObject.Despawn();
        while (model.interactables.Count > 0)
            NetworkModelInteractable.Delete(model.interactables[0], false);
        Destroy(model.gameObject);
    }

    #endregion

    #region initialize

    //load the model from file and create an interactable for it
    private async Task Initialize(string modelName, string metadataName)
    {
        if (Initialized)
        {
            Debug.LogWarning($"NetworkModel already initialized - skip Initialize");
            return;
        }

        this.name = $"{this.name} {modelName}";
        this.modelName = modelName;
        this.metadataName = metadataName;

        if (!modelsManager.LoadedModels.Contains(this))
            modelsManager.LoadedModels.Add(this);

        NetworkModelInteractable interactable = null;
        if (!IsSpawned || IsServer)
            interactable = NetworkModelInteractable.Create(this, null, interactablePrefab);

        Transform parent = interactable?.transform ?? this.transform;

        byte[] modelBytes = await modelsManager.FileManager.GetFile(modelName);
        byte[] metadataBytes = null;
        string metadataExtension = null;
        Dictionary<string, string> metadataParameter = null;
        if (!string.IsNullOrEmpty(metadataName))
        {
            metadataExtension = Path.GetExtension(metadataName);
            metadataBytes = await modelsManager.FileManager.GetFile(metadataName);
            metadataParameter = ConfigHelper.ReadVariables(ConfigHelper.metadataConfigFile);
        }

        //load the model from file
        GameObject model = await ModelLoader.LoadModel(modelBytes, metadataBytes, metadataExtension, metadataParameter, parent, nodePrefab, meshNodePrefab, material, materialDouble);
        rootNode = model.GetComponent<ModelNode>();
        if (rootNode.transform.parent != this.transform)
            rootNode.transform.parent = this.transform;

        rootNode.OverrideParent(this.transform);
        CalculateStatistics();

        interactable?.Initialize(this, rootNode);
        initializedEvent?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitializeServerRpc(ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
        InitializeClientRpc(modelName, metadataName, clientRpcParams);
    }

    [ClientRpc]
    private void InitializeClientRpc(string modelName, string metadataName, ClientRpcParams clientRpcParams)
    {
#pragma warning disable CS4014
        Initialize(modelName, metadataName);
#pragma warning restore CS4014
    }

    private void LoadConfig()
    {
        var variables = ConfigHelper.ReadVariables(ConfigHelper.physicsConfigFile);
        if (variables != null)
        {
            if (variables.TryGetValue("physics", out string value))
            {
                withPhysics = value.Equals("yes", System.StringComparison.InvariantCultureIgnoreCase)
                    || value.Equals("true", System.StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }

    private void CalculateStatistics()
    {
        CalculateStatisticsRecursive(rootNode, out nodeCount, out meshCount, out vertexCount, out triangleCount);
    }

    private static void CalculateStatisticsRecursive(ModelNode node, out int nodeCount, out int meshCount, out int vertexCount, out int triangleCount)
    {
        nodeCount = 1;
        meshCount = 0;
        vertexCount = 0;
        triangleCount = 0;
        if (node.TryGetComponent(out MeshFilter mf))
        {
            meshCount = 1;
            var mesh = mf.sharedMesh;
            vertexCount = mesh.vertexCount;
            triangleCount = mesh.triangles.Length / 3;
        }
        foreach (var child in node.Children)
        {
            CalculateStatisticsRecursive(child, out int childNodeCount, out int childMeshCount, out int childVertexCount, out int childTriangleCount);
            nodeCount += childNodeCount;
            meshCount += childMeshCount;
            vertexCount += childVertexCount;
            triangleCount += childTriangleCount;
        }
    }

    #endregion

    #region node <-> nodepath

    //get a model node along a hierarchy path (e.g. root/this/is/a/path)
    public ModelNode GetNode(string path)
    {
        string[] stringPath = path.Split('/');
        int[] intPath = new int[stringPath.Length];
        for (int i = 0; i < intPath.Length; i++)
        {
            intPath[i] = int.Parse(stringPath[i]);
        }
        return GetNode(intPath);
    }

    //get a model node along a hierarchy path with child indices (as in ModelNode.children list)
    private ModelNode GetNode(int[] path)
    {
        ModelNode part = rootNode;
        for (int i = 1; i < path.Length; i++)
        {
            part = part.Children[path[i]];
        }
        return part;
    }

    //get the path to a ModelNode in the model hierarchy
    public string GetPath(ModelNode node)
    {
        ModelNode tr = node;
        string path = "";
        while (tr.Parent != null)
        {
            for (int i = 0; i < tr.Parent.Children.Count; i++)
            {
                ModelNode child = tr.Parent.Children[i];
                if (child == tr)
                {
                    path = $"{i}/{path}";
                    break;
                }
            }
            tr = tr.Parent;
        }
        path = $"{0}/{path}";
        path = path.Remove(path.Length - 1, 1);
        return path;
    }

    #endregion
}
