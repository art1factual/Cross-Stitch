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

        private  int successfulWalks;

        private bool carHit;
        private CrossStitchCourse course;
        private float fastestWalk = float.PositiveInfinity;


        private Rigidbody[] bodyParts;

        private Rigidbody pelvis;

        private ConfigurableJoint[] joints;

        private Vector3[] startingPositions;
        private Quaternion[] startingRotations;
        private CrossStitchGroundContact[] footContacts;

        private bool initialized; 

        private const float WalkDistance = 3f;

        private const float WalkTimeLimit = 10f;

        private Vector3 walkStart;
        private float previousProgress;

        private Collider leftFootCollider;
        private Collider rightFootCollider;

        private bool liftLeftNext;
        private float lastStepDistance;

        private bool stepInProgress;
        private float stepStartZ;
        private int completedSteps;


        public void NotifyCarHit()
        {
            carHit = true;
        }
         private readonly RaycastHit[] obstacleHits = new RaycastHit[32];


        public override void Initialize()
        {
            course = FindAnyObjectByType<CrossStitchCourse>();

           
            if (course == null)
        {
            throw new System.InvalidOperationException("Create the course before adding CrossStitchAgent.");
        }
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

                CrossStitchBodyContact contact = part.GetComponent<CrossStitchBodyContact>();

                if (contact == null)
                {
                    contact = part.gameObject.AddComponent<CrossStitchBodyContact>();

                }
                contact.Owner = this;
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
            return Mathf.Max(0f, foot.bounds.min.y - course.Origin.y);
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
            GUILayout.Label($"Completed steps: {completedSteps}");
            GUILayout.EndArea();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            foreach(Rigidbody part in bodyParts)
            {
                Vector3 bodyOffset = part.position - pelvis.position;
                Vector3 position = transform.InverseTransformDirection(bodyOffset);         

                sensor.AddObservation(Vector3.ClampMagnitude(position / 3f, 1f));

                Quaternion rotation = Quaternion.Inverse(transform.rotation) * part.rotation;

                sensor.AddObservation(rotation);

                Vector3 velocity = transform.InverseTransformDirection(part.linearVelocity);

                sensor.AddObservation(Vector3.ClampMagnitude(velocity / 10f, 1f));

                Vector3 angularVelocity = transform.InverseTransformDirection(part.angularVelocity);

                sensor.AddObservation(Vector3.ClampMagnitude(angularVelocity / 15f, 1f));
            }

                sensor.AddObservation(stepInProgress);

                Vector3 goal = course.Origin + Vector3.forward * WalkDistance;
                goal.y = pelvis.position.y;

                Vector3 goalOffset = transform.InverseTransformDirection(goal - pelvis.position);

                sensor.AddObservation(Vector3.ClampMagnitude(goalOffset / 15f, 1f));
                for(int i = 0; i < CrossStitchCourse.CarCount; i++)
            {
                CrossStitchCar car = course.Cars[i];
                bool active = car.gameObject.activeInHierarchy;

                Vector3 offset = active ? transform.InverseTransformDirection(car.Position - pelvis.position) : Vector3.zero;

                Vector3 relativeVelocity = active ? transform.InverseTransformDirection(car.Velocity - pelvis.linearVelocity)
                : Vector3.zero;

                sensor.AddObservation(Vector3.ClampMagnitude(offset / 20f, 1f));
                sensor.AddObservation(Vector3.ClampMagnitude(relativeVelocity / 10f, 1f));

                sensor.AddObservation(active);
            } 
            for (int i = 0; i < 5; i++)
            {
                float angle = -90f + i * 45f;

                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

                sensor.AddObservation(ObstacleDistance(direction));
            }            
            
                
            sensor.AddObservation(pelvis.transform.up);

            foreach (CrossStitchGroundContact contact in footContacts)
            {
                sensor.AddObservation(contact.Grounded);

            }

            
                sensor.AddObservation(Mathf.Clamp01(FootClearance(leftFootCollider) / 0.3f));

                sensor.AddObservation(Mathf.Clamp01(FootClearance(rightFootCollider) / 0.3f));

                sensor.AddObservation(liftLeftNext);
                
                float pelvisHeight = pelvis.position.y - transform.position.y;
                sensor.AddObservation(Mathf.Clamp01(pelvisHeight / 2f));
        }

        private float ObstacleDistance(Vector3 direction)
    {
        const float range = 8f;

        Vector3 origin = new Vector3(pelvis.position.x, course.Origin.y + 0.5f, pelvis.position.z);

        int count = Physics.RaycastNonAlloc(origin, direction, obstacleHits, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        float nearest = range;

        for (int i = 0; i < count; i++)
        {
            Collider hit = obstacleHits[i].collider;

            if (hit.transform.IsChildOf(transform))
            continue;
            nearest = Mathf.Min(nearest, obstacleHits[i].distance);

        }

        return nearest / range;
    }

        private void RewardFootLift(float distance, float upright)
        {

            int movingIndex = liftLeftNext ? 0 : 1;
            int supportingIndex = liftLeftNext ? 1 : 0;

            Collider movingFoot = liftLeftNext ? leftFootCollider : rightFootCollider;

            bool movingGrounded = footContacts[movingIndex].Grounded;
            bool supportingGrounded = footContacts[supportingIndex].Grounded;

            float footZ = transform.InverseTransformPoint(movingFoot.bounds.center).z;

            if (!stepInProgress)
            {
                if (upright >= 0.7f && !movingGrounded && supportingGrounded && FootClearance(movingFoot) >= 0.04f)
                {
                    stepInProgress = true;
                    stepStartZ = footZ;
                }
                return;

            }

            if (!movingGrounded)
            return;

            bool advancedFoot = footZ >= stepStartZ + 0.05f;
            bool advancedBody = distance >= lastStepDistance + 0.05f;

            if (upright >= 0.7f && advancedFoot && advancedBody)
            {
                AddReward(0.1f);
                completedSteps++;
                lastStepDistance = distance;
                liftLeftNext = !liftLeftNext;

            }


            stepInProgress = false;
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

            if (carHit)
            {
                AddReward(-1f);
                FinishStandingAttempt("Hit by Car");
                return;
            }

        float height = pelvis.position.y - course.Origin.y;
        float upright = Vector3.Dot(pelvis.transform.up, Vector3.up);

        float distance = pelvis.position.z - course.Origin.z;
        float sideways = Mathf.Abs(pelvis.position.x - course.Origin.x);

        if (episodeTime  > 0.4f && (height < 0.55f || upright < 0.4f))
        {
            AddReward(-1f);
            FinishStandingAttempt("Fell");
            return;

        }

        if (sideways > course.HalfWidth || distance < -2f)
        {
            AddReward(-1f);
            FinishStandingAttempt("Left Course");
            return;
        }

        RewardFootLift(distance, upright);

        float progress = Mathf.Clamp(distance, 0f, WalkDistance);
        float gain = Mathf.Max(0f, progress - previousProgress);

        previousProgress = Mathf.Max(previousProgress, progress);

        float posture = Mathf.InverseLerp(0.4f, 0.95f, upright);

        AddReward(gain * 0.5f * posture);
        float sliding = 0f;

        if (footContacts[0].Grounded)
        {
            sliding += HorizontalSpeed(leftFootCollider.attachedRigidbody);

        }

        if (footContacts[1].Grounded)
        {
            sliding += HorizontalSpeed(rightFootCollider.attachedRigidbody);
        }

        AddReward(-0.01f * Mathf.Min(sliding, 4f) * dt);
        
        bool supported = footContacts[0].Grounded || footContacts[1].Grounded;

        if (distance >= WalkDistance && upright >= 0.8f && supported && completedSteps >= 4)
        {
            AddReward(1f);
            FinishStandingAttempt("Crossed safely", reachedFinish: true);
           
            return;

        }
        
        if (episodeTime >= WalkTimeLimit)
        {
            AddReward(-0.5f);
            FinishStandingAttempt("Walking time Limit");
            return;
        }
        
        }
        private static float HorizontalSpeed(Rigidbody body)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;

            return velocity.magnitude;
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
            carHit = false;


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

            stepInProgress = false;
            stepStartZ = 0f;
            completedSteps = 0;
            
            course.ResetTraffic();
            Physics.SyncTransforms();

            
            

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

