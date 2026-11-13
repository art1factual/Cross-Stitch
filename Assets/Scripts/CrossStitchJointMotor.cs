using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class CrossStitchJointMotor : MonoBehaviour
{
    public Vector3 minimumAngles;
    public Vector3 maximumAngles;

    public float spring = 700f;
    public float damper = 65f;
    public float maximumTorque = 500f;
    private ConfigurableJoint joint;
    private Vector3 smoothedAngles;
    private float smoothedStrength;

    public Vector3 NormalizedTarget => smoothedAngles / 180f;

    public float CurrentStrength => smoothedStrength;

    public void Configure(Vector3 minimum, Vector3 maximum, float stiffness, float damping, float torque)
    {
        joint = GetComponent<ConfigurableJoint>();
        minimumAngles = minimum;
        maximumAngles = maximum;
        spring = stiffness;
        damper = damping;
        maximumTorque = torque;
        ResetMotor();
    }

    private static float Angle(float action, float low, float high)
    {
        action = Mathf.Clamp(action, -1f, 1f);
        return action < 0f ? -action * low : action * high;
    }

    public void Apply(float x, float y, float z, float strength, float dt)
    {
        Vector3 desired = new Vector3(Angle(x, minimumAngles.z, maximumAngles.z), Angle(y, minimumAngles.y, maximumAngles.y), Angle(z, minimumAngles.z, maximumAngles.z));

        smoothedAngles = Vector3.MoveTowards(smoothedAngles, desired, 240f * dt);
        smoothedStrength = Mathf.MoveTowards(smoothedStrength, Mathf.Clamp(strength, -1f, 1f), 5f * dt);
        WriteDrive();

    }

    private void WriteDrive()
    {
        joint.targetRotation = Quaternion.Inverse(Quaternion.Euler(smoothedAngles));
        joint.targetAngularVelocity = Vector3.zero;

        JointDrive drive = new JointDrive
        {
            positionSpring = spring * Mathf.Lerp(0.65f, 1.35f, (smoothedStrength + 1f) * 0.5f), positionDamper = damper, maximumForce = maximumTorque};
        joint.angularXDrive = drive;
        joint.angularYZDrive = drive;

        
    }
    public void ResetMotor()
    {
        if (joint == null) joint = GetComponent<ConfigurableJoint>();
        smoothedAngles = Vector3.zero;
        smoothedStrength = 0f;
        WriteDrive();
    }
}