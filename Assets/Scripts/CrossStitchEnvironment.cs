using UnityEngine;

public class CrossStitchEnvironment : MonoBehaviour
{

    [Header("Road")]
    public float roadWidth = 24f;
    public float laneWidth = 1f;

    [Header("Traffic")]
    public float vehicleSpeed = 4f;
    
    private Rigidbody vehicleBody;
    
    

    private void Start()
    {
        GameObject vehicle = CreateBox("Vehicle", new Vector3(-10f, 0.6f, 0f), new Vector3 (2.5f, 1.2f, 1.6f));

        CreateBox("Starting Island", new Vector3(0f,-0.25f, -3f), new Vector3(roadWidth, 0.5f, laneWidth));
    

    CreateBox("Traffic Lane", new Vector3( 0f, -0.25f, 0f), new Vector3(roadWidth, 0.5f, laneWidth));

    CreateBox("Finish Island", new Vector3(0f, -0.25f, 2f), new Vector3(2.5f, 1.2f, 1.6f));

    vehicleBody = vehicle.AddComponent<Rigidbody>();

    vehicleBody.isKinematic = true;
    vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
    vehicleBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

    ConfigureCamera();
}       

    private void FixedUpdate()
    {
        if (vehicleBody == null)
        return;
        
        Vector3 nextPosition = vehicleBody.position;
        nextPosition.x += vehicleSpeed * Time.fixedDeltaTime;

        float wrapBoundary = roadWidth * 0.5f + 3f;

        if (nextPosition.x > wrapBoundary)
        {
            nextPosition.x = -wrapBoundary;
            vehicleBody.position = nextPosition;
            
        }
        else
        {
            vehicleBody.MovePosition(nextPosition);
        }
    }

    private GameObject CreateBox(string objectName, Vector3 position, Vector3 size)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.name = objectName;

        box.transform.SetParent(transform, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        return box;
    }

    private void ConfigureCamera()
    {
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
        return;

        Vector3 focus = transform.position;

        sceneCamera.transform.position = focus + new Vector3(10f, 14f, -12f);
        sceneCamera.transform.LookAt(focus);
        sceneCamera.orthographic = true;

        sceneCamera.orthographicSize = 10f;
    }

    
}