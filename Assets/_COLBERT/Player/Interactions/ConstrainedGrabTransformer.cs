using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

//transformer implementation for rotating with arrow keys (controlled by ConstrainedGrabHelper) and placing interactables on the floor
public class ConstrainedGrabTransformer : XRBaseGrabTransformer
{
    [SerializeField] private bool yRotationOnly = false;
    [SerializeField] private bool floorCollision = false;
    [SerializeField] private LayerMask placeLayerMask;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float distanceSpeed = 0.001f;

    [SerializeField] private XRGrabInteractable interactable = null;
    [SerializeField] private Bounds bounds;

    public Bounds Bounds => GetBounds();

    private Vector3 basePositionOffset = Vector3.zero;
    private Quaternion baseRotationOffset = Quaternion.identity;
    //private Vector3 targetPositionOffset = Vector3.zero;
    private Quaternion targetRotationOffset = Quaternion.identity;
    //private Vector3 positionOffset = Vector3.zero;
    private Quaternion rotationOffset = Quaternion.identity;

    private Vector3 grabPoint = Vector3.zero;

    private float distanceOffset = 0f;

    private void OnDrawGizmos()
    {
        if (interactable != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    public void SetConstraints(bool floorCollision, bool yRotationOnly)
    {
        this.floorCollision = floorCollision;
        this.yRotationOnly = yRotationOnly;
    }

    public void RotateObject(float x, float y, bool step)
    {
        if (step)
        {
            Quaternion rotation = Quaternion.Euler(90f * Sign(x), 90f * Sign(y), 0);
            targetRotationOffset = rotation * targetRotationOffset;

            Vector3 euler = (targetRotationOffset * baseRotationOffset).eulerAngles;
            euler.x = Mathf.Round(euler.x / 90f) * 90f;
            euler.y = Mathf.Round(euler.y / 90f) * 90f;
            euler.z = Mathf.Round(euler.z / 90f) * 90f;
            targetRotationOffset = Quaternion.Euler(euler) * Quaternion.Inverse(baseRotationOffset);
        }
        else
        {
            float angle = rotateSpeed * Time.deltaTime;
            Quaternion rotation = Quaternion.Euler(angle * Sign(x), angle * Sign(y), 0);
            targetRotationOffset = rotation * targetRotationOffset;
        }
    }

    public void MoveDistance(float delta)
    {
        distanceOffset += delta * distanceSpeed;
    }

    public void OnSelectEnterInitialize(SelectEnterEventArgs args)
    {
        interactable = (XRGrabInteractable)args.interactableObject;
        var interactor = interactable.interactorsSelecting[0];

        distanceOffset = 0f;

        bounds = GetBounds();
        targetRotationOffset = Quaternion.identity;
        this.rotationOffset = targetRotationOffset;

        grabPoint = bounds.center;
        if (Physics.Raycast(interactor.transform.position, interactor.transform.forward, out RaycastHit hit, 100f))
        {
            var interactablee = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (interactablee == interactable)
            {
                grabPoint = transform.InverseTransformPoint(hit.point);
            }
        }

        Quaternion baseRotation;
        if (yRotationOnly)
        {
            baseRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(interactor.transform.forward, Vector3.up).normalized);
        }
        else
        {
            baseRotation = interactor.transform.rotation;
        }
        baseRotationOffset = Quaternion.Inverse(baseRotation) * interactable.transform.rotation;
        basePositionOffset = Quaternion.Inverse(interactor.transform.rotation * baseRotationOffset) * (interactor.transform.rotation * baseRotationOffset * grabPoint - baseRotation * baseRotationOffset * grabPoint);
    }

    public void OnSelectExitReset(SelectExitEventArgs args)
    {
        if ((IXRSelectInteractable)interactable == args.interactableObject)
        {
            interactable = null;
        }
    }

    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
    {
        switch (updatePhase)
        {
            case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
            case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                {
                    UpdateTarget(grabInteractable, ref targetPose);
                    break;
                }
        }
    }

    private void UpdateTarget(XRGrabInteractable grabInteractable, ref Pose targetPose)
    {
        var interactor = grabInteractable.interactorsSelecting[0];
        var interactorAttachPose = interactor.GetAttachTransform(grabInteractable).GetWorldPose();
        var thisTransformPose = grabInteractable.transform.GetWorldPose();
        var thisAttachTransform = grabInteractable.GetAttachTransform(interactor);

        // Calculate offset of the grab interactable's position relative to its attach transform
        var attachOffset = thisTransformPose.position - thisAttachTransform.position;

        // Transform that offset direction from world space to local space of the transform it's relative to.
        // It will be applied to the interactor's attach position using the orientation of the Interactor's attach transform.
        var positionOffset = thisAttachTransform.InverseTransformDirection(attachOffset);
        var rotationOffset = Quaternion.Inverse(Quaternion.Inverse(thisTransformPose.rotation) * thisAttachTransform.rotation);

        Vector3 newPosition = (interactorAttachPose.rotation * positionOffset) + interactorAttachPose.position;
        Quaternion newRotation; // = (interactorAttachPose.rotation * rotationOffset); //rotation without user input rotation or other constraints

        newPosition += interactor.transform.forward * distanceOffset;

        Quaternion baseRotation;
        if (yRotationOnly)
        {
            baseRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(interactor.transform.forward, Vector3.up).normalized);
        }
        else
        {
            baseRotation = interactor.transform.rotation;
        }

        this.rotationOffset = Quaternion.RotateTowards(this.rotationOffset, targetRotationOffset, 0.5f * rotateSpeed * Time.deltaTime); //user input rotation
        newRotation = baseRotation * this.rotationOffset * baseRotationOffset;
        newPosition = newPosition - newRotation * bounds.center + baseRotation * baseRotationOffset * bounds.center; //adjust position to rotate around bounds center
        newPosition = newPosition - (interactor.transform.rotation * baseRotationOffset * basePositionOffset) + (interactor.transform.rotation * baseRotationOffset * grabPoint - baseRotation * baseRotationOffset * grabPoint);

        if (floorCollision && Physics.Raycast(transform.position + transform.rotation * bounds.center, -Vector3.up, out RaycastHit hit, 100, placeLayerMask.value, QueryTriggerInteraction.Ignore))
        {
            //determine bottom vertex point of bounds (extents with signed components)
            Vector3 extents = FindBottomExtents(newRotation, bounds.extents);
            float extentY = Mathf.Abs((newRotation * extents).y);

            Vector3 newCenter = newPosition + newRotation * bounds.center;
            float bottomPos = newCenter.y - extentY; //lowest point of bounding box

            if (hit.point.y > bottomPos) //if object bounds intersect with floor
            {
                if (Quaternion.Angle(targetRotationOffset, this.rotationOffset) < 0.1f) //if no artificial rotation from user is running
                {
                    //rotate object so it aligns with floor plane
                    Vector3 minAxis = FindVerticalExtentsComponent(newRotation, extents);
                    float overlap = hit.point.y - bottomPos;
                    Quaternion addRotation = Quaternion.FromToRotation(minAxis, Vector3.down);
                    newRotation = Quaternion.Slerp(newRotation, addRotation * newRotation, overlap * 10f);

                    newCenter = newPosition + newRotation * bounds.center;
                    extentY = Mathf.Abs((newRotation * extents).y);
                    bottomPos = newCenter.y - extentY;
                }
                newPosition.y += hit.point.y - bottomPos; //move object up so it does not intersect with floor
            }
        }

        targetPose.rotation = newRotation;
        targetPose.position = newPosition;
    }

