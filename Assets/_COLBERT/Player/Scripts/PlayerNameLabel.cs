using TMPro;
using UnityEngine;

//show player name on a label above player head
public class PlayerNameLabel : MonoBehaviour
{
    [SerializeField]
    public  TextMeshProUGUI username;

    public void OnNameChange(string value)
    {
        this.username.text = value;
    }
}
