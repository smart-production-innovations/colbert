using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

//manages an interactable part/subassembly (ModelNode) of a loaded model (NetworkModel)
//with physics enabled: manages a second object (physicsDummy) containing just a rigidbody and boxcollider. If the interactable is selected the physicsDummy follows the interactable or else the interactable follows the physicsdummy.
public class NetworkModelInteractable : NetworkBehaviour
{
    [SerializeField]
    private float snapDistance = 0.2f;
    [SerializeField]
    private float snapSpeed = 5f;
    [SerializeField]
    private float attractSpeed = 5f;
    [SerializeField]
    private Material ghostMaterial;
    [SerializeField]
    private Rigidbody physicsDummyPrefab;

    private NetworkModel model = null;
    private ModelNode node = null;

    private Rigidbody physicsDummy = null;

    public ModelNode Node => node;
    public bool Initialized => node != null;
    public NetworkModel Model => model;

    [HideInInspector]
    public UnityEvent initializedEvent;

    private Coroutine snapCoroutine = null;
    private Coroutine attractCoroutine = null;

    private void OnEnable()
    {
        if (model && !model.Interactables.Contains(this))
            model.Interactables.Add(this);
    }

    private void OnDisable()
    {
        if (model && model.Interactables.Contains(this))
            model.Interactables.Remove(this);

        DestroyPhysicsDummy();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkObject.DontDestroyWithOwner = true; //keep the interactable if the owning client disconnects

        if (!IsServer)
            InitializeServerRpc(NetworkManager.LocalClientId);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            node?.ResetParent();
    }

    #region static create/delete
    //create or delete an interactable

    public static NetworkModelInteractable Create(NetworkModel model, ModelNode node, NetworkModelInteractable interactablePrefab)
    {
        Vector3 position = node != null ? node.transform.position : Vector3.zero;
        Quaternion rotation = node != null ? node.transform.rotation : Quaternion.identity;
        NetworkModelInteractable interactable = Instantiate(interactablePrefab, position, rotation);
        if (node != null)
        {
            interactable.Initialize(model, node);
        }
        return interactable;
    }

    public static void Delete(NetworkModelInteractable interactable, bool resetNode)
    {
        interactable.node?.ResetParent();

        if (interactable.IsServer && interactable.IsSpawned)
            interactable.NetworkObject.Despawn();

        Destroy(interactable.gameObject);
    }

    #endregion

    #region initialize
    //initialize the interactable (assign its ModelNode, assemble its children, position the interactable)

    [ServerRpc(RequireOwnership = false)]
    private void InitializeServerRpc(ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
        NetworkObjectReference modelReference = new NetworkObjectReference(model.NetworkObject);
        InitializeClientRpc(modelReference, model.GetPath(node), clientRpcParams);
    }

    [ClientRpc]
    private void InitializeClientRpc(NetworkObjectReference modelReference, string path, ClientRpcParams clientRpcParams)
    {
        if (!modelReference.TryGet(out NetworkObject networkObject))
            return;
        NetworkModel model = networkObject.GetComponent<NetworkModel>();
        if (model.Initialized)
            Initialize(model, path);
        else
            model.initializedEvent.AddListener(() => Initialize(model, path));
    }

    private void Initialize(NetworkModel model, string path)
    {
        Initialize(model, model.GetNode(path));
    }

    public void Initialize(NetworkModel model, ModelNode node)
    {
        if (Initialized)
        {
            Debug.LogWarning($"NetworkModelInteractable already initialized - skip Initialize");
            return;
        }

        this.model = model;
        this.node = node;
        this.name = $"{this.name} {this.node.name}";

        if (!model.Interactables.Contains(this))
            model.Interactables.Add(this);

        if (this.node.Parent == null && IsOwner)
            this.transform.SetPositionAndRotation(this.node.transform.position, this.node.transform.rotation); //prevents wrong initial rotation, if model root has a rotation

        if (this.node.transform.parent != this.transform)
            this.node.transform.parent = this.transform;

        this.node.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); //prevents wrong placement of node, if client joins after model was disassembled

        //updated colliders in XRGrabInteractable are only registered when enabled -> disable and enable
        gameObject.SetActive(false);
        GetComponentsInChildren(GetComponent<XRGrabInteractable>().colliders);
        gameObject.SetActive(true);

        AttractChildren();

        if (NetworkManager.IsServer && !IsSpawned) //do not use IsServer here, it is always false until spawned!
            NetworkObject.Spawn();

