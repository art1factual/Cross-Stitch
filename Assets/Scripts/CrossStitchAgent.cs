using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


public class CrossStitchAgent : Agent
{
    private float episodeTime;

    [SerializeField] private bool logEpisodeStats = true;

    public bool showTrainingPanel = true;

    private int attemptCount;
    private float longestAttempt;
    private float lastAttemptTime;
    private float lastAttemptReward;
    private string lastOutcome = "Waiting for first result";

    private int successfulWalks;
    private float fastestWalk = float.PositiveInfinity;


    private Rigidbody[] bodyParts;

    private Rigidbody pelvis;

    private ConfigurableJoint[] joints;

    private Vector3[] startingPositions;
    private Quaternion[] startingRotations;
    private CrossStitchGroundContact[] footContacts;

    private bool initialized; 

    private const float WalkDistance = 2f;

    private const float WalkTimeLimit = 10f;

    private Vector3 walkStart;
    private float previousProgress;

    private Collider leftFootCollider;
    private Collider rightFootCollider;

    private bool liftLeftNext;
    private float lastStepDistance;

    public override void Initialize()
    {
        bodyParts = GetComponentsInChildren<Rigidbody>();

        joints = GetComponentsInChildren<ConfigurableJoint>();
        System.Array.Sort(joints, (a, b) => string.CompareOrdinal(a.name, b .name));
        footContacts = GetComponentsInChildren<CrossStitchGroundContact>();

        System.Array.Sort(footContacts, (a, b) => string.CompareOrdinal(a.name, b.name));
        System.Array.Sort(bodyParts, (a, b) => string.CompareOrdinal(a.name, b.name));

        pelvis = transform.Find("Pelvis").GetComponent<Rigidbody>();

        leftFootCollider = transform.Find("Left Foot").GetComponent<Collider>();
        rightFootCollider = transform.Find("Right Foot").GetComponent<Collider>();
        startingPositions = new Vector3[bodyParts.Length];
        startingRotations = new Quaternion[bodyParts.Length];

        for (int i = 0; i < bodyParts.Length; i++)
        {
            Rigidbody part = bodyParts[i];
            startingPositions[i] = transform.InverseTransformPoint(part.position);
            startingRotations[i] = Quaternion.Inverse(transform.rotation) * part.rotation;

        }

        MaxStep = 0;
        initialized = true;

        Debug.Log($"Agent initialized: {bodyParts.Length} bodies, {footContacts.Length} foot sensors.", this);
     }

    private void FinishStandingAttempt(string outcome, bool reachedFinish = false)
    {
        attemptCount++;
        lastAttemptTime = episodeTime;
        lastAttemptReward = GetCumulativeReward();
        lastOutcome = outcome;
        longestAttempt = Mathf.Max(longestAttempt, episodeTime);

        if (reachedFinish)
        {
            successfulWalks++;
            fastestWalk = Mathf.Min(fastestWalk, episodeTime);

        }

        if (logEpisodeStats)
        {
            Debug.Log($"Attempt {attemptCount} | {outcome} | " + $"Duration: {lastAttemptTime:F2}s | " + $"Longest: {longestAttempt:F2}s | Reward: {lastAttemptReward:F2}", this);
 
        }
        EndEpisode();
    }


