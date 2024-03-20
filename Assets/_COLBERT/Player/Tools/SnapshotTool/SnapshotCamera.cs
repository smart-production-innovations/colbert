using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

//take a snapshot of the current view (or in vr with the virtual camera) and save as png on disk
public class SnapshotCamera : MonoBehaviour
{
    [SerializeField]
    private Camera photoCamera;

    [SerializeField]
    private RenderTexture texture;

    [SerializeField]
    private InputActionReference trigger;

    public bool isActive;

    [SerializeField]
    private bool isNonXRPlayer = false;
    private SnapshotIndicator[] indicators; //for nonXRLaser

    [SerializeField]
    private GameObject result;

    [SerializeField]
    private XRSphereButton sphereBtn;

    public int width; 
    public int height;

    private void Awake()
    {
        if (isNonXRPlayer)
            indicators = FindObjectsByType<SnapshotIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (isNonXRPlayer && trigger.action.WasPressedThisFrame()) SavePNG();

        if (isActive && !isNonXRPlayer && trigger && trigger.action.WasReleasedThisFrame() && !sphereBtn.isHovered)
            SavePNG();
    }

    public void SavePNG()
    {
        RenderTexture mRt = new RenderTexture(width, height, texture.depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        mRt.antiAliasing = texture.antiAliasing;

        var tex = new Texture2D(mRt.width, mRt.height, TextureFormat.ARGB32, false);
        photoCamera.targetTexture = mRt;
        photoCamera.Render();
        RenderTexture.active = mRt;

        tex.ReadPixels(new Rect(0, 0, mRt.width, mRt.height), 0, 0);
        tex.Apply();

        string targetDirectory = ConfigHelper.Path(ConfigHelper.snapshotDirectory);

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        string path = Path.Combine(targetDirectory, $"snapshot_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("Saved file to: " + path);

        DestroyImmediate(tex);

        photoCamera.targetTexture = texture;
        photoCamera.Render();
        RenderTexture.active = texture;

        DestroyImmediate(mRt);

        //Show result
        StartCoroutine(ResultCoroutine());
    }

    private IEnumerator ResultCoroutine()
    {
        result.SetActive(true);
        yield return new WaitForSeconds(2);
        result.SetActive(false);
        if (sphereBtn)
        {
            DeactivateSnapshotTool();
            sphereBtn.SetPassivDeactive();
        }
    }

    public void SetActive(bool isActive)
    {
        if (isActive)
            ActivateSnapshotTool();
        else
            DeactivateSnapshotTool();
    }

    public void ActivateSnapshotTool()
    {
        photoCamera.gameObject.SetActive(true);
        isActive = true;

        if (indicators != null)
            foreach (var indicator in indicators)
                indicator.gameObject.SetActive(true);
    }

    public void DeactivateSnapshotTool()
    {
        photoCamera.gameObject.SetActive(false);
        isActive = false;

        if (indicators != null)
            foreach (var indicator in indicators)
                indicator.gameObject.SetActive(false);
    }
}
