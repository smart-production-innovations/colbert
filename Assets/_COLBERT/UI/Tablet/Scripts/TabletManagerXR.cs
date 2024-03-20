using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//tablet follows xr head movement
public class TabletManagerXR : MonoBehaviour
{
    [SerializeField] private Transform anchorObject;
    [SerializeField] private float maxDistanceFromAnchor = 0.2f;
    [SerializeField] private float maxRotDistance = 20;
    [SerializeField] private float moveSpeed = 0.3f;
    [SerializeField] private float rotateSpeed = 20f;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        transform.position = anchorObject.position;
        transform.rotation = Quaternion.LookRotation(anchorObject.forward, Vector3.up);// anchorObject.transform.rotation;
    }

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, anchorObject.position);
        if (distance > maxDistanceFromAnchor)
        {
            gameObject.transform.position = Vector3.MoveTowards(transform.position, anchorObject.position, moveSpeed * Time.deltaTime);
        }

        float angle = Quaternion.Angle(transform.rotation, anchorObject.rotation);
        if (angle > maxRotDistance)
        {
            transform.rotation = Quaternion.LookRotation(Quaternion.RotateTowards(transform.rotation, anchorObject.rotation, rotateSpeed * Time.deltaTime) * Vector3.forward, Vector3.up);
        }
    }
}
