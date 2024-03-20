using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIEnvironmentListElement : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI label;
    [SerializeField]
    private Color defaultTextColor = Color.black;
    [SerializeField]
    private Color selectedTextColor = Color.white;

    private NetworkEnvironmentManager manager = null;
    private string environmentName = null;

    public void Initialize(NetworkEnvironmentManager manager, string name, bool available)
    {
        this.manager = manager;
        environmentName = name;
        label.text = name;

        bool ison = name == manager.ActiveEnvironment;
        Toggle button = GetComponent<Toggle>();
        button.SetIsOnWithoutNotify(ison);
        button.interactable = available;
        label.color = ison ? selectedTextColor : defaultTextColor;
    }

    public void ChangeEnvironment()
    {
        if (!GetComponent<Toggle>().isOn)
            return;

        Debug.Log($"Button ChangeEnvironment to '{environmentName}'");
        manager.ChangeToEnvironment(environmentName);
    }
}
