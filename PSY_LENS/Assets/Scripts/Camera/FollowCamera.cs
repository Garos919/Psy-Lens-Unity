// FollowCamera.cs
// Simple over-the-shoulder / tracking camera for Psy-Lens
// Enabled only when CameraManager sets follow mode active

using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;              // player transform
    public Vector3 offset = new Vector3(0f, 2.0f, -4.0f);
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;

    private Vector3 desiredPosition;

    void LateUpdate()
    {
        if (target == null) return;

        // desired position behind player
        desiredPosition = target.position + target.TransformDirection(offset);

        // smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // look at player smoothly
        Quaternion lookRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
    }
    
}