    private float FootClearance(Collider foot)
    {
        return Mathf.Max(0f, foot.bounds.min.y - transform.position.y);
    }

    
private void OnGUI()
    {
        if (!showTrainingPanel || !initialized)
        return;

        GUILayout.BeginArea(new Rect(16f, 16f, 310f, 250f), GUI.skin.box);

        GUILayout.Label("CROSS-STITCHING");
        GUILayout.Label($"Attempt {attemptCount + 1} | Time {episodeTime:F2}s");
        GUILayout.Label($"Current reward {GetCumulativeReward():F2}");
        GUILayout.Label($"Longest attempt {longestAttempt:F2}s");

        GUILayout.Space(6f);
        GUILayout.Label($"Last result: {lastOutcome}");

        if (attemptCount > 0)
        GUILayout.Label($"Last time {lastAttemptTime:F2}s | Reward {lastAttemptReward:F2}");

        string fastest = successfulWalks > 0 ? $"{fastestWalk:F2}s" : "_";
        GUILayout.Label($"Walk finished {successfulWalks}/{attemptCount} / Fastest {fastest}");
        
        foreach (CrossStitchGroundContact foot in footContacts)
        GUILayout.Label($"{foot.name}: {(foot.Grounded ? "Grounded" : "Airborne")}");

        float tilt = Vector3.Angle(pelvis.rotation * Vector3.up, Vector3.up);
        GUILayout.Label($"Pelvis tilt {tilt:F0}");
        GUILayout.EndArea();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach(Rigidbody part in bodyParts)
        {
            Vector3 position = transform.InverseTransformPoint(part.position);

            sensor.AddObservation(Vector3.ClampMagnitude(position / 3f, 1f));

            Quaternion rotation = Quaternion.Inverse(transform.rotation) * part.rotation;

            sensor.AddObservation(rotation);

            Vector3 velocity = transform.InverseTransformDirection(part.linearVelocity);

            sensor.AddObservation(Vector3.ClampMagnitude(velocity / 10f, 1f));

            Vector3 angularVelocity = transform.InverseTransformDirection(part.angularVelocity);

            sensor.AddObservation(Vector3.ClampMagnitude(angularVelocity / 15f, 1f));

          
        }
        
          
            
        sensor.AddObservation(pelvis.transform.up);

        foreach (CrossStitchGroundContact contact in footContacts)
        {
            sensor.AddObservation(contact.Grounded);

        }

        
            sensor.AddObservation(Mathf.Clamp01(FootClearance(leftFootCollider) / 0.3f));

            sensor.AddObservation(Mathf.Clamp01(FootClearance(rightFootCollider) / 0.3f));

            sensor.AddObservation(liftLeftNext);
    }

    private void RewardFootLift(float distance, float upright)
    {
        if (upright < 0.7f || distance < lastStepDistance + 0.05f)
        return;

        bool leftLifted = !footContacts[0].Grounded && FootClearance(leftFootCollider) >= 0.04f;

        bool rightLifted = !footContacts[1].Grounded && FootClearance(rightFootCollider) >= 0.04f;

        bool completedLift = liftLeftNext ? leftLifted && footContacts[1].Grounded : rightLifted && footContacts[0].Grounded;

        if (!completedLift) 
        return;

        AddReward(0.05f);
        lastStepDistance = distance;
        liftLeftNext = !liftLeftNext;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var controls = actions.ContinuousActions;
        for (int i = 0; i < joints.Length; i++)
        {
            ConfigurableJoint joint = joints[i];

            bool isLegJoint = joint.name.EndsWith("Thigh") || joint.name.EndsWith("Shin");

            float angleScale = isLegJoint ? 0.9f : 0.5f;
            int index = i * 4;

            float x = Mathf.Clamp(controls[index], -1f, 1f) * joint.highAngularXLimit.limit * angleScale;
            float y = Mathf.Clamp(controls[index + 1], -1f, 1f) * joint.angularYLimit.limit * 0.5f;
            float z = Mathf.Clamp(controls[index + 2], -1f, 1f) * joint.angularZLimit.limit * 0.5f;
            
            joint.targetRotation = Quaternion.Inverse(Quaternion.Euler(x, y, z));

            float strength = Mathf.Clamp(controls[index + 3], -1f, 1f);
            JointDrive drive = joint.slerpDrive;
            drive.positionSpring = 600f + 400f * strength;
            joint.slerpDrive = drive;

    
        }
        ScoreWalking(controls);

        
    }