        initializedEvent?.Invoke();
    }

    #endregion

    #region ownership (select)
    //for multiplayer: change ownership to the selecting client, drop interactable on all other clients

    public void OnSelectEnterOwnership(SelectEnterEventArgs args)
    {
        if (IsSpawned)
        {
            SelectEnterServerRpc(NetworkManager.LocalClientId);
        }
    }

    public void OnSelectExitOwnership(SelectExitEventArgs args)
    {

    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectEnterServerRpc(ulong clientId)
    {
        NetworkObject.ChangeOwnership(clientId);
        SelectEnterClientRpc();
    }

    [ClientRpc]
    private void SelectEnterClientRpc()
    {
        if (!IsOwner)
            SelectExit();
    }

    private void SelectExit()
    {
        XRInteractionManager manager = FindAnyObjectByType<XRInteractionManager>(); //todo: cache reference?
        IXRSelectInteractable interactable = GetComponent<IXRSelectInteractable>();
        for (int i = 0; i < interactable.interactorsSelecting.Count; i++)
        {
            IXRSelectInteractor interactor = interactable.interactorsSelecting[i];
            manager.SelectExit(interactor, interactable);
        }
    }

    #endregion

    #region glow (hover)
    //show glow effect on the interactable that is currently hovered

    public void OnHoverEnterGlow(HoverEnterEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    public void OnHoverExitGlow(HoverExitEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    public void OnSelectEnterGlow(SelectEnterEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    public void OnSelectExitGlow(SelectExitEventArgs args)
    {
        UpdateGlow((XRBaseInteractable)args.interactableObject);
    }

    private int id = 0;
    private bool init = false;
    private void InitGlow()
    {
        if (node == null)
            return;

        id = this.GetHashCode();
        MeshRenderer[] meshrenderers = node.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshrenderers)
            foreach (Material mat in renderer.sharedMaterials)
                mat.SetInt("_objID", id); //optimizsation: set id (_objID) on each belonging material once for the interactable, activate glow for an interactable by just setting a single global variable (_glowID) (implemented in glow shader)
        init = true;
    }

    private void UpdateGlow(XRBaseInteractable interactable)
    {
        bool glow = false;

        foreach (var hoveringInteractor in interactable.interactorsHovering)
            if (!((IXRSelectInteractor)hoveringInteractor).IsSelecting(interactable))
                glow = true;

        if (glow)
        {
            if (!init)
                InitGlow();
            Shader.SetGlobalInt("_glowID", id);
        }
        else
        {
            Shader.SetGlobalInt("_glowID", 0);
        }
    }

    #endregion

    #region ghost (target)
    //show ghost at original position relative to its parent (where it belongs to)

    private void OnRenderObject()
    {
        if (!GetComponent<IXRSelectInteractable>().isSelected)
            return;

        if (node.Parent == null)
            return;

        Matrix4x4 target = node.Parent.transform.localToWorldMatrix * node.LocalMatrix;
        if (Vector3.Distance(transform.position, target.GetPosition()) < snapDistance) //don't show ghost, if interactable is within snapDistance
            return;

        ghostMaterial.SetPass(0);
        MeshFilter[] mfs = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in mfs) //render all child meshes of the interactable
        {
            ModelNode node = mf.GetComponent<ModelNode>();
            Matrix4x4 targetLocal = node.LocalMatrix;
            node = node.Parent;
            while (node != null && node != this.node.Parent)
            {
                targetLocal = node.LocalMatrix * targetLocal;
                node = node.Parent;
            }
            if (node == null)
                continue;
            Matrix4x4 matrix = node.transform.localToWorldMatrix * targetLocal;
            Mesh mesh = mf.sharedMesh;
            Graphics.DrawMeshNow(mesh, matrix);
        }
    }

    #endregion

    #region snap (target)
    //snap interactable to its original position relative to its parent when dropped within snapDistance (manual assembly of single part)

    public void OnSelectEnterPhysics(SelectEnterEventArgs args)
    {
        DisablePhysics();
    }

    public void OnSelectExitSnap(SelectExitEventArgs args)
    {
        TargetSnap();
    }

    public void TargetSnap()
    {
        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(TargetSnapCoroutine());
    }

    private IEnumerator TargetSnapCoroutine()
    {
        if (node.Parent == null)
        {
            EnablePhysics();
            snapCoroutine = null;
            yield break;
        }
        Transform parent = node.Parent.transform;
        Matrix4x4 target = parent.localToWorldMatrix * node.LocalMatrix;
        if (Vector3.Distance(transform.position, target.GetPosition()) > snapDistance)
        {
            EnablePhysics();
            snapCoroutine = null;
            yield break;
        }
        DisablePhysics();

        var interactable = GetComponent<IXRSelectInteractable>();
        yield return null; //wait one frame or isSelected will return false, even if another interactor is still selecting

        while (Vector3.Distance(transform.position, target.GetPosition()) > 0.001f)
        {
            if (interactable.isSelected)
            {
                snapCoroutine = null;
                yield break;
            }
            if (IsSpawned && !IsOwner)
            {
                Debug.Log($"canceled snap - ownership lost");
                snapCoroutine = null;
                yield break;
            }

            float time = Vector3.Distance(transform.position, target.GetPosition()) / snapSpeed;
            float rotateSpeed = Quaternion.Angle(transform.rotation, target.rotation) / time;
            transform.position = Vector3.MoveTowards(transform.position, target.GetPosition(), snapSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target.rotation, rotateSpeed * Time.deltaTime);
            yield return null;
            target = parent.localToWorldMatrix * node.LocalMatrix;
        }
        transform.position = target.GetPosition();
        transform.rotation = target.rotation;
        snapCoroutine = null;
    }

    #endregion

    #region attract children (assemble)
    //all child nodes of the interactable are flying towards their original position in the modelhierarchy relative to this interactable (assemble)

    private void AttractChildren()
    {
        if (attractCoroutine != null)
            StopCoroutine(attractCoroutine);
        attractCoroutine = StartCoroutine(AttractChildrenCoroutine());
    }

    private IEnumerator AttractChildrenCoroutine()
    {
        ModelNode childParent = this.node;
        while (childParent.Children.Count == 1)
            childParent = childParent.Children[0];
        List<ModelNode> nodes = new List<ModelNode>();
        foreach (ModelNode child in childParent.Children)
            nodes.Add(child);

        while (nodes.Count > 0)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                ModelNode node = nodes[i];
                Matrix4x4 target = node.Parent.transform.localToWorldMatrix * node.LocalMatrix;
                float time = Vector3.Distance(node.transform.position, target.GetPosition()) / attractSpeed;
                float rotateSpeed = Quaternion.Angle(node.transform.rotation, target.rotation) / time;
                node.transform.position = Vector3.MoveTowards(node.transform.position, target.GetPosition(), attractSpeed * Time.deltaTime);
                node.transform.rotation = Quaternion.RotateTowards(node.transform.rotation, target.rotation, rotateSpeed * Time.deltaTime);
                if (Vector3.Distance(node.transform.position, target.GetPosition()) < 0.001f)
                {
                    node.transform.position = target.GetPosition();
                    node.transform.rotation = target.rotation;
                    nodes.Remove(node);
                    i--;
                }
            }
            yield return null;
        }

        if (model.WithPhysics)
        {
            CreatePhysicsDummy();
        }
    }

    #endregion

    #region physics
    //physics movement with boxcollider, if physics is enabled

    private Vector3 physicsVelocity = Vector3.zero;
    private Vector3 physicsAngularVelocity = Vector3.zero;
    private void CreatePhysicsDummy()
    {
        if (physicsDummy == null)
        {
            physicsDummy = Instantiate(physicsDummyPrefab);
            StartCoroutine(PhysicsDummyCoroutine());
        }
    }

    private void DestroyPhysicsDummy()
    {
        if (physicsDummy != null)
        {
            Destroy(physicsDummy.gameObject);
            physicsDummy = null;
        }
    }

    private void EnablePhysics()
    {
        if (physicsDummy != null)
        {
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            physicsDummy.isKinematic = false;
            physicsDummy.useGravity = true;
            physicsDummy.velocity = physicsVelocity;
            physicsDummy.angularVelocity = physicsAngularVelocity;
            physicsDummy.Move(rigidbody.position, rigidbody.rotation);
        }
    }

    private void DisablePhysics()
    {
        if (physicsDummy != null)
        {
            physicsDummy.useGravity = false;
            physicsDummy.isKinematic = true;
        }
    }

    IEnumerator PhysicsDummyCoroutine()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();

        node.CalculateBounds();
        BoxCollider collider = physicsDummy.GetComponent<BoxCollider>();
        collider.center = node.Bounds.center;
        collider.size = node.Bounds.size;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 previousPosition = position;
        Quaternion previousRotation = rotation;

        while (physicsDummy != null)
        {
            transform.GetPositionAndRotation(out position, out rotation);

            //track velocities
            physicsVelocity = (position - previousPosition) / Time.deltaTime;
            Quaternion rotationDiff = rotation * Quaternion.Inverse(previousRotation);
            Vector3 eulerAngles = rotationDiff.eulerAngles;
            Vector3 deltaAngles = new Vector3(
                Mathf.DeltaAngle(0f, eulerAngles.x),
                Mathf.DeltaAngle(0f, eulerAngles.y),
                Mathf.DeltaAngle(0f, eulerAngles.z));
            physicsAngularVelocity = (deltaAngles / Time.deltaTime) * Mathf.Deg2Rad;

            //interactable follows dummy or the reverse
            if (physicsDummy.isKinematic)
            {
                physicsDummy.Move(rigidbody.position, rigidbody.rotation);
            }
            else
            {
                if (physicsDummy.transform.position.y < -10) //safety check in case object falls through the floor
                {
                    physicsDummy.transform.position = new Vector3(0, 2, 0);
                    physicsDummy.velocity = Vector3.zero;
                    physicsDummy.angularVelocity = Vector3.zero;
                }

                rigidbody.Move(physicsDummy.position, physicsDummy.rotation);
            }

            previousPosition = position;
            previousRotation = rotation;
            yield return null;
        }
    }

    #endregion

}
