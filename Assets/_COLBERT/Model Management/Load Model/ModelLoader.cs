using GLTFast;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Defective.JSON;
using System.IO;
using System.Xml;
using System;
using Unity.Burst;

//load gltf models + corresponding json/xml metadata
public static class ModelLoader
{
    private static string hiddenIdLabel = "_id_"; //additional metadata entry that holds a copy of the entry, which is used to match with the hierarchy node in the model (the id entry that is defined in the config)
    public static string HiddenIdLabel => hiddenIdLabel;

    //load a model with or without metadata
    public static async Task<GameObject> LoadModel(byte[] modelBytes, byte[] metadataBytes, string metadataExtension, Dictionary<string, string> metadataParameter, Transform parent, GameObject nodePrefab, MeshFilter meshNodePrefab, Material material, Material materialDouble)
    {
        GltfImport gltf = new GltfImport(null, null, new GlowMaterialGenerator(material, materialDouble), null);
        var modelTask = gltf.LoadGltfBinary(modelBytes);
        var metadataTask = LoadMetadata(metadataBytes, metadataExtension, metadataParameter);

        bool success = await modelTask;
        if (!success)
            Debug.LogError("NetworkModel - could not load gltf");

        success = await gltf.InstantiateSceneAsync(new ModelInstantiator(gltf, parent, nodePrefab, meshNodePrefab));
        if (!success)
            Debug.LogError("NetworkModel - could not instantiate gltf");

        Transform root = null;
        if (parent.childCount == 0)
        {
            Debug.LogError("could not load gltf, no objects were created");
            return null;
        }
        else if (parent.childCount == 1)
        {
            root = parent.GetChild(0);
        }
        else //several root objects: choose the one with a mesh (for example when imported from keyshot: a second object is added with a camera which will be ignored)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).GetComponentsInChildren<MeshRenderer>().Length > 0)
                {
                    root = parent.GetChild(i);
                    break;
                }
            }
        }

        //flatten hierarchy: sometimes the root is inside additional gameobjects which results in an unnecessary steep hierarchy, these will be removed
        ModelNode rootNode = root.gameObject.GetComponent<ModelNode>();
        while (rootNode.Children.Count == 1 && !rootNode.gameObject.TryGetComponent(out MeshFilter _))
        {
            Debug.Log($"removed unnecessary root of loaded model '{rootNode.name}'");
            ModelNode nodeToDelete = rootNode;
            rootNode = rootNode.Children[0];
            rootNode.transform.parent = nodeToDelete.Parent == null ? null : nodeToDelete.Parent.transform;
            GameObject.Destroy(nodeToDelete.gameObject);
            rootNode.Initialize();
            root = rootNode.transform;
        }

        await Task.Yield();
        await CombineSubMeshes(root);
        await Task.Yield(); //without this delay, GetComponentsInChildren() in AddColliders finds MeshFilters that were destroyed before in the same frame
        await AddColliders(root.gameObject); //await, if colliders are needed instantly
        await Task.Yield();
#pragma warning disable CS4014
        Dictionary<string, string>[] metadata = await metadataTask;
        AddMetadata(root.gameObject, metadata); //no need to await, metadata will be available later
