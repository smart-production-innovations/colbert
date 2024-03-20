using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

//helper script on the interactor (screenInteractor) for rotating the selected interactable with arrow keys
public class ConstrainedGrabHelper : MonoBehaviour
{
    [SerializeField] private bool collision = false;
    [SerializeField] private bool constrainRotation = true;

    [SerializeField] private InputActionReference rotateX;
    [SerializeField] private InputActionReference rotateY;
    [SerializeField] private InputActionReference rotateNegX;
    [SerializeField] private InputActionReference rotateNegY;

    [SerializeField] private InputActionReference rotateX_Continuous;
    [SerializeField] private InputActionReference rotateY_Continuous;
    [SerializeField] private InputActionReference rotateNegX_Continuous;
    [SerializeField] private InputActionReference rotateNegY_Continuous;

    [SerializeField] private InputActionReference moveDistance;
    [SerializeField] private InputActionReference moveDistanceBlocker;

    private bool xtriggered = false;
    private bool ytriggered = false;
    private bool xnegtriggered = false;
    private bool ynegtriggered = false;

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform.TryGetComponent(out ConstrainedGrabTransformer transformer))
        {
            transformer.SetConstraints(collision, constrainRotation);
        }
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform.TryGetComponent(out ConstrainedGrabTransformer transformer))
        {
            transformer.SetConstraints(false, false);
        }
    }

    private void OnEnable()
    {
        var interactor = GetComponent<XRBaseInteractor>();
        interactor.selectEntered.AddListener(OnSelectEnter);
        interactor.selectExited.AddListener(OnSelectExit);

        rotateX.action.performed += RotateObjectX;
        rotateY.action.performed += RotateObjectY;
        rotateNegX.action.performed += RotateObjectNegX;
        rotateNegY.action.performed += RotateObjectNegY;

        rotateX_Continuous.action.performed += RotateObjectX_Continuous;
        rotateY_Continuous.action.performed += RotateObjectY_Continuous;
        rotateNegX_Continuous.action.performed += RotateObjectNegX_Continuous;
        rotateNegY_Continuous.action.performed += RotateObjectNegY_Continuous;

        moveDistance.action.performed += MoveDistance;
    }

    private void OnDisable()
    {
        var interactor = GetComponent<XRBaseInteractor>();
        interactor.selectEntered.RemoveListener(OnSelectEnter);
        interactor.selectExited.RemoveListener(OnSelectExit);

        rotateX.action.performed -= RotateObjectX;
        rotateY.action.performed -= RotateObjectY;
        rotateNegX.action.performed -= RotateObjectNegX;
        rotateNegY.action.performed -= RotateObjectNegY;

        rotateX_Continuous.action.performed -= RotateObjectX_Continuous;
        rotateY_Continuous.action.performed -= RotateObjectY_Continuous;
        rotateNegX_Continuous.action.performed -= RotateObjectNegX_Continuous;
        rotateNegY_Continuous.action.performed -= RotateObjectNegY_Continuous;

        moveDistance.action.performed -= MoveDistance;
    }

    private void Update()
    {
        if (rotateX_Continuous.action.ReadValue<float>() == 0f)
        {
            xtriggered = false;
        }
        if (rotateNegX_Continuous.action.ReadValue<float>() == 0f)
        {
            xnegtriggered = false;
        }
        if (rotateY_Continuous.action.ReadValue<float>() == 0f)
        {
            ytriggered = false;
        }
        if (rotateNegY_Continuous.action.ReadValue<float>() == 0f)
        {
            ynegtriggered = false;
        }

        float x = 0f;
        float y = 0f;
        if (xtriggered)
        {
            x += 1f;
        }
        if (ytriggered)
        {
            y += 1f;
        }
        if (xnegtriggered)
        {
            x -= 1f;
        }
        if (ynegtriggered)
        {
            y -= 1f;
        }
        if (x != 0f || y != 0f)
        {
            RotateObject(x, y, false);
        }
    }

    private void RotateObject(float x, float y, bool step)
    {
        IXRSelectInteractor interactor = GetComponent<IXRSelectInteractor>();
        if (interactor.hasSelection)
        {
            IXRSelectInteractable interactable = interactor.firstInteractableSelected;
            ConstrainedGrabTransformer transformer = interactable.transform.GetComponent<ConstrainedGrabTransformer>();
            transformer?.RotateObject(x, y, step);
        }
    }

    private void RotateObjectX(InputAction.CallbackContext obj)
    {
        RotateObject(1f, 0f, true);
    }
    private void RotateObjectNegX(InputAction.CallbackContext obj)
    {
        RotateObject(-1f, 0f, true);
    }
    private void RotateObjectY(InputAction.CallbackContext obj)
    {
        RotateObject(0f, 1f, true);
    }
    private void RotateObjectNegY(InputAction.CallbackContext obj)
    {
        RotateObject(0f, -1f, true);
    }

    private void RotateObjectX_Continuous(InputAction.CallbackContext obj)
    {
        xtriggered = true;
    }
    private void RotateObjectNegX_Continuous(InputAction.CallbackContext obj)
    {
        xnegtriggered = true;
    }
    private void RotateObjectY_Continuous(InputAction.CallbackContext obj)
    {
        ytriggered = true;
    }
    private void RotateObjectNegY_Continuous(InputAction.CallbackContext obj)
    {
        ynegtriggered = true;
    }

    private void MoveDistance(InputAction.CallbackContext obj)
    {
        if (moveDistanceBlocker && moveDistanceBlocker.action.ReadValue<float>() != 0)
            return;

        IXRSelectInteractor interactor = GetComponent<IXRSelectInteractor>();
        if (interactor.hasSelection)
        {
            IXRSelectInteractable interactable = interactor.firstInteractableSelected;
            ConstrainedGrabTransformer transformer = interactable.transform.GetComponent<ConstrainedGrabTransformer>();
            transformer?.MoveDistance(obj.ReadValue<float>());
        }
    }

}
