using UnityEngine;
using Unity.MLAgents.Policies;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
public class CrossStitchRagdoll : MonoBehaviour
{

    [SerializeField] private bool showTrainingPanel = true;
    [SerializeField]
    private Unity.InferenceEngine.ModelAsset trainedModel; 
    
    private void Start()
    {

        Rigidbody pelvis = CreateBodyPart("Pelvis", new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 0.30f, 0.30f), 6f);

        Rigidbody torso = CreateBodyPart("Torso", new Vector3(0f, 1.48f, 0f), new Vector3( 0.55f, 0.50f, 0.30f), 8f);

       
       
        Rigidbody head = CreateBodyPart("Head", new Vector3(0f, 1.96f, 0f), new Vector3(0.30f, 0.34f, 0.30f), 2f);

        Connect(torso, pelvis, new Vector3(0f, 1.22f, 0f), 25f);

        Connect(head, torso, new Vector3(0f, 1.76f, 0f),35f);

        //Legs 
        Rigidbody leftThigh = CreateBodyPart("Left Thigh", new Vector3(-0.15f, 0.70f, 0f), new Vector3(0.20f, 0.40f, 0.24f), 3f);

        Rigidbody rightThigh = CreateBodyPart("Right Thigh", new Vector3(0.15f, 0.70f, 0f), new Vector3(0.20f, 0.40f, .24f), 3f);

        Rigidbody rightShin = CreateBodyPart("Right Shin", new Vector3(0.15f, 0.28f, 0f), new Vector3( 0.18f, 0.40f, 0.22f), 2f);
        
        
        Rigidbody leftShin = CreateBodyPart("Left Shin", new Vector3(-0.15f, 0.28f, 0f), new Vector3(0.18f, 0.40f, 0.22f), 2f);
        
        
        //Arms
        Rigidbody leftUpperArm = CreateBodyPart("Left Upper Arm", new Vector3(-0.40f, 1.47f, 0f), new Vector3(0.18f, 0.38f, 0.22f), 1.5f);

        Rigidbody rightUpperArm = CreateBodyPart("Right Upper Arm", new Vector3(0.40f, 1.47f, 0f), new Vector3(0.18f, 0.38f, 0.22f) ,1.5f);

        Rigidbody leftForearm = CreateBodyPart("Left Forearm", new Vector3(-0.40f, 1.08f, 0f), new Vector3(0.16f, 0.36f, 0.20f), 1f);

        Rigidbody rightForearm = CreateBodyPart("Right ForeArm", new Vector3(0.40f, 1.085f, 0f),
        new Vector3(0.16f, 0.36f, 0.20f), 1f);

        Rigidbody leftFoot = CreateBodyPart("Left Foot", new Vector3(-0.15f, 0.04f, 0.10f), new Vector3(0.26f, 0.08f, 0.46f), 1f);

        Rigidbody rightFoot = CreateBodyPart("Right Foot", new Vector3(0.15f, 0.04f, 0.10f), new Vector3(0.26f, 0.08f, 0.46f), 1f);

        leftFoot.gameObject.AddComponent<CrossStitchGroundContact>();
        rightFoot.gameObject.AddComponent<CrossStitchGroundContact>();


        //Hips and Knees
        Connect(leftThigh, pelvis, new Vector3(-0.15f, 0.90f, 0f), 45f);
        Connect(rightThigh, pelvis, new Vector3(0.15f, 0.90f, 0f), 45f);
        Connect(leftShin, leftThigh, new Vector3(-0.15f, 0.49f, 0f), 60f);
        Connect(rightShin, rightThigh, new Vector3(0.15f, 0.49f, 0f), 60f);
        
        //Shoulders and Elbows
        Connect(leftUpperArm, torso, new Vector3(-0.31f, 1.66f, 0f), 65f);
        Connect(rightUpperArm, torso, new Vector3(0.31f, 1.66f, 0f), 65);
        Connect(leftForearm, leftUpperArm, new Vector3(-0.40f, 1.28f, 0f), 65f);
        Connect(rightForearm, rightUpperArm, new Vector3(0.40f, 1.28f, 0f), 65f);

        Connect(leftFoot, leftShin, new Vector3(-0.15f, 0.08f, 0f), 20f);
        Connect(rightFoot, rightShin, new Vector3(0.15f, 0.08f, 0f), 20f);

        
        
        


        Rigidbody[] parts =
        {
            pelvis, torso, head, leftThigh, rightThigh, leftShin, rightShin, leftFoot, rightFoot, leftUpperArm, rightUpperArm, leftForearm, rightForearm

        };

        for (int i = 0; i < parts.Length; i++)
        {
            for (int j = i + 1; j < parts.Length; j++)
            {
                Physics.IgnoreCollision(parts[i].GetComponent<Collider>(), parts[j].GetComponent<Collider>()
                );
            }
        }

            CrossStitchBalance oldBalance = gameObject.GetComponent<CrossStitchBalance>();
            
            if (oldBalance != null)
            {
                oldBalance.enabled = false;
            }
            pelvis.isKinematic = false;

   BehaviorParameters behavior = GetComponent<BehaviorParameters>();

   if (behavior == null)
        {
            behavior = gameObject.AddComponent<BehaviorParameters>();
        }

    
        behavior.BrainParameters.VectorObservationSize = 178;
        behavior.BrainParameters.NumStackedVectorObservations = 1;

    behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(48);
    behavior.BehaviorType = BehaviorType.Default;

    behavior.BehaviorName = "CrossStitchWalk";



    behavior.Model = trainedModel; 
    behavior.BehaviorType = trainedModel != null
    ? BehaviorType.InferenceOnly : BehaviorType.Default;
    


    CrossStitchAgent agent = gameObject.AddComponent<CrossStitchAgent>();
    agent.showTrainingPanel = showTrainingPanel;

    DecisionRequester requester = gameObject.AddComponent<DecisionRequester>();
    requester.DecisionPeriod = 5;
    requester.TakeActionsBetweenDecisions = true;




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

    joint.angularYMotion = ConfigurableJointMotion.Limited;
    joint.angularZMotion = ConfigurableJointMotion.Limited;

    joint.lowAngularXLimit = new SoftJointLimit { limit = -angleLimit };
    joint.highAngularXLimit = new SoftJointLimit { limit = angleLimit };
    joint.angularYLimit = new SoftJointLimit { limit = angleLimit };
    joint.angularZLimit = new SoftJointLimit { limit = angleLimit };
    joint.enableCollision = false;

    joint.configuredInWorldSpace = false;
    joint.rotationDriveMode = RotationDriveMode.Slerp;

    joint.slerpDrive = new JointDrive
    {
        positionSpring = 600f, positionDamper = 60f, maximumForce = 1000f
    };

    joint.targetRotation = Quaternion.identity;
    joint.targetAngularVelocity = Vector3.zero;

    }
}