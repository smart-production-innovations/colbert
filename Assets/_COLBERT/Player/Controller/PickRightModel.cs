using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

//loads the corresponding model to the used xr input device
public class PickRightModel : MonoBehaviour
{
    [SerializeField]
    public enum Handedness
    {
        Left,
        Right,
    }
    [SerializeField]
    public Handedness handedness;

    [System.Serializable]
    public struct ControllerObjects
    {
        public string name;
        public GameObject mesh;
        public Vector3 knobPosition;
        public Vector3 knobRotation;
    }

    [SerializeField]
    private GameObject directKnob;

    [SerializeField]
    private GameObject rayInteractor;

    [SerializeField]
    private GameObject teleportInteractor;

    [SerializeField]
    private GameObject controllerRoot;

    [SerializeField]
    private bool isLocal = true;

    [SerializeField]
    private ControllerObjects[] controllers;
    [SerializeField]
    private ControllerObjects fallbackController;

    private List<InputDevice> inputDeviceList = new List<InputDevice>();

    private ControllerObjects activeController;
    public GameObject ActiveController => activeController.mesh;
    public string ActiveControllerName => activeController.name;


    private void Awake()
    {
        foreach (var controller in controllers)
            controller.mesh.SetActive(false);
    }

    private void Update()
    {
        if (isLocal)
        {
            InputDeviceCharacteristics handedness;
            switch (this.handedness)
            {
                case Handedness.Left:
                default:
                    handedness = InputDeviceCharacteristics.Left;
                    break;
                case Handedness.Right:
                    handedness = InputDeviceCharacteristics.Right;
                    break;
            }

            inputDeviceList.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller & handedness, inputDeviceList);

            if (inputDeviceList.Count > 0)
            {
                for (int i = 0; i < inputDeviceList.Count; i++)
                {
                    InputDevice device = inputDeviceList[i];
                    if (device.characteristics.HasFlag(handedness))
                    {
                        SetModelByName(device.name);
                    }
                }
            }
        }
    }

    public void SetModelByName(string name)
    {
        for (int j = 0; j < controllers.Length; j++)
        {
            if (controllers[j].name.Equals(name) && controllers[j].mesh != null)
            {
                SetModel(controllers[j]);
                this.enabled = false; //disable this script after controller model is set
                return;
            }
        }
        SetModel(fallbackController);
    }

    private void SetModel(ControllerObjects controller)
    {
        if (controller.mesh != activeController.mesh || !controller.mesh.activeSelf)
        {
            if (activeController.mesh)
                activeController.mesh.gameObject.SetActive(false);
            controller.mesh.SetActive(true);
            if (directKnob)
            {
                directKnob.transform.localPosition = controller.knobPosition;
                directKnob.transform.localRotation = Quaternion.Euler(controller.knobRotation);
            }
            if (rayInteractor)
            {
                rayInteractor.transform.localPosition = controller.knobPosition;
                rayInteractor.transform.localRotation = Quaternion.Euler(controller.knobRotation);
            }
            if (teleportInteractor)
            {
                teleportInteractor.transform.localPosition = controller.knobPosition;
                teleportInteractor.transform.localRotation = Quaternion.Euler(controller.knobRotation);
            }
            activeController = controller;
        }
    }
}


