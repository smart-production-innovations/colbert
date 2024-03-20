using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

//single elements in the metadata list on the metadata panel
public class MetadataPanelElement : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI labelField;
    [SerializeField]
    private TextMeshProUGUI valueField;

    public void Initialize(string key, string value)
    {
        labelField.text = $"{key}:";
        valueField.text = $"{value}";
    }
}
