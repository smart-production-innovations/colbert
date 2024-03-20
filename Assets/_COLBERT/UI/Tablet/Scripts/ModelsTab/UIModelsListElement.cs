using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIModelsListElement : MonoBehaviour
{
    private ModelFileData currFile = null;

    [SerializeField] private TextMeshProUGUI fileName;
    [SerializeField] private TextMeshProUGUI polyCount;
    
    [SerializeField] private Button btnFileDelete;
    [SerializeField] private Button btnSpawnInScene;

    [SerializeField] private Image iconMetadata;

    [SerializeField] private Loading loadingObj;

    private NetworkModelsManager modelsManager = null;


    public void Initialize(ModelFileData file, NetworkModelsManager modelsManager)
    {
        this.modelsManager = modelsManager;
        currFile = file;
        fileName.text = currFile.modelName;
        ToggleActives();
    }

    private void Update()
    {
        if (currFile != null && modelsManager.IsLoaded(currFile.modelName, out _)) //maybe Change in the future
        {
            btnFileDelete.gameObject.SetActive(true);
            btnSpawnInScene.gameObject.SetActive(false);
        }
        else
        {
            btnFileDelete.gameObject.SetActive(false);
            btnSpawnInScene.gameObject.SetActive(true);
        }
    }
    public async void SelectObj()
    {
        loadingObj.EnableLoading();
        await modelsManager.LoadModel(currFile.modelName, currFile.metadataName, GetComponentInParent<Player>());
        ToggleActives();
        loadingObj.DisableLoading();
    }

    public void DeleteInScene()
    {
        modelsManager.DeleteModel(currFile.modelName);
        ToggleActives();
    }

    private void ToggleActives()
    {
        btnSpawnInScene.interactable = currFile.Spawnable;
        iconMetadata.enabled = currFile.HasMetadata;

        if (modelsManager.IsLoaded(currFile.modelName, out NetworkModel model) && model.Initialized)
        {
            polyCount.gameObject.SetActive(true);

            int tricount = model.TriangleCount;
            if (tricount >= 1000000)
                polyCount.text = $"{Mathf.Round(tricount / 1000000f):0.0}M triangles";
            else if (tricount >= 10000)
                polyCount.text = $"{Mathf.Round(tricount / 1000f):0.0}k triangles";
            else
                polyCount.text = $"{tricount} triangles";
        }
        else
        {
            polyCount.gameObject.SetActive(false);
        }

    }
}
