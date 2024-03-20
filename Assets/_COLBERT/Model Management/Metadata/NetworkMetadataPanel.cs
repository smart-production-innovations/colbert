using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//main script on the metadata panel
public class NetworkMetadataPanel : NetworkBehaviour
{
    [SerializeField]
    private GameObject metadataViewport;
    [SerializeField]
    private GameObject hierarchyViewport;

    [SerializeField]
    private Transform metadataRoot;
    private List<MetadataPanelElement> metadataEntries = new List<MetadataPanelElement>();

    [SerializeField]
    private Transform hierarchyRoot;
    private List<MetadataPanelHierarchyElement> hierarchyEntries = new List<MetadataPanelHierarchyElement>();

    [SerializeField]
    private GameObject childDummy; //the hierarchy list element that indicates that the interactable has children

    private NetworkModelInteractable interactable = null;
    private NetworkModelMetadata manager = null;
    private bool xr;
    private NetworkVariable<NetworkObjectReference> interactableReference = new NetworkVariable<NetworkObjectReference>();
    private NetworkVariable<NetworkObjectReference> managerReference = new NetworkVariable<NetworkObjectReference>();
    private NetworkVariable<bool> xrnet = new NetworkVariable<bool>();

    public bool XR => xr;


    private void Awake()
    {
        metadataRoot.GetComponentsInChildren(true, metadataEntries);
        hierarchyRoot.GetComponentsInChildren(true, hierarchyEntries);
    }

    private void OnDisable()
    {
        if (manager.Panels.Contains(this))
            manager.Panels.Remove(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            interactableReference.Value = new NetworkObjectReference(interactable.NetworkObject);
            managerReference.Value = new NetworkObjectReference(manager.NetworkObject);
            xrnet.Value = xr;
        }
        else if (IsClient)
        {
            Initialize(interactableReference.Value, managerReference.Value, xrnet.Value);
        }
    }

    public void Initialize(NetworkObjectReference interactableReference, NetworkObjectReference managerReference, bool xr)
    {
        StartCoroutine(WaitForData(interactableReference, managerReference, xr));
    }

    public void Initialize(NetworkModelInteractable interactable, NetworkModelMetadata manager, bool xr)
    {
        this.xr = xr;
        this.manager = manager;
        this.interactable = interactable;

        if (!manager.Panels.Contains(this))
            manager.Panels.Add(this);


        //set hierarchy data:
        //determine hierarchy list (self up to root)
        List<ModelNode> hierarchy = new List<ModelNode>();
        hierarchy.Add(interactable.Node);
        while (hierarchy[0].Parent != null)
            hierarchy.Insert(0, hierarchy[0].Parent);

        //create list on the infopanel
        int i = 0;
        foreach (var entry in hierarchy)
        {
            if (i >= hierarchyEntries.Count)
                hierarchyEntries.Add(Instantiate(hierarchyEntries[0], hierarchyRoot));
            hierarchyEntries[i].Initialize(hierarchy[i].name);
            hierarchyEntries[i].gameObject.SetActive(true);
            i++;
        }
        for (; i < hierarchyEntries.Count; i++)
        {
            hierarchyEntries[i].gameObject.SetActive(false);
        }

        //enable child-dummy at list end, if the interactable has children
        NetworkModelExplorer explorer = interactable.Model.GetComponent<NetworkModelExplorer>();
        if (explorer.DisassemblePossible(interactable))
            childDummy.SetActive(true);
        else
            childDummy.SetActive(false);
        
        //set metadata:
        Dictionary<string, string> metadata = interactable.Node.Metadata;
        if (metadata == null)
        {
            metadataViewport.SetActive(false);
        }
        else
        {
            i = 0;
            foreach (var entry in metadata)
            {
                if (i >= metadataEntries.Count)
                    metadataEntries.Add(Instantiate(metadataEntries[0], metadataRoot));
                metadataEntries[i].Initialize(entry.Key, entry.Value);
                metadataEntries[i].gameObject.SetActive(true);

                if (entry.Key == ModelLoader.HiddenIdLabel) //hide the hidden label
                    metadataEntries[i].gameObject.SetActive(false);

                i++;
            }
            for (; i < metadataEntries.Count; i++) //disable surplus list elements
            {
                metadataEntries[i].gameObject.SetActive(false);
            }
        }
    }

    //wait for the corresponding interactable to be initialized before initializing the panel
    private IEnumerator WaitForData(NetworkObjectReference interactableReference, NetworkObjectReference managerReference, bool xr)
    {
        while (interactable == null)
        {
            if (interactableReference.TryGet(out NetworkObject noInt) && managerReference.TryGet(out NetworkObject noMan))
            {
                NetworkModelInteractable interactable = noInt.GetComponent<NetworkModelInteractable>();
                if (interactable.Initialized)
                    Initialize(interactable, noMan.GetComponent<NetworkModelMetadata>(), xr);
            }
            yield return null;
        }
    }

}
