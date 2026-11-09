using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CrossStitchCar : MonoBehaviour
{
    private Rigidbody body;

    private Vector3 origin;

    private float laneZ;
    private float speed;
    private float halfWidth;

    public Vector3 Position => body.position;
    public Vector3 Velocity => Vector3.right * speed;

    public void Configure(Vector3 courseOrigin, float lanePosition, float signedSpeed, float roadHalfWidth)
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        origin = courseOrigin; laneZ = lanePosition;
        speed = signedSpeed;
        halfWidth = roadHalfWidth;

    }

    public void ResetPosition(float localX)
    {
        body.position = origin + new Vector3(localX, 0.45f, laneZ);

        body.rotation = Quaternion.identity;

    }

    private void FixedUpdate()
    {
        Vector3 next = body.position + Velocity * Time.fixedDeltaTime;
        float localX = next.x - origin.x;
        float boundary = halfWidth + 3f;

        if (localX > boundary)
        {
            ResetPosition(-boundary);
            return;
        }

        if (localX < -boundary)
        {
            ResetPosition(boundary);
            return;
        }

        body.MovePosition(next);
       
    }
}