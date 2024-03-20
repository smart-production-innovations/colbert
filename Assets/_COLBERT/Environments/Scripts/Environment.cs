using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

//initializes an environment that was loaded from a file:
//  calculate its bounds
//  generate meshcolliders
//  update reflection probe
//  resize placement area and teleport area
public class Environment : MonoBehaviour
{
    [SerializeField]
    private ReflectionProbe reflectionProbe;

    [SerializeField]
    private TeleportationArea teleportArea;

    [SerializeField]
    private GameObject placementArea;

    [SerializeField]
    public GameObject meshContainer;

    [SerializeField]
    private Transform physicsEnvironment;

    private void OnValidate()
    {
        if (physicsEnvironment.childCount == 0)
            GeneratePhysicCollider(meshContainer.transform);
    }

    public void Initialize(string name)
    {
        this.name = name;

        Bounds bounds = Encap(meshContainer.transform, new Bounds()); //recursion function, must get applied to a Bounds

        if (physicsEnvironment.childCount == 0)
            GeneratePhysicCollider(meshContainer.transform);

        reflectionProbe.size = bounds.size;
        reflectionProbe.center = bounds.center;
        reflectionProbe.RenderProbe();

        teleportArea.transform.localPosition = new Vector3(bounds.center.x, teleportArea.transform.localPosition.y, bounds.center.z);
        placementArea.transform.localPosition = new Vector3(bounds.center.x, teleportArea.transform.localPosition.y, bounds.center.z);

        teleportArea.transform.localScale = new Vector3(bounds.size.x / 10f, 0.1f, bounds.size.z / 10f);
        placementArea.transform.localScale = new Vector3(bounds.size.x / 10f, 0.1f, bounds.size.z / 10f);
    }

    //encapsulate all child meshes of parent in bounds
    private Bounds Encap(Transform parent, Bounds bounds)
    {
        foreach (Transform child in parent) //gets all the children in the transform
        {
            if (child.TryGetComponent(out MeshFilter mf)) //checks if it has a mesh
            {
                MeshCollider mc = child.gameObject.AddComponent<MeshCollider>();
                bounds.Encapsulate(mc.bounds); //makes bounds cover the child
            }
            bounds = Encap(child, bounds); //calls function on child to get all objects
        }
        return bounds;
    }

    //generate meshcolliders for all child meshes of parent
    private void GeneratePhysicCollider(Transform parent)
    {
        foreach (Transform child in parent) //gets all the children in the transform
        {
            if (child.TryGetComponent(out MeshFilter mf) && mf.gameObject.activeInHierarchy) //checks if it has a mesh and is active
            {
                GameObject physicsObj = new GameObject(mf.name);
                physicsObj.layer = physicsEnvironment.gameObject.layer;
                physicsObj.transform.parent = physicsEnvironment;
                physicsObj.transform.localScale = mf.transform.lossyScale;
                physicsObj.transform.position = mf.transform.position;
                physicsObj.transform.rotation = mf.transform.rotation;
                physicsObj.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            GeneratePhysicCollider(child);
        }
    }
}