    private void ScoreWalking(ActionSegment<float> controls)
    {
        float dt = Time.fixedDeltaTime;
        episodeTime += dt;
        Vector3 position = transform.InverseTransformPoint(pelvis.position);
        float upright = Vector3.Dot(pelvis.rotation * Vector3.up, Vector3.up);

        if (episodeTime > 0.3f && (position.y < 0.55f || upright < 0.4f))
        {
            AddReward(-1f);
            FinishStandingAttempt("Fell");
            return;

        }

        float distance = position.z - walkStart.z;

        RewardFootLift(distance, upright);

        float progress = Mathf.Clamp(distance / WalkDistance, -1f, 1f);
        float progressDelta = progress - previousProgress;
        previousProgress = progress;

        float posture = Mathf.InverseLerp(0.4f, 0.95f, upright);
        float forwardReward = Mathf.Max(0f, progressDelta) * posture;

        float backwardPenalty = Mathf.Min(0f, progressDelta);
        float sidewaysDrift = Mathf.Clamp01(Mathf.Abs(position.x - walkStart.x));

        float effort = 0f;

        for (int i = 0; i < joints.Length; i++)
        {
            int index = i *4;
            float x = Mathf.Clamp(controls[index], -1f, 1f);
            float y = Mathf.Clamp(controls[index + 1], -1f, 1f);
            float z = Mathf.Clamp(controls[index + 2], -1f, 1f);
            effort += x * x + y * y + z * z;

        }

        effort /= 3f * joints.Length;

        float runningCost = 0.10f + 0.05f * sidewaysDrift + 0.02f * effort;
        AddReward(forwardReward + backwardPenalty - runningCost * dt);

        if (distance >= WalkDistance && upright >= 0.7f)
        {
            AddReward(1f);
            FinishStandingAttempt("Reached finished", reachedFinish: true);
            return;
        }
        if (episodeTime >= WalkTimeLimit)
        FinishStandingAttempt("Timed out");
    }


    private void ScoreStanding()
    {
        float dt = Time.fixedDeltaTime;
        episodeTime += dt;
        
        float pelvisHeight = transform.InverseTransformPoint(pelvis.position).y;
        float upright = Vector3.Dot(pelvis.transform.up, Vector3.up);

        if (episodeTime > 0.3f && (pelvisHeight < 0.55f || upright < 0.4f))
        {
            AddReward(-1f);
            FinishStandingAttempt("Fell");
            return;
        }
        int groundedFeet = 0;

        foreach (CrossStitchGroundContact foot in footContacts)
        {
            if (foot.Grounded)
            groundedFeet++;
        }

        float footSupport = groundedFeet * 0.5f;
        float heightScore = Mathf.Clamp01(pelvisHeight / 1.05f);
        float uprightScore = Mathf.Clamp01(upright);

        AddReward(dt * ( 0.5f * uprightScore + 0.3f * heightScore + 0.2f * footSupport));

        if (episodeTime >= 10f)
        FinishStandingAttempt("Reached Time Limit");
    }

    public override void Heuristic( in ActionBuffers actionsOut)
    {
        var controls = actionsOut.ContinuousActions;

        for (int i = 0; i < controls.Length; i++)
        controls[i] = 0f;

    }

    public override void OnEpisodeBegin()
    {
        if (!initialized)
        return;

        episodeTime = 0f;

        foreach (ConfigurableJoint joint in joints)
        {
            joint.targetRotation = Quaternion.identity;

            JointDrive drive = joint.slerpDrive;
            drive.positionSpring = 600f;
            joint.slerpDrive = drive;
        }
        ResetBody();
        walkStart = transform.InverseTransformPoint(pelvis.position);


        previousProgress = 0f;
        liftLeftNext = Random.value < 0.5f;
        lastStepDistance = 0f;
    }
    private void ResetBody()
    {
        for (int i = 0; i < bodyParts.Length; i++)
        {
            Rigidbody part = bodyParts[i];
            part.isKinematic = false;

            part.position = transform.TransformPoint(startingPositions[i] + Vector3.up * 0.02f);
            part.rotation = transform.rotation * startingRotations[i];

            part.linearVelocity = Vector3.zero;
            part.angularVelocity = Vector3.zero;

            part.WakeUp();

        }

        foreach (CrossStitchGroundContact contact in footContacts)
        {
            contact.ClearContacts();
        }

        Physics.SyncTransforms();

        Debug.Log($"Reset pelvis: {pelvis.position:F3} | Ragdoll root: {transform.position:F3}", this);
    }

    [ContextMenu("Test Episode Reset")]
    private void TestEpisodeReset()
    {
        if (!Application.isPlaying || !initialized)
        return;
        EndEpisode();
    }
}

