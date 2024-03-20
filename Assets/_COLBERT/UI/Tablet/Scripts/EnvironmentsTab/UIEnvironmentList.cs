using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//creates a button for each available environment
public class UIEnvironmentList : MonoBehaviour
{
    [SerializeField]
    private Transform buttonContainer;

    [SerializeField]
    private ToggleGroup toggleGroup;

    private NetworkEnvironmentManager manager = null;
    private List<UIEnvironmentListElement> elements = new List<UIEnvironmentListElement>();


    private void Awake()
    {
        manager = FindAnyObjectByType<NetworkEnvironmentManager>();
        buttonContainer.GetComponentsInChildren(true, elements);
    }

    private void OnEnable()
    {
        manager?.listUpdatedEvent.AddListener(DrawEnvironmentsList);
        manager?.UpdateAvailableEnvironments();
    }

    private void OnDisable()
    {
        manager?.listUpdatedEvent.RemoveListener(DrawEnvironmentsList);
    }

    public void DrawEnvironmentsList()
    {
        int i = 0;
        foreach (var env in manager.environments)
        {
            if (i >= elements.Count)
                elements.Add(Instantiate(elements[0], buttonContainer));

            if (toggleGroup != null)
                elements[i].GetComponent<Toggle>().group = toggleGroup;

            bool available = !manager.IsSpawned || manager.commonEnvironments.Contains(env);
            elements[i].Initialize(manager, env, available);
            elements[i].gameObject.SetActive(true);
            i++;
        }
        for (; i < elements.Count; i++)
        {
            elements[i].gameObject.SetActive(false);
        }
    }

}
