using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionNumber : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI label;

    private void Awake()
    {
        label.text = $"v{Application.version}";
    }
}
