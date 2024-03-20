using UnityEngine;
using UnityEngine.InputSystem;

//script for laser pointer either following middle of screen (NonXR-Player) or Controller (XR Player)
public class Laserpointer : MonoBehaviour
{
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField] private LineRenderer pointer;
    [SerializeField] private GameObject arrow;

    [SerializeField]
    private Transform startPosition;

    [SerializeField]
    private InputActionReference laserAction;

    public bool isActive = false;

    [SerializeField]
    private bool isNonXRPlayer = false;
    private LaserpointerIndicator[] indicators; //for nonXRLaser


    public void ActivateLaser()
    {
        pointer.gameObject.SetActive(true);
        arrow.gameObject.SetActive(true);
        isActive = true;
        if (indicators != null)
            foreach (var indicator in indicators)
                indicator.gameObject.SetActive(true);
    }

    public void DeactivateLaser()
    {
        pointer.gameObject.SetActive(false);
        arrow.gameObject.SetActive(false);
        isActive = false;
        if (indicators != null)
            foreach (var indicator in indicators)
                indicator.gameObject.SetActive(false);
    }
    public void SetActive(bool isActive)
    {
        if (isActive)
            ActivateLaser();
        else
            DeactivateLaser();
    }

    private void Awake()
    {
        if (isNonXRPlayer)
            indicators = FindObjectsByType<LaserpointerIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (!isNonXRPlayer)
        {
            if (laserAction && laserAction.action.WasReleasedThisFrame()) DeactivateLaser();
            if (isActive && laserAction && laserAction.action.IsPressed()) ActivateLaser();
        }
        else
        {
            if (laserAction && laserAction.action.WasPressedThisFrame())
            {
                if (isActive)
                    DeactivateLaser();
                else
                    ActivateLaser();
            }
        }
        if (startPosition != null)
            transform.SetPositionAndRotation(startPosition.position, startPosition.rotation);

        pointer.SetPosition(0, transform.position);
        if (Physics.Raycast(transform.position + 0.02f * transform.forward, transform.forward, out RaycastHit hit, float.MaxValue, layerMask.value, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider)
            {
                pointer.SetPosition(1, hit.point - (transform.forward * 0.1f));
                arrow.transform.position = hit.point - (transform.forward * 0.1f);
                arrow.transform.rotation = transform.rotation;
            }
        }
        else
        {
            pointer.SetPosition(1, transform.forward * 1000);
            arrow.transform.position = transform.forward * 1000;
            arrow.transform.rotation = transform.rotation;
        }
    }
}
