    using UnityEngine;
    using Unity.MLAgents;
    using Unity.MLAgents.Sensors;
    using Unity.MLAgents.Actuators;


    public class CrossStitchAgent : Agent
    {

        public enum TrainingStage 
        {
            LiftPractice, Walking, Crossing
        }

        public TrainingStage trainingStage = TrainingStage.LiftPractice; 
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

        private CrossStitchJointMotor[] motors;
        private float liftHoldTime;

        private Vector3[] startingPositions;
        private Quaternion[] startingRotations;
        private CrossStitchGroundContact[] footContacts;

        private bool initialized; 

        private const float WalkDistance = 3f;

        private const float WalkTimeLimit = 30f;

        private Vector3 walkStart;
        private float previousProgress;

        private Collider leftFootCollider;
        private Collider rightFootCollider;

        private bool liftLeftNext;
        private float lastStepDistance;

        private float bestLiftClearance;
        private float supportingTime;
        private float landingTime;


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

            motors = GetComponentsInChildren<CrossStitchJointMotor>();

            System.Array.Sort(motors, (a, b) => string.CompareOrdinal(a.name, b.name));

            if (motors.Length != 12)
            {
                throw new System.InvalidOperationException("There has been an expected amount of 12 joint motors. Please Update the CrossStitchRagdoll.Connect() Scriopt first.");
            };
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

            GUILayout.Label($"Completed Steps: {completedSteps}");

            float distance = pelvis.position.z - course.Origin.z;
            GUILayout.Label($"Forward Distances: {distance:F2} m");
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
        const float targetClearance = 0.06f;
        const float requiredLiftHold = 0.12f;
        const float requiredLandingHold = 0.10f;
        float dt = Time.fixedDeltaTime;

        int movingIndex = liftLeftNext ? 0 : 1;
        int supportIndex = liftLeftNext ? 1 : 0;

        Collider movingFoot = liftLeftNext ? leftFootCollider : rightFootCollider;

        bool movingGrounded = footContacts[movingIndex].Grounded;
        bool supportGrounded = footContacts[supportIndex].Grounded;
        
        bool balanced = upright >= 0.85f && pelvis.position.y - course.Origin.y >= 0.8f;
        
        if (episodeTime < 0.3f)
        return;
        
        supportingTime = supportGrounded && balanced ? supportingTime + dt : 0f;
        
        if (!stepInProgress)
            {
                if (movingGrounded)
                stepStartZ = movingFoot.bounds.center.z;
                if (supportingTime < 0.1f)
                {
                    liftHoldTime = 0f;
                return;
                }

                float clearance = Mathf.Clamp(FootClearance(movingFoot), 0f, targetClearance);

                float improvement = Mathf.Max(0f, clearance - bestLiftClearance);

                AddReward(0.1f * improvement / targetClearance);

                bestLiftClearance = Mathf.Max(bestLiftClearance, clearance);

                bool raised = !movingGrounded && clearance >= targetClearance;

                liftHoldTime = raised ? liftHoldTime + dt : 0f;

                if (liftHoldTime >= requiredLiftHold)
                {
                    stepInProgress = true;
                    landingTime = 0f;
                }
                return;
            }

            if (!supportGrounded || !balanced)
        {
            stepInProgress = false;
            liftHoldTime = 0f;
            landingTime = 0f;
            return;
        }
        
    

    bool stableLanding = movingGrounded && supportGrounded && balanced; 
    
    landingTime = stableLanding ? landingTime + dt : 0f; 
    if (landingTime < requiredLandingHold)
    return;

    bool forwardStep = movingFoot.bounds.center.z - stepStartZ >= 0.10f && distance - lastStepDistance >= 0.03f;

    bool accepted = trainingStage == TrainingStage.LiftPractice || forwardStep;

    if (accepted)
        {
            AddReward(0.15f);

            if (trainingStage != TrainingStage.LiftPractice)
            {
                float progress = Mathf.Clamp(distance, 0f, WalkDistance);
                float gain = Mathf.Clamp(progress - previousProgress, 0f, 0.45f);

                AddReward(0.5f * gain);

                previousProgress = Mathf.Max(previousProgress, progress);
            }
            completedSteps++;
            liftLeftNext = !liftLeftNext;
            lastStepDistance = distance;

            bestLiftClearance = 0f;
          
        }

            stepInProgress = false;
            liftHoldTime = 0f;
            supportingTime = 0f;
            landingTime = 0f;
    }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!initialized)
            return;



            var controls = actions.ContinuousActions;

            if (controls.Length != motors.Length * 4)
        {
            throw new System.InvalidOperationException("Expected 48 Continuous Outcoming Actions: Four Per each Motor.");
        }

            for (int i = 0; i < motors.Length; i++)
            {
    
                int index = i * 4;

                motors[i].Apply(controls[index], controls[index], controls[index + 2], controls[index + 3], Time.fixedDeltaTime);
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

       if (trainingStage != TrainingStage.LiftPractice && distance >= WalkDistance && upright >= 0.8f && supported && completedSteps >= 4)
        {
            AddReward(1f);
            FinishStandingAttempt("Crossed safely", reachedFinish: true);
           
            return;

        }

       if (trainingStage == TrainingStage.LiftPractice && completedSteps >= 2)
        {
            AddReward(0.5f);
            FinishStandingAttempt("Two alternating lift-and-land cycles have been accurately and successfully completed", reachedFinish: true);

            return;
        }
        
        if (episodeTime >= WalkTimeLimit)
        {
            FinishStandingAttempt("Lift Practice had Timed Out");
            
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


           foreach(CrossStitchJointMotor motor in motors)
        {
            motor.ResetMotor();
        }
        liftHoldTime = 0f;
            ResetBody();
            walkStart = transform.InverseTransformPoint(pelvis.position);


            previousProgress = 0f;
            liftLeftNext = Random.value < 0.5f;
            lastStepDistance = 0f;

            stepInProgress = false;
            stepStartZ = 0f;
            completedSteps = 0;

            bestLiftClearance = 0f;
            supportingTime = 0f;
            landingTime = 0f;

            foreach (CrossStitchCar car in course.Cars)
        {
            car.gameObject.SetActive(trainingStage == TrainingStage.Crossing);
        }
            
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

