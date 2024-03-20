using System.IO;
using UnityEngine;

//open userdata folder in windows file browser
public class OpenDataFolder : MonoBehaviour
{
    public void OnButtonClick()
    {
        string pathStr = Path.Combine(ConfigHelper.modelDirectory);
        Application.OpenURL(pathStr);
    }
}
