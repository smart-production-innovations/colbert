using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Startscreen : MonoBehaviour
{
    [Serializable]
    private struct Panel
    {
        public GameObject panel;
        public float seconds;
    }

    [SerializeField]
    private Panel[] panels;

    [SerializeField]
    [HideInInspector]
    private string scenename = null;

    //show fields for scene assignment only in editor, copy names to hidden string variables for runtime loading
#if UNITY_EDITOR
    [SerializeField]
    private UnityEditor.SceneAsset scene;
    private void OnValidate()
    {
        scenename = scene != null ? scene.name : null;
    }
#endif

    private int activePanel = -1;


    private void Start()
    {
        activePanel = -1;
        Next();
    }

    
    private void Next()
    {
        StopAllCoroutines();

        if (activePanel == panels.Length - 1)
        {
            SceneManager.LoadScene(scenename);
        }
        else
        {
            activePanel++;
            for (int i = 0; i < panels.Length; i++)
                panels[i].panel.SetActive(i == activePanel);

            float seconds = panels[activePanel].seconds;
            if (seconds >= 0)
            {
                StartCoroutine(SwitchDelayed(seconds));
            }
            else
            {
                Button button = panels[activePanel].panel.GetComponentInChildren<Button>();
                if (button != null)
                    button.onClick.AddListener(Next);
            }
        }
    }

    private IEnumerator SwitchDelayed(float seconds)
    {
        yield return  new WaitForSecondsRealtime(seconds);
        Next();
    }


}
