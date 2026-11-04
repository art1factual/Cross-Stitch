using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class CrossStitchMuscle : MonoBehaviour
{
    public bool musclesEnabled = true;

    [Min(0f)] public float strength = 40f;
    [Min(0f)] public float damping = 10f;
    [Min(0f)] public float maximumAcceleration = 60f;

    private Rigidbody body;
    private Rigidbody connectedBody;
    private Quaternion restingRotation;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        if (joint == null || joint.connectedBody == null)
        {
            enabled = false;
            return;

           

        }
 connectedBody = joint.connectedBody;

            restingRotation = Quaternion.Inverse(connectedBody.rotation) * body.rotation;
    }


    private void FixedUpdate()
    {
        if (!musclesEnabled || body.isKinematic)
        return;

        Quaternion desiredRotation = connectedBody.rotation * restingRotation;

        Quaternion difference = desiredRotation * Quaternion.Inverse(body.rotation);

        if (difference.w < 0f)
        {
            difference = new Quaternion(-difference.x, -difference.y, -difference.z, -difference.w);

        }

        difference.ToAngleAxis(out float angle, out Vector3 axis);

        Vector3 rotationError = Vector3.zero;

        if (angle > 0.001f && axis.sqrMagnitude > 0.001f)
        {
            rotationError = axis.normalized * angle * Mathf.Deg2Rad;
        }

        Vector3 relativeAngularVelocity = body.angularVelocity - connectedBody.angularVelocity;

        Vector3 acceleration = rotationError * strength - relativeAngularVelocity * damping;

        acceleration = Vector3.ClampMagnitude(acceleration, maximumAcceleration);
        body.AddTorque(acceleration, ForceMode.Acceleration);
    }
}