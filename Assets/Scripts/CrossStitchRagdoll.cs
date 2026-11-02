using UnityEngine;

public class CrossStitchRagdoll : MonoBehaviour
{

    [Header("Initial Joint Test")]
    public bool anchorPelvis = true;

    private void Start()
    {
        Rigidbody pelvis = CreateBodyPart("Pelvis", new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 0.30f, 0.30f), 6f);

        Rigidbody torso = CreateBodyPart("Torso", new Vector3(0f, 1.48f, 0f), new Vector3( 0.55f, 0.50f, 0.30f), 8f);

       
       
        Rigidbody head = CreateBodyPart("Head", new Vector3(0f, 1.96f, 0f), new Vector3(0.30f, 0.34f, 0.30f), 2f);

        pelvis.isKinematic = anchorPelvis;
        Connect(torso, pelvis, new Vector3(0f, 1.22f, 0f), 25f);

        Connect(head, torso, new Vector3(0f, 1.76f, 0f),35f);

        //Legs 
        Rigidbody leftThigh = CreateBodyPart("Left Thigh", new Vector3(-0.15f, 0.70f, 0f), new Vector3(0.20f, 0.40f, 0.24f), 3f);

        Rigidbody rightThight = CreateBodyPart("Right Thigh", new Vector3(0.15f, 0.70f, 0f), new Vector3(0.20f, 0.40f, .24f), 3f);

        Rigidbody righhtShin = CreateBodyPart("Right Shin", new Vector3(0.15f, 0.28f, 0.f), new Vector3( 0.18f, 0.40f, 0.22f), 2f);
        
        
        Rigidbody leftShin = CreateBodyPart("Left Shin", new Vector3(-0.15f, 0.28f, 0f), new Vector3(0.18f, 0.40f, 0.22f), 2f);
        
        
        //Arms
        Rigidbody leftUpperArm = CreateBodyPart("Left Upper Arm", new Vector3(-0.40f, 1.47f, 0f), new Vector3(0.18f, 0.38f, 0.22f), 1.5f);

        Rigidbody rightUpperArm = CreateBodyPart("Right Upper Arm", new Vector3(0.40f, 1.47f, 0f), new Vector3(0.18f, 0.38f, 0.22f) ,1.5f);

        Rigidbody leftForearm = CreateBodyPart("Left Forearm", new Vector3(-0.40f, 1.08f, 0f), new Vector3(0.16f, 0.36f, 0.20f), 1f);

        Rigidbody rightForeame = CreateBodyPart("Right ForeArm", new Vector3(0.40f, 1.085f, 0f), new Vector3(0.40f, 1.085f, 0f),
        new Vector3(0.16f, 0.36f, 0.20f), 1f);

        //Hips and Knees
        


        Rigidbody[] parts =
        {
            pelvis, torso, head 

        };

        for (int i = 0; i < parts.Length; i++)
        {
            for (int j = i + 1; j < parts.Length; j++)
            {
                Physics.IgnoreCollision(parts[i].GetComponent<Collider>(), parts[j].GetComponent<Collider>()
                );
            }
        }



    }

    private Rigidbody CreateBodyPart(string partName, Vector3 localPosition, Vector3 size, float mass)
    {
        GameObject part = new GameObject(partName);

        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;

        BoxCollider collider = part.AddComponent<BoxCollider>();
        collider.size = size;

        Rigidbody body = part.AddComponent<Rigidbody>();
        body.mass = mass;

        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.solverIterations = 12;
        body.solverVelocityIterations = 6;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);

        visual.name = "Visual";
        visual.transform.SetParent(part.transform, false);
        visual.transform.localScale = size;

        visual.GetComponent<Collider>().enabled = false;

        return body;
    }

    private void Connect(Rigidbody child, Rigidbody parent, Vector3 localAnchor, float angleLimit)
    {
        ConfigurableJoint joint = child.gameObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody = parent;
        joint.autoConfigureConnectedAnchor = false;

        Vector3 worldAnchor = transform.TransformPoint(localAnchor);
        joint.anchor = child.transform.InverseTransformPoint(worldAnchor);
        joint.connectedAnchor = parent.transform.InverseTransformPoint(worldAnchor);
        

    joint.xMotion = ConfigurableJointMotion.Locked;
    joint.yMotion = ConfigurableJointMotion.Locked;
    joint.zMotion = ConfigurableJointMotion.Locked;

    joint.angularXMotion = ConfigurableJointMotion.Limited;

    joint.lowAngularXLimit = new SoftJointLimit { limit = -angleLimit };
    joint.highAngularXLimit = new SoftJointLimit { limit = angleLimit };
    joint.angularYLimit = new SoftJointLimit { limit = angleLimit };
    joint.angularZLimit = new SoftJointLimit { limit = angleLimit };
    joint.enableCollision = false;

    }
}