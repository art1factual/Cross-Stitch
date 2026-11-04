using UnityEngine;

public class CrossStitchBalance : MonoBehaviour
{
    public bool supportEnabled = true;

    [Header("Position Supports")]
    public float positionStrength = 12f;
    public float positionDamping = 5f;
    public float maximumAcceleration = 15f;

    [Header("Upright Supports")]

    public float uprightStrength = 30f;
    public float uprightDamping = 8f;
    public float maximumAngularAcceleration = 60f;

    private Rigidbody pelvis;

    private Vector3 standingPosition;
    private Quaternion standingRotation;
    private float totalMass;

    [Header("movement")]
    public Transform movementTarget;

    [Min(0f)]
    public float movementSpeed = 1f;


    public void Initialize(Rigidbody pelvisBody)
    {
        pelvis = pelvisBody;

        standingPosition = pelvis.position;
        standingRotation = Quaternion.Euler(0f, pelvis.rotation.eulerAngles.y, 0f);

        totalMass = 0f;

        foreach (Rigidbody part in GetComponentsInChildren<Rigidbody>())
        {
            totalMass += part.mass;

        }

        pelvis.isKinematic = false;

        

    }

    private void FixedUpdate()
    {
        if (pelvis == null || !supportEnabled)
        return;

        UpdateMovementTarget();

        ApplyPositionSupport();
        ApplyUprightSupport();
    }

    private void UpdateMovementTarget()
    {
        if (movementTarget == null)
        return;

        Vector3 destination = movementTarget.position;

        destination.y = standingPosition.y;

        standingPosition = Vector3.MoveTowards(standingPosition, destination, movementSpeed * Time.fixedDeltaTime);

    }



    private void ApplyPositionSupport()
    {
        Vector3 positionError = standingPosition - pelvis.position;

        Vector3 acceleration = positionError * positionStrength - pelvis.linearVelocity * positionDamping;

        acceleration -= Physics.gravity;
        //Support the Weight of the Connected Body ^

        acceleration = Vector3.ClampMagnitude(acceleration, maximumAcceleration);

        pelvis.AddForce(acceleration * totalMass, ForceMode.Force);

    }

    private void ApplyUprightSupport()
    {
        Quaternion difference = standingRotation * Quaternion.Inverse(pelvis.rotation);

        if (difference.w < 0f)
        {
            difference = new Quaternion( -difference.x, -difference.y, -difference.z, -difference.w);
        }
        difference.ToAngleAxis(out float angle, out Vector3 axis);

        Vector3 rotationError = Vector3.zero;

        if (angle > 0.001f && axis.sqrMagnitude > 0.001f)
        {
            rotationError = axis.normalized * angle * Mathf.Deg2Rad;
        }

        Vector3 acceleration = rotationError * uprightStrength - pelvis.angularVelocity * uprightDamping;

        acceleration = Vector3.ClampMagnitude(acceleration, maximumAngularAcceleration);

        pelvis.AddTorque(acceleration, ForceMode.Acceleration);
    }

    
    




}

