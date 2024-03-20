using UnityEngine;

//enable/disable controller menu (spherical buttons) depending on controller angle
public class ToggleControllerUI : MonoBehaviour
{
    [SerializeField] 
    private GameObject ControllerUI;
    [SerializeField] 
    private Transform controllerTransform;
    [SerializeField] 
    private Transform headsetTransform;

    public float maxDifference = 45;

    [SerializeField]
    private Handedness handedness;

    private enum Handedness
    {
        right, 
        left
    }

    private void Start()
    {
        ControllerUI.SetActive(false);
    }

    private void Update()
    {
        //TODO: Either Disassembly or Other UI

        if (controllerTransform && headsetTransform)
        {
            Vector3 controllerDirection = handedness == Handedness.left ? controllerTransform.right : -controllerTransform.right;
            if (Mathf.Abs(Vector3.Angle(controllerDirection, headsetTransform.forward) - 180) < maxDifference)
            {
                if (!ControllerUI.activeSelf)
                    ControllerUI.SetActive(true);
            }
            else
            {
                if (ControllerUI.activeSelf)
                    ControllerUI.SetActive(false);
            }
        }
        
    }
}
