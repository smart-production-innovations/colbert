using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

//draw measure lines beetween two points in space that shows the distance between them
public class MeasureTool : MonoBehaviour
{
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField] private LineRenderer pointer;
    [SerializeField] private GameObject sphere;

    public bool isActive = false;

    public float distance;

    [SerializeField]
    private Transform startPosition;

    [SerializeField]
    private InputActionReference measureAction;
    [SerializeField]
    private InputActionReference measureDeleteAction;

    private Vector3 firstPoint;
    private Vector3 secondPoint;

    [SerializeField]
    private GameObject linePrefab;


    [SerializeField]
    public List<GameObject> lines;

    MeasureLine measureLine;

    [SerializeField]
    private XRSphereButton[] measureButtons;

    [SerializeField]
    private List<GameObject> deleteButtons;

    [SerializeField]
    private MeasureTool counterMeasureTool;

    [SerializeField]
    private ScreenInteractor interactor;

    private int lineCount = 0;

    [SerializeField]
    private InputActionReference toggleAction;

    [SerializeField]
    private bool isNonXRPlayer = false;
    private MeasureIndicator[] indicators; //for nonXRLaser


    public void ActivateMeasureTool()
    {
        pointer.enabled = !isNonXRPlayer;
        pointer.gameObject.SetActive(true);
        sphere.gameObject.SetActive(true);
        isActive = true;
        if (isNonXRPlayer) interactor.enabled = false;

        if (indicators != null)
            foreach (var indicator in indicators)
                indicator.gameObject.SetActive(true);
    }

    public void DeactivateMeasureTool()
    {
        pointer.gameObject.SetActive(false);
        sphere.gameObject.SetActive(false);
        lineCount = 0;
        isActive = false;
        if(isNonXRPlayer) interactor.enabled = true;

        //remove last element on deactivate -TODO
        if (!isNonXRPlayer && measureButtons[0].isHovered)
        {
            Destroy(lines[lines.Count - 1].gameObject);
            lines.RemoveAt(lines.Count - 1);
        }

        if (indicators != null)
            foreach (var indicator in indicators)
                indicator.gameObject.SetActive(false);
    }

    public void RemoveAllLines()
    {
        foreach (GameObject line in lines) Destroy(line.gameObject);
        lines.Clear();
        if (counterMeasureTool != null && counterMeasureTool.lines.Count > 0)
            counterMeasureTool?.RemoveAllLines();
    }

    private void Awake()
    {
        if (isNonXRPlayer)
            indicators = FindObjectsByType<MeasureIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (isNonXRPlayer)
        {
            var deleteIndicators = FindObjectsByType<MeasureDeleteIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var indicator in deleteIndicators)
                if (!deleteButtons.Contains(indicator.gameObject))
                    deleteButtons.Add(indicator.gameObject);
        }
    }

    private void OnDestroy()
    {
        RemoveAllLines();
    }

    private void Update()
    {
        if (isNonXRPlayer)
        {
            if (toggleAction.action.WasPressedThisFrame())
            {
                if (!isActive)
                    ActivateMeasureTool();
                else
                    DeactivateMeasureTool();
            }
        }
        
        if (isActive)
        {
            pointer.SetPosition(0, startPosition.position);
            if (Physics.Raycast(startPosition.position, startPosition.forward, out RaycastHit hit, float.MaxValue, layerMask.value, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider)
                {
                    pointer.SetPosition(1, hit.point);
                    sphere.transform.position = hit.point;
                }

                if (measureAction.action.WasPressedThisFrame())
                {
                    GameObject currentLine = Instantiate(linePrefab);
                    measureLine = currentLine.GetComponent<MeasureLine>();
                    measureLine.measureTool = this;

                    firstPoint = pointer.GetPosition(1);
                    measureLine.lineDrawer.SetPosition(0, firstPoint);

                    measureLine.firstParentTranform = hit.collider.gameObject.transform;
                    measureLine.offsetToFirst = measureLine.firstParentTranform.InverseTransformPoint(hit.point);

                    measureLine.firstPointSphere.transform.position = firstPoint;
                    lineCount++;
                    lines.Add(currentLine);
                }
                if (lineCount == 1)
                {
                    firstPoint = measureLine.firstParentTranform.TransformPoint(measureLine.offsetToFirst);
                    measureLine.lineDrawer.SetPosition(0, firstPoint);
                    measureLine.firstPointSphere.transform.position = firstPoint;

                    measureLine.lineDrawer.enabled = true;
                    secondPoint = pointer.GetPosition(1);
                    measureLine.lineDrawer.SetPosition(1, secondPoint);
                    distance = Vector3.Distance(firstPoint, secondPoint);

                    measureLine.secondParentTranform = hit.collider.gameObject.transform;
                    measureLine.offsetToSecond = measureLine.secondParentTranform.InverseTransformPoint(hit.point);

                    measureLine.secondPointSphere.transform.position = secondPoint;
                    measureLine.canvas.transform.localPosition = (firstPoint + secondPoint) / 2;
                    measureLine.direction = (secondPoint - firstPoint).normalized;
                    measureLine.owner = GetComponentInParent<Player>();
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    measureLine.canvas.GetComponentInChildren<TextMeshPro>().text = $"{distance:0.000}\u2009m";// distance.ToString("0.000") + "\u2009" + "m";

                    if (distance * 6 < 2) measureLine.canvas.GetComponentInChildren<TextMeshPro>().fontSize = 2;
                    else if (distance * 6 > 30) measureLine.canvas.GetComponentInChildren<TextMeshPro>().fontSize = 30;
                    else measureLine.canvas.GetComponentInChildren<TextMeshPro>().fontSize = (distance * 6);

                    
                }
            }

            if (measureAction.action.WasReleasedThisFrame())
            {
                lineCount = 0;
                measureLine.isPlaced = true;
                DeactivateMeasureTool();
                foreach (XRSphereButton btn in measureButtons)
                {
                    btn.SetPassivDeactive();
                }
                if (distance < 0.01f) // deletes 
                {
                    Destroy(lines[lines.Count - 1].gameObject);
                    lines.RemoveAt(lines.Count - 1);
                }
            }
        }

        if (isNonXRPlayer)
        {
            if (measureDeleteAction.action.WasPressedThisFrame())
            {
                RemoveAllLines();
            }
        }

        if (lines.Count.Equals(0) && (counterMeasureTool == null || counterMeasureTool.lines.Count.Equals(0)))
        {
            foreach (var btn in deleteButtons)
            {
                btn.SetActive(false);
            }
        }
        else
        {
            foreach (var btn in deleteButtons)
            {
                btn.SetActive(true);
            }
        }
    }
}
