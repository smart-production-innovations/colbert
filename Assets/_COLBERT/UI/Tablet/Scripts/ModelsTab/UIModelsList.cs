using System.Collections.Generic;
using UnityEngine;

//create a list element for each available environment
public class UIModelsList : MonoBehaviour
{
    [SerializeField]
    private RectTransform listRoot;
    [SerializeField]
    private Loading loadingScreen;

    private List<UIModelsListElement> elements = new List<UIModelsListElement>();
    private NetworkModelsManager modelsManager = null;

    private void Awake()
    {
        modelsManager = FindAnyObjectByType<NetworkModelsManager>();
        listRoot.GetComponentsInChildren(true, elements);
    }

    private void OnEnable()
    {
        modelsManager?.listUpdatedEvent.AddListener(DrawFilesList);
        RefreshList();
    }

    private void OnDisable()
    { 
        modelsManager?.listUpdatedEvent.RemoveListener(DrawFilesList);
    }

    public void DrawFilesList(List<ModelFileData> fileList)
    {
        int i = 0;
        foreach (ModelFileData file in fileList)
        {
            if (i >= elements.Count)
                elements.Add(Instantiate(elements[0], listRoot));
            elements[i].Initialize(file, modelsManager);
            elements[i].gameObject.SetActive(true);
            i++;
        }
        for (; i < elements.Count; i++)
        {
            elements[i].gameObject.SetActive(false);
        }
    }

    public async void RefreshList()
    {
        Debug.Log("RefreshList");
        
        loadingScreen.EnableLoading();
        await modelsManager.UpdateList();
        loadingScreen.DisableLoading();
    }
}
