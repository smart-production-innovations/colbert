using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

//single elements in the hierarchy list on the metadata panel
public class MetadataPanelHierarchyElement : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textField;

    public void Initialize(string text)
    {
        textField.text = $"{text}";
    }
}