    //calculate bounds relative to this transform
    private Bounds GetBounds()
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        MeshRenderer[] children = gameObject.GetComponentsInChildren<MeshRenderer>();
        Bounds bounds = children[0].bounds;
        for (int i = 1; i < children.Length; i++)
        {
            bounds.Encapsulate(children[i].bounds);
        }

        transform.position = position;
        transform.rotation = rotation;
        return bounds;
    }

    //determine the lowest vertex point of a box with extents and rotation, which is extents with adjusted signs
    private static Vector3 FindBottomExtents(Quaternion rotation, Vector3 extents)
    {
        Vector3 bottomExtents = extents;
        float maxExtentY = Mathf.Abs((rotation * bottomExtents).y);

        Vector3 ex = new Vector3 { x = -extents.x, y = extents.y, z = extents.z };
        if (Mathf.Abs((rotation * ex).y) > maxExtentY)
        {
            bottomExtents = ex;
            maxExtentY = Mathf.Abs((rotation * bottomExtents).y);
        }
        ex = new Vector3 { x = extents.x, y = -extents.y, z = extents.z };
        if (Mathf.Abs((rotation * ex).y) > maxExtentY)
        {
            bottomExtents = ex;
            maxExtentY = Mathf.Abs((rotation * bottomExtents).y);
        }
        ex = new Vector3 { x = extents.x, y = extents.y, z = -extents.z };
        if (Mathf.Abs((rotation * ex).y) > maxExtentY)
        {
            bottomExtents = ex;
            maxExtentY = Mathf.Abs((rotation * bottomExtents).y);
        }

        if ((rotation * bottomExtents).y > 0)
            bottomExtents = -bottomExtents;
        return bottomExtents;
    }

    //determine the component of a box (with rotation and extents) that is closest to the vertical axis
    private static Vector3 FindVerticalExtentsComponent(Quaternion rotation, Vector3 extents)
    {
        Vector3 ex1 = rotation * new Vector3 { x = extents.x, y = 0, z = 0 }.normalized;
        Vector3 ex2 = rotation * new Vector3 { x = 0, y = extents.y, z = 0 }.normalized;
        Vector3 ex3 = rotation * new Vector3 { x = 0, y = 0, z = extents.z }.normalized;
        Vector3 minAxis = ex1;
        if (Vector3.Dot(Vector3.down, ex2) > Vector3.Dot(Vector3.down, minAxis))
            minAxis = ex2;
        if (Vector3.Dot(Vector3.down, ex3) > Vector3.Dot(Vector3.down, minAxis))
            minAxis = ex3;
        return minAxis;
    }

    private static float Sign(float x)
    {
        return x == 0 ? 0 : Mathf.Sign(x);
    }
}
