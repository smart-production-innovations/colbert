using System.IO;
using UnityEngine;
using UnityEngine.UI;

//loads logo from _config folder and activates logo shield when found
public class LogoLoader : MonoBehaviour
{
    [SerializeField]
    private RawImage logoContainer;

    [SerializeField]
    private GameObject shieldObject;

    private void Start()
    {
        string path1 = ConfigHelper.Path(ConfigHelper.logoFileA);
        string path2 = ConfigHelper.Path(ConfigHelper.logoFileB);
        //search in file path if there is a logo with .png or .jpg
        if (File.Exists(path1))
        {
            LoadAndScaleLogo(path1);
        }
        else if (File.Exists(path2))
        {
            LoadAndScaleLogo(path2);
        }
    }

    private void LoadAndScaleLogo(string filePath)
    {
        shieldObject.SetActive(true);
        logoContainer.texture = LoadPNG(filePath);
        AspectRatioFitter fitter = logoContainer.GetComponent<AspectRatioFitter>();
        fitter.aspectRatio = (float)logoContainer.texture.width / (float)logoContainer.texture.height;
    }
    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }
}
