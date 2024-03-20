using UnityEngine;
using UnityEngine.UI;

//loading screen overlay for loading models/modelslist
public class Loading : MonoBehaviour
{
    private bool isLoading = false;

    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private Image background;
    [SerializeField] private Image loadingImg;

    private void OnEnable()
    {
        DisableLoading();
    }

    private void Update()
    {
        if (isLoading)
        {
            background.enabled = true;
            loadingImg.enabled = true;
            loadingImg.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            background.enabled = false;
            loadingImg.enabled = false;
        }
    }

    public void EnableLoading()
    {
        isLoading = true;
    }

    public void DisableLoading()
    {
        isLoading = false;
    }

}