#pragma warning restore CS4014

        await Task.Yield();

        return rootNode.gameObject;
    }

    //load metadata from file into a dictionary
    private static async Task<Dictionary<string, string>[]> LoadMetadata(byte[] bytes, string extension, Dictionary<string, string> metadataParameter)
    {
        if (bytes == null || string.IsNullOrEmpty(extension))
            return null;

        if (metadataParameter == null)
        {
            Debug.LogError($"could not load metadata (could not load metadata config)");
            return null;
        }
        string jsonPathToList = null;
        string jsonIdEntry = null;
        string xmlLabelName = null;
        string xmlEntryName = null;
        string xmlIdEntry = null;

        metadataParameter.TryGetValue("json-path-to-list", out jsonPathToList);
        metadataParameter.TryGetValue("json-id-entry", out jsonIdEntry);
        metadataParameter.TryGetValue("xml-label-name", out xmlLabelName);
        metadataParameter.TryGetValue("xml-entry-name", out xmlEntryName);
        metadataParameter.TryGetValue("xml-id-entry", out xmlIdEntry);

        Dictionary<string, string>[] entries;
        if (extension.Equals(".xml", StringComparison.InvariantCultureIgnoreCase))
        {
            entries = await Task.Run(() => LoadMetadataXml(bytes, SplitIdEntries(xmlIdEntry), xmlLabelName, xmlEntryName));
        }
        else if (extension.Equals(".json", StringComparison.InvariantCultureIgnoreCase))
        {
            entries = await Task.Run(() => LoadMetadataJson(bytes, SplitIdEntries(jsonIdEntry), jsonPathToList));
        }
        else
        {
            Debug.LogWarning($"LoadMetadata - metadata with unknown extension '{extension}'");
            return null;
        }

        if (entries != null)
            Debug.Log($"{entries.Length} metadata entries found");

        return entries;
    }

    private static Dictionary<string, string>[] LoadMetadataJson(byte[] bytes, string[] idEntries, string pathToList)
    {
        if (idEntries == null || pathToList == null)
        {
            Debug.LogError($"could not load metadata - parameters are null");
            return null;
        }

        string str = Encoding.Default.GetString(bytes);
        JSONObject baseObj = new JSONObject(str);

        string[] pathToListArray = pathToList.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        JSONObject obj = baseObj;
        foreach (var dir in pathToListArray)
        {
            if (!obj.HasField(dir))
            {
                Debug.Log($"could not find metadata list in json file (json entry '{dir}' not found)");
                return null;
            }
            obj = obj.GetField(dir);
        }

        if (!obj.isArray)
        {
            Debug.Log($"specified path in config file is not a list");
            return null;
        }

        List<JSONObject> list = obj.list;

        string idEntry = null;
        for (int i = 0; i < idEntries.Length; i++)
        {
            JSONObject jsonEntry = list[0];
            for (int j = 0; j < jsonEntry.list.Count; j++)
            {
                string key = jsonEntry.keys[j];

                if (key.Equals(idEntries[i]))
                {
                    idEntry = idEntries[i];
                    break;
                }
            }
        }
        if (idEntry == null)
        {
            Debug.Log($"could not find specified id entry for matching metadata");
            return null;
        }

        Dictionary<string, string>[] entries = new Dictionary<string, string>[list.Count];
        for (int i = 0; i < entries.Length; i++)
        {
            Dictionary<string, string> entry = new Dictionary<string, string>();
            entries[i] = entry;
            JSONObject jsonEntry = list[i];
            for (int j = 0; j < jsonEntry.list.Count; j++)
            {
                string key = jsonEntry.keys[j];
                string value = jsonEntry.GetField(key).stringValue;
                entry.Add(key, value);

                if (key.Equals(idEntry))
                    entry.Add(hiddenIdLabel, value);
            }
        }
        return entries;
    }

    private static Dictionary<string, string>[] LoadMetadataXml(byte[] bytes, string[] idEntries, string labelName, string entryName)
    {
        if (idEntries == null || labelName == null || entryName == null)
        {
            Debug.LogError($"could not load metadata - parameters are null");
            return null;
        }

        try
        {
            List<Dictionary<string, string>> entries = new List<Dictionary<string, string>>();

            using (MemoryStream stream = new MemoryStream(bytes))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                reader.ReadToDescendant(labelName);
                List<string> labels = new List<string>();
                using (var sectionReader = reader.ReadSubtree())
                {
                    //sectionReader.ReadToDescendant("ColumnName");
                    sectionReader.Read();
                    sectionReader.Read();
                    while (sectionReader.Read())
                    {
                        if (sectionReader.HasValue)
                        {
                            labels.Add(sectionReader.Value.Trim());
                        }
                    }
                }

                string idEntry = null;
                for (int i = 0; i < idEntries.Length; i++)
                {
                    for (int j = 0; j < labels.Count; j++)
                    {
                        if (labels[j].Equals(idEntries[i]))
                        {
                            idEntry = idEntries[i];
                            break;
                        }
                    }
                }
                if (idEntry == null)
                {
                    Debug.Log($"could not find specified id entry for matching metadata");
                    return null;
                }

                while (reader.ReadToNextSibling(entryName))
                {
                    Dictionary<string, string> entry = new Dictionary<string, string>();
                    entries.Add(entry);
                    using (var sectionReader = reader.ReadSubtree())
                    {
                        //sectionReader.ReadToDescendant("Attribute");
                        sectionReader.Read();
                        sectionReader.Read();
                        XmlNodeType previousNodeType = XmlNodeType.None;
                        int i = 0;
                        while (sectionReader.Read())
                        {
                            if (sectionReader.HasValue)
                            {
                                entry.Add(labels[i], sectionReader.Value.Trim());
                                i++;
                            }
                            else if (sectionReader.NodeType == XmlNodeType.EndElement && previousNodeType == XmlNodeType.Element) //empty element
                            {
                                //entry.Add(labels[i], ""); //add empty
                                i++;
                            }
                            previousNodeType = sectionReader.NodeType;
                        }
                    }
                    string id = entry[idEntry];
                    if (Path.HasExtension(id))
                        id = Path.GetFileNameWithoutExtension(id);
                    entry.Add(hiddenIdLabel, id);
                }
            }
            return entries.ToArray();
        }
        catch
        {
            Debug.LogError($"error in parsing xml metadata");
            return null;
        }
    }

    //split id entry definitions from config file by ';'
    private static string[] SplitIdEntries(string idEntries)
    {
        if (idEntries == null)
            return null;

        string[] entries = idEntries.Split(';');
        for (int i = 0; i < entries.Length; i++)
            entries[i] = entries[i].Trim();
        return entries;
    }

    //multicolor objects are loaded as individual objects, those will be combined into one mesh as submeshes; especially important for meshcolliders to work properly; recursive
    private static int counter = 0;
    private static async Task CombineSubMeshes(Transform tr)
    {
        List<Transform> trs = new List<Transform>();
        for (int i = 0; i < tr.childCount; i++)
        {
            trs.Add(tr.GetChild(i));
        }

        if (tr.TryGetComponent(out MeshFilter mf))
        {
            //Find submeshes
            List<MeshFilter> mfs = new List<MeshFilter>();
            mfs.Add(mf);
            for (int i = 0; i < tr.childCount; i++)
            {
                Transform child = tr.GetChild(i);
                if (child.TryGetComponent(out MeshFilter mfc) && child.name.StartsWith($"{tr.name}_"))//&& child.name.StartsWith("Primitive_"))
                {
                    mfs.Add(mfc);
                    trs.Remove(child);
                }
            }

            if (mfs.Count > 1)
            {
                //Combine submeshes
                CombineInstance[] combine = new CombineInstance[mfs.Count];
                Material[] materials = new Material[mfs.Count];
                for (int i = 0; i < mfs.Count; i++)
                {
                    combine[i].mesh = mfs[i].sharedMesh;
                    materials[i] = mfs[i].GetComponent<MeshRenderer>().sharedMaterial;
                }
                mf.sharedMesh = new Mesh();
                mf.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mf.sharedMesh.CombineMeshes(combine, false, false);
                mf.GetComponent<MeshRenderer>().sharedMaterials = materials;
                //Destroy submesh objects (Primitive_)
                for (int i = 1; i < mfs.Count; i++)
                {
                    if (mfs[i].transform.childCount > 0)
                        Debug.LogWarning($"submesh primitive has children which get destroyed with the submesh!");
                    GameObject.Destroy(mfs[i].gameObject);
                }
                counter++;
            }
        }
        if (counter > 30) //at most 30 per frame to reduce hicups
        {
            counter = 0;
            await Task.Yield();
        }
        //continue with child objects
        for (int i = 0; i < trs.Count; i++)
        {
            await CombineSubMeshes(trs[i]);
        }
    }

    //match and add metadata from dictionary to model nodes
    private static async Task AddMetadata(GameObject model, Dictionary<string, string>[] metadata)
    {
        if (metadata == null || model == null)
            return;

        ModelNode[] nodes = model.GetComponentsInChildren<ModelNode>();
        string[] nodenames = new string[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
            nodenames[i] = nodes[i].name;
        Dictionary<string, string>[] matches = new Dictionary<string, string>[nodes.Length];

        await Task.Run(() =>
        {
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = System.Environment.ProcessorCount - 2;
            Parallel.For(0, nodes.Length, parallelOptions, (i) =>
            {
                int matchLength = 0;
                string name = nodenames[i];
                foreach (Dictionary<string, string> entry in metadata)
                {
                    string id = entry[hiddenIdLabel];
                    if (name.Contains(id, StringComparison.InvariantCultureIgnoreCase) && id.Length > matchLength) //find the longest match!
                    {
                        matches[i] = entry;
                        matchLength = id.Length;
                    }
                }
            });
        });

        for (int i = 0; i < nodes.Length; i++)
            if (matches[i] != null)
                nodes[i].InitMetadata(matches[i]);
    }

    //flatten hierarchy (remove nodes that contain no mesh and only have a single child
    private static Transform ReduceHierarchy(Transform tr)
    {
        while (tr.childCount == 1 && !tr.TryGetComponent(out MeshFilter _))
        {
            Transform child = tr.GetChild(0);
            child.parent = tr.parent;
            child.name = $"{tr.name}_{child.name}"; //combine names to not loose information for metadata matching
            GameObject.Destroy(tr.gameObject);
            tr = child;
        }
        //continue with child objects
        for (int i = 0; i < tr.childCount; i++)
        {
            ReduceHierarchy(tr.GetChild(i));
        }
        return tr;
    }

    //add meshcolliders to all nodes with a mesh, all meshes are baked in a job for physics use before assigning or else they are baked automatically on the main thread during assignment which results in a short freeze
    private static async Task AddColliders(GameObject model)
    {
        MeshFilter[] meshfilters = model.GetComponentsInChildren<MeshFilter>();

        if (meshfilters.Length == 0)
        {
            Debug.LogWarning($"could not add colliders, {model.name} has no MeshFilters in its children", model);
            return;
        }

        NativeArray<int> meshIds = new NativeArray<int>(meshfilters.Length, Allocator.Persistent);
        for (int i = 0; i < meshfilters.Length; ++i)
        {
            meshIds[i] = meshfilters[i].sharedMesh.GetInstanceID();
        }
        var job = new BakeJob(meshIds, meshfilters[0].GetComponent<MeshCollider>().cookingOptions);
        JobHandle jobHandle = job.Schedule(meshIds.Length, 20);

        while (!jobHandle.IsCompleted)
        {
            await Task.Yield();
        }
        jobHandle.Complete();

        meshIds.Dispose();

        for (int i = 0; i < meshfilters.Length; i++)
        {
            meshfilters[i].GetComponent<MeshCollider>().sharedMesh = meshfilters[i].sharedMesh;
        }
    }

    //job for baking meshes for physics in parallel on worker threads
    [BurstCompile]
    private struct BakeJob : IJobParallelFor
    {
        private NativeArray<int> meshIds;
        private MeshColliderCookingOptions cookingOptions;
        public BakeJob(NativeArray<int> meshIds, MeshColliderCookingOptions cookingOptions)
        {
            this.meshIds = meshIds;
            this.cookingOptions = cookingOptions;
        }

        public void Execute(int index)
        {
            Physics.BakeMesh(meshIds[index], false, cookingOptions);
        }
    }
}
