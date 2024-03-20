using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

//on every hierarchy node of a loaded model,
//holds properties such as its parent/child-nodes, original local position/rotation relative to its parent node, local bounds, metadata
public class ModelNode : MonoBehaviour
{
    private ModelNode parent = null;
    private Vector3 position;
    private Quaternion rotation;
    private Matrix4x4 localMatrix;
    private List<ModelNode> children = new List<ModelNode>();
    private Transform parentTransform = null;
    private Dictionary<string, string> metadata = null;
    private Bounds bounds;

    //public NetworkModelExplorer explorer;

#if UNITY_EDITOR //workaround to show dictionary values in inspector
    [System.Serializable]
    private struct KeyValuePair
    {
        public string key;
        public string value;
        public KeyValuePair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
    [SerializeField] private List<KeyValuePair> metadataList = new List<KeyValuePair>();
#endif

    public ModelNode Parent => parent;
    public Vector3 Position => position;
    public Quaternion Rotation => rotation;
    public Matrix4x4 LocalMatrix => localMatrix;
    public List<ModelNode> Children => children;
    public Dictionary<string, string> Metadata => metadata;
    public Bounds Bounds => bounds;

    private void OnDestroy()
    {
        if (parent != null)
            parent.children.Remove(this);
    }

    //initial node properties
    public void Initialize()
    {
        transform.GetLocalPositionAndRotation(out position, out rotation);
        localMatrix = Matrix4x4.TRS(position, rotation, transform.localScale);

        parent = transform.parent?.GetComponent<ModelNode>();
        parent?.children.Add(this); //add self to parent children!
        parentTransform = parent?.transform;
    }

    public void OverrideParent(Transform parent) //for rootnode -> parentTransform is the NetworkModel object
    {
        parentTransform = parent;
    }

    //calculate local bounds
    public void CalculateBounds()
    {
        Pose nodePose = transform.GetWorldPose();
        transform.SetWorldPose(Pose.identity); //move to origin to get local bounds with meshrenderer (meshrenderer calculates global bounds!), reset pose afterwards

        MeshRenderer[] mrs = GetComponentsInChildren<MeshRenderer>();
        if (mrs != null && mrs.Length > 0)
        {
            bounds = mrs[0].bounds;
            foreach (MeshRenderer mr in mrs)
            {
                bounds.Encapsulate(mr.bounds);
            }
        }

        transform.SetWorldPose(nodePose);
    }

    public void InitMetadata(Dictionary<string, string> metadata)
    {
        this.metadata = metadata;

#if UNITY_EDITOR
        foreach (var entry in metadata)
        {
            metadataList.Add(new KeyValuePair(entry.Key, entry.Value));
        }
#endif
    }

    //reset to original pose in model hierarchy
    public void ResetNode()
    {
        ResetParent();

        if (parentTransform != null)
        {
            Vector3 position = parentTransform.TransformPoint(this.position);
            Quaternion rotation = parentTransform.rotation * this.rotation;

            transform.SetPositionAndRotation(position, rotation);
        }
    }

    public void ResetParent()
    {
        if (transform.parent != parentTransform)
            transform.parent = parentTransform;
    }

}
