using Unity.Netcode;

//parameters of a model file
[System.Serializable]
public class ModelFileData
{
    public string modelName = null; //filename
    public string metadataName = null; //name of metadata file, if available
    public bool everyone; //for multiplayer: is the file available on every client?

    public bool Spawnable => everyone || (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient);
    public bool HasMetadata => !string.IsNullOrEmpty(metadataName);

    public override bool Equals(object obj)
    {
        if (obj.GetType() != typeof(ModelFileData))
        {
            return false;
        }
        ModelFileData other = obj as ModelFileData;
        return modelName.Equals(other.modelName, System.StringComparison.InvariantCultureIgnoreCase) &&
            metadataName.Equals(other.metadataName, System.StringComparison.InvariantCultureIgnoreCase);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }
}
