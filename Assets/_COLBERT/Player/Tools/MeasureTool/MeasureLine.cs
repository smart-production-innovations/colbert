using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

//a measure line between two points that shows the distance between the points on a label
public class MeasureLine : MonoBehaviour
{
    [SerializeField]
    public GameObject canvas;
    [SerializeField]
    public TextMeshPro measureText;

    [SerializeField] public LineRenderer lineDrawer;

    [SerializeField]
    public GameObject firstPointSphere;
    [SerializeField]
    public GameObject secondPointSphere;

    public Vector3 direction = Vector3.zero;
    public Player owner = null;

    [SerializeField]
    public MeasureTool measureTool;

    public  Transform firstParentTranform;
    public Transform secondParentTranform;

    public Vector3 offsetToFirst; 
    public Vector3 offsetToSecond;

    public float lineDistance = 0;

    public bool isPlaced = false;


    private void Awake()
    {
        MeshRenderer[] meshrenderers = this.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshrenderers)
            foreach (Material mat in renderer.materials) { }
    }

    private void Update()
    {
        if (owner != null && direction != Vector3.zero)
        {
            Transform cam = owner.Camera.transform;
            Vector3 camForward = (canvas.transform.position - cam.position).normalized;
            Vector3 up = Vector3.Cross(camForward, direction).normalized;
            if (Vector3.Dot(up, cam.up) < 0)
                up = -up;
            Vector3 forward = Vector3.Cross(direction, up).normalized;
            if (Vector3.Dot(forward, camForward) < 0)
                forward = -forward;

            canvas.transform.rotation = Quaternion.LookRotation(forward, up);
        }
        if (isPlaced)
        {
            if(firstParentTranform != null && secondParentTranform != null)
            {
                lineDrawer.SetPosition(0, firstParentTranform.TransformPoint(offsetToFirst));
                firstPointSphere.transform.position = firstParentTranform.TransformPoint(offsetToFirst);
                lineDrawer.SetPosition(1, secondParentTranform.TransformPoint(offsetToSecond));
                secondPointSphere.transform.position = secondParentTranform.TransformPoint(offsetToSecond);
                direction = (secondPointSphere.transform.position - firstPointSphere.transform.position).normalized;
                canvas.transform.localPosition = (firstParentTranform.TransformPoint(offsetToFirst) + secondParentTranform.TransformPoint(offsetToSecond)) / 2;

                float distance = Vector3.Distance(firstPointSphere.transform.position, secondPointSphere.transform.position);
                canvas.GetComponentInChildren<TextMeshPro>().text = $"{distance:0.000}\u2009m";// distance.ToString("0.000") + "\u2009" + "m";

                if (distance * 6 < 2) canvas.GetComponentInChildren<TextMeshPro>().fontSize = 2;
                else if (distance * 6 > 30) canvas.GetComponentInChildren<TextMeshPro>().fontSize = 30;
                else canvas.GetComponentInChildren<TextMeshPro>().fontSize = (distance * 6);
            }
            else
            {
                RemoveThisFromLines();
            }
        }
    }

    public void RemoveThisFromLines()
    {
        measureTool.lines.Remove(this.gameObject);
        Destroy(this.gameObject);
    }

    #region colorChange (hover)

    public void OnHoverEnterGlow(HoverEnterEventArgs args)
    {
        MeshRenderer[] meshrenderers = this.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshrenderers)
            foreach (Material mat in renderer.sharedMaterials)
            {
                mat.color = Color.red;
            }

        lineDrawer.startColor = Color.red;
        lineDrawer.endColor = Color.red;
    }

    public void OnHoverExitGlow(HoverExitEventArgs args)
    {
        MeshRenderer[] meshrenderers = this.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshrenderers)
            foreach (Material mat in renderer.sharedMaterials)
                mat.color = Color.white;

        lineDrawer.startColor = Color.white;
        lineDrawer.endColor = Color.white;
    }

    #endregion
}
