using GLTFast;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

//instantiate model into scene after loading from file with ModelLoader
public class ModelInstantiator : IInstantiator
{
    private IGltfReadable gltf;
    private Transform parent;
    private GameObject nodePrefab;
    private MeshFilter meshNodePrefab;
    private Dictionary<uint, GameObject> nodes = new Dictionary<uint, GameObject>();

    public ModelInstantiator(IGltfReadable gltf, Transform parent, GameObject nodePrefab, MeshFilter meshNodePrefab)
    {
        this.gltf = gltf;
        this.parent = parent;
        this.nodePrefab = nodePrefab;
        this.meshNodePrefab = meshNodePrefab;
    }

    public void AddCamera(uint nodeIndex, uint cameraIndex)
    {
        Debug.LogWarning("ModelInstantiator - support for cameras not implemented");
    }
    public void AddLightPunctual(uint nodeIndex, uint lightIndex)
    {
        Debug.LogWarning("ModelInstantiator - support for lights not implemented");
    }
    public void AddAnimation(AnimationClip[] animationClips)
    {
        Debug.LogWarning("ModelInstantiator - support for animationClips not implemented");
    }

    public void BeginScene(string name, uint[] rootNodeIndices)
    {
        nodes.Clear();
        if (parent != null)
            parent.hierarchyCapacity += 5000;
    }

    public void EndScene(uint[] rootNodeIndices) { }

    public void SetNodeName(uint nodeIndex, string name)
    {
        GameObject go = nodes[nodeIndex];

        //var node = gltf.GetSourceNode((int)nodeIndex);
        go.name = $"{name ?? $"Node-{nodeIndex}"}";
        //go.name = $"{(data != null ? "* " : "")}{(node.mesh >= 0 ? "[] " : "")}{name ?? $"Node-{nodeIndex}"}";
    }

    public void CreateNode(uint nodeIndex, uint? parentIndex, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        Transform parent = parentIndex.HasValue ? nodes[parentIndex.Value].transform : this.parent;
        Vector3 position = localPosition;
        Quaternion rotation = localRotation;
        if (parent != null)
        {
            position = parent.TransformPoint(localPosition);
            rotation = parent.rotation * localRotation;
        }

        GameObject go;

        var node = gltf.GetSourceNode((int)nodeIndex);
        if (node.mesh >= 0)
        {
            go = GameObject.Instantiate(meshNodePrefab, position, rotation, parent).gameObject;
        }
        else
        {
            go = GameObject.Instantiate(nodePrefab, position, rotation, parent);
        }

        go.transform.localScale = localScale;
        nodes[nodeIndex] = go;

        ModelNode modelNode = go.GetComponent<ModelNode>();
        modelNode.Initialize();
    }

    public void AddPrimitive(uint nodeIndex, string meshName, MeshResult meshResult, uint[] joints = null, uint? rootJoint = null, float[] morphTargetWeights = null, int primitiveNumeration = 0)
    {
        GameObject meshGo;
        if (primitiveNumeration == 0)
        {
            // Use Node GameObject for first Primitive
            meshGo = nodes[nodeIndex];
        }
        else
        {
            meshGo = GameObject.Instantiate(meshNodePrefab, nodes[nodeIndex].transform).gameObject;
            meshGo.name = meshName;
        }

        Renderer renderer;

        var hasMorphTargets = meshResult.mesh.blendShapeCount > 0;
        if (joints == null && !hasMorphTargets)
        {
            MeshFilter mf = meshGo.GetComponent<MeshFilter>();
            mf.sharedMesh = meshResult.mesh;
            var mr = meshGo.GetComponent<MeshRenderer>();
            renderer = mr;
        }
        else
        {
            Debug.LogWarning("ModelInstantiator - support for skinned meshes not implemented");
            return;
        }

        var materials = new Material[meshResult.materialIndices.Length];
        for (var index = 0; index < materials.Length; index++)
        {
            var material = gltf.GetMaterial(meshResult.materialIndices[index]) ?? gltf.GetDefaultMaterial();
            materials[index] = material;
        }

        renderer.sharedMaterials = materials;
        renderer.materials = renderer.materials;
    }

    public void AddPrimitiveInstanced(uint nodeIndex, string meshName, MeshResult meshResult, uint instanceCount, NativeArray<Vector3>? positions, NativeArray<Quaternion>? rotations, NativeArray<Vector3>? scales, int primitiveNumeration = 0)
    {
        Debug.LogWarning("ModelInstantiator - support for instancing not implemented");
    }
}
