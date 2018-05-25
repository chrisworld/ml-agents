using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DogAgent : Agent {

    [Header("Target To Walk Towards")] 
    [Space(10)] 
    public Transform target;
    public bool respawnTargetWhenTouched;
    public float targetSpawnRadius;


    [Header("Body Parts")] 
    [Space(10)] 
    public Transform body;
    public Transform leg0_upper;
    public Transform leg1_upper;
    public Transform leg2_upper;
    public Transform leg3_upper;
    public Transform leg0_lower;
    public Transform leg1_lower;
    public Transform leg2_lower;
    public Transform leg3_lower;
    public Dictionary<Transform, BodyPart> bodyParts = new Dictionary<Transform, BodyPart>();
    public List<BodyPart> bodyPartsList = new List<BodyPart>();


    [Header("Joint Settings")] 
    [Space(10)] 
	public float maxJointSpring;
	public float jointDampen;
	public float maxJointForceLimit;
	public Vector3 footCenterOfMassShift; //used to shift the centerOfMass on the feet so the agent isn't so top heavy
	public Vector3 bodyCenterOfMassShift; //used to shift the centerOfMass on the feet so the agent isn't so top heavy
	Vector3 dirToTarget;
	float movingTowardsDot;
	float facingDot;


    [Header("Reward Functions To Use")] 
    [Space(10)] 
    public bool rewardMovingTowardsTarget; //agent should move towards target
    public bool rewardFacingTarget; //agent should face the target
    public bool rewardUseTimePenalty; //hurry up


    [Header("Foot Grounded Visualization")] 
    [Space(10)] 
    public bool useFootGroundedVisualization;
    public MeshRenderer foot0;
    public MeshRenderer foot1;
    public MeshRenderer foot2;
    public MeshRenderer foot3;
    public Material groundedMaterial;
    public Material unGroundedMaterial;
    List<Rigidbody> allRBs = new List<Rigidbody>();
    public bool isNewDecisionStep;
    public int currentAgentStep;
    public float maxJointAngleChangePerDecision = 10f;
    public float maxJointStrengthChangePerDecision = .1f; 
    // Vector3 currentAvgCoM;

    public bool testJointRotation;
    public Vector3 targetRotUpper;
    public Vector3 targetRotLower;

    SpeedUI speedUI;

//force = spring * (targetPosition - position) + damping * (targetVelocity - velocity)


    /// <summary>
    /// Used to store relevant information for acting and learning for each body part in agent.
    /// </summary>
    [System.Serializable]
    public class BodyPart
    {
        public ConfigurableJoint joint;
        public Rigidbody rb;
        public Vector3 startingPos;
        public Quaternion startingWorldRot;
        public Quaternion startingLocalRot;
        public DogContact groundContact;
		public DogAgent agent;
        public Vector3 previousJointRotation;
        public float previousSpringValue;

        /// <summary>
        /// Reset body part to initial configuration.
        /// </summary>
        public void Reset()
        {
            rb.transform.position = startingPos;
            rb.transform.rotation = startingWorldRot;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
			groundContact.touchingGround = false;;
        }
        
        /// <summary>
        /// Apply torque according to defined goal `x, y, z` angle and force `strength`.
        /// </summary>
        public void SetNormalizedTargetRotation(float x, float y, float z)
        {
            // Transform values from [-1, 1] to [0, 1]
            x = (x + 1f) * 0.5f;
            y = (y + 1f) * 0.5f;
            z = (z + 1f) * 0.5f;
        
            var xRot = Mathf.MoveTowards(previousJointRotation.x, Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x), agent.maxJointAngleChangePerDecision);
            var yRot = Mathf.MoveTowards(previousJointRotation.y, Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y), agent.maxJointAngleChangePerDecision);
            var zRot = Mathf.MoveTowards(previousJointRotation.z, Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z), agent.maxJointAngleChangePerDecision);

            // var xRot = Mathf.MoveTowards(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
            // var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
            // var xRot = Mathf.Lerp(previousJointRotation.x, joint.highAngularXLimit.limit, x);
            // var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y);
            // var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z);
            // if(joint == agent.bodyParts[agent.leg0_upper].joint)
            // {
            // // if(agent.isNewDecisionStep)
            // // {
            // //     print("NEW DECISION");
            // // }
            //     print("Time: " + Time.time + "xRot: " + xRot + " yRot: " + yRot + " zRot: " + zRot);
            // }

            joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
            previousJointRotation = new Vector3(xRot, yRot, zRot);


            // // var spring = Mathf.MoveTowards(previousSpringValue, (strength + 1f) * 0.5f, agent.maxJointStrengthChangePerDecision);
            // var rawSpringVal = ((strength + 1f) * 0.5f) * agent.maxJointSpring;
            // var clampedSpring = Mathf.MoveTowards(previousSpringValue, rawSpringVal, agent.maxJointStrengthChangePerDecision);
            // var jd = new JointDrive
            // {
            //     // positionSpring = ((strength + 1f) * 0.5f) * agent.maxJointSpring,
            //     // positionSpring = ((strength + 1f) * 0.5f) * agent.maxJointSpring,
            //     // positionSpring = spring * agent.maxJointSpring,
            //     positionSpring = clampedSpring,
			// 	positionDamper = agent.jointDampen,
            //     maximumForce = agent.maxJointForceLimit
            // };
            // joint.slerpDrive = jd;

            // previousSpringValue = jd.positionSpring;
        }


        public void UpdateJointDrive(float strength)
        {

            // var spring = Mathf.MoveTowards(previousSpringValue, (strength + 1f) * 0.5f, agent.maxJointStrengthChangePerDecision);
            var rawSpringVal = ((strength + 1f) * 0.5f) * agent.maxJointSpring;
            var clampedSpring = Mathf.MoveTowards(previousSpringValue, rawSpringVal, agent.maxJointStrengthChangePerDecision);
            var jd = new JointDrive
            {
                // positionSpring = ((strength + 1f) * 0.5f) * agent.maxJointSpring,
                // positionSpring = ((strength + 1f) * 0.5f) * agent.maxJointSpring,
                // positionSpring = spring * agent.maxJointSpring,
                positionSpring = clampedSpring,
				positionDamper = agent.jointDampen,
                maximumForce = agent.maxJointForceLimit
            };
            joint.slerpDrive = jd;

            // previousJointRotation = new Vector3(xRot, yRot, zRot);
            previousSpringValue = jd.positionSpring;
        }



    }

    /// <summary>
    /// Create BodyPart object and add it to dictionary.
    /// </summary>
    public void SetupBodyPart(Transform t)
    {
        BodyPart bp = new BodyPart
        {
            rb = t.GetComponent<Rigidbody>(),
            joint = t.GetComponent<ConfigurableJoint>(),
            startingPos = t.position,
            startingWorldRot = t.rotation,
            startingLocalRot = t.localRotation
        };
		bp.rb.maxAngularVelocity = 100;
        bodyParts.Add(t, bp);
        bp.groundContact = t.GetComponent<DogContact>();
		bp.agent = this;
        bodyPartsList.Add(bp);
    }


    void ShiftCoM()
    {

        //we want a lower center of mass or the crawler will roll over easily. 
        //these settings shift the COM on the lower legs
		bodyParts[leg0_lower].rb.centerOfMass = footCenterOfMassShift;
		bodyParts[leg1_lower].rb.centerOfMass = footCenterOfMassShift;
		bodyParts[leg2_lower].rb.centerOfMass = footCenterOfMassShift;
		bodyParts[leg3_lower].rb.centerOfMass = footCenterOfMassShift;
		bodyParts[body].rb.centerOfMass = bodyCenterOfMassShift;
    }

    void Awake()
    {
        speedUI = FindObjectOfType<SpeedUI>();
    }
    //Initialize
    public override void InitializeAgent()
    {
        //Setup each body part
        SetupBodyPart(body);
        SetupBodyPart(leg0_upper);
        SetupBodyPart(leg0_lower);
        SetupBodyPart(leg1_upper);
        SetupBodyPart(leg1_lower);
        SetupBodyPart(leg2_upper);
        SetupBodyPart(leg2_lower);
        SetupBodyPart(leg3_upper);
        SetupBodyPart(leg3_lower);



        allRBs.AddRange(gameObject.GetComponentsInChildren<Rigidbody>());
        currentAgentStep = 1;
    }


    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    // public void CollectObservationBodyPart(BodyPart bp)
    public void CollectObservationBodyPart(Transform bp)
    {
        ShiftCoM();
        var rb = bodyParts[bp].rb;
        AddVectorObs(bodyParts[bp].groundContact.touchingGround ? 1 : 0); // Is this bp touching the ground


        if(bp == leg0_upper || bp == leg1_upper || bp == leg2_upper || bp == leg3_upper )
        {
            AddVectorObs(rb.velocity);
            AddVectorObs(rb.angularVelocity);
            // Vector3 localPosRelToBody = body.InverseTransformPoint(rb.position);
            // AddVectorObs(localPosRelToBody);
            AddVectorObs(Quaternion.FromToRotation(-body.forward, rb.transform.forward)); //-forward is local up because capsule is rotated
        }

        // if(bp.rb.transform != body)
        // {
        //     // AddVectorObs(body.localRotation); //the capsule is rotated so this is local forward

        //     AddVectorObs(Quaternion.FromToRotation(-body.forward, bp.rb.transform.forward)); //-forward is local up because capsule is rotated
        //     // // AddVectorObs(Quaternion.FromToRotation(body.forward, bp.rb.transform.forward));
        //     // AddVectorObs(Quaternion.FromToRotation(body.up, bp.rb.transform.forward));  //up is local forward because capsule is rotated
        // }
    }



    // /// <summary>
    // /// Add relevant information on each body part to observations.
    // /// </summary>
    // public void CollectObservationBodyPart(BodyPart bp)
    // {
    //     ShiftCoM();
    //     var rb = bp.rb;
    //     AddVectorObs(bp.groundContact.touchingGround ? 1 : 0); // Is this bp touching the ground

    //     // AddVectorObs(rb.velocity);
    //     // AddVectorObs(rb.angularVelocity);
    //     Vector3 localPosRelToBody = body.InverseTransformPoint(rb.position);
    //     AddVectorObs(localPosRelToBody);

    //     if(bp.rb.transform != body)
    //     {
    //         // AddVectorObs(body.localRotation); //the capsule is rotated so this is local forward

    //         // AddVectorObs(Quaternion.FromToRotation(-body.forward, bp.rb.transform.forward));
    //         // // AddVectorObs(Quaternion.FromToRotation(body.forward, bp.rb.transform.forward));
    //         AddVectorObs(Quaternion.FromToRotation(body.up, bp.rb.transform.forward));  //up is local forward because capsule is rotated
    //     }
    // }

    // void FixedUpdate()
    // {
    //     //update pos to target
	// 	dirToTarget = target.position - bodyParts[body].rb.position;

    //     //if enabled the feet will light up green when the foot is grounded.
    //     //this is just a visualization and isn't necessary for function
    //     if(useFootGroundedVisualization)
    //     {
    //         foot0.material = bodyParts[leg0_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
    //         foot1.material = bodyParts[leg1_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
    //         foot2.material = bodyParts[leg2_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
    //         foot3.material = bodyParts[leg3_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
    //     }
    // }

	/// <summary>
    /// Adds the raycast hit dist and relative pos to observations
    /// </summary>
    void RaycastObservation(Vector3 pos, Vector3 dir, float maxDist)
    {
        RaycastHit hit;
        float dist = 0;
        Vector3 relativeHitPos = Vector3.zero;
        if(Physics.Raycast(pos, dir, out hit, maxDist))
        {
            if(hit.collider.CompareTag("ground"))
            {
                //normalized hit distance
                dist = hit.distance/maxDist; 

                //hit point position relative to the body's local space
                relativeHitPos = body.InverseTransformPoint(hit.point); 
                // print(hit.collider.gameObject.name);
            }
        }

        //add our raycast observation 
        AddVectorObs(dist);
        AddVectorObs(relativeHitPos);
    }

    public override void CollectObservations()
    {
        //normalize dir vector to help generalize
        AddVectorObs(dirToTarget.normalized);


        AddVectorObs(bodyParts[body].rb.velocity);
        AddVectorObs(bodyParts[body].rb.angularVelocity);
        // AddVectorObs(GetAvgCenterOfMassForAllRBs());

        //raycast out of the bottom of the legs to get information about where the ground is
        RaycastObservation(leg0_lower.position, leg0_lower.up, 1);
        RaycastObservation(leg1_lower.position, leg1_lower.up, 1);
        RaycastObservation(leg2_lower.position, leg2_lower.up, 1);
        RaycastObservation(leg3_lower.position, leg3_lower.up, 1);

        //forward & up to help with orientation
        // AddVectorObs(-body.forward); //this is local up
        // AddVectorObs(body.up); //the capsule is rotated so this is local forward


        // AddVectorObs(body.rotation); //the capsule is rotated so this is local forward
        if(dirToTarget != Vector3.zero)
        {
            AddVectorObs(Quaternion.LookRotation(dirToTarget));
            // print(Quaternion.LookRotation(dirToTarget));
        }
        else
        {
            AddVectorObs(Quaternion.identity);

        }


        // foreach (var bodyPart in bodyParts.Values)
        // {
        //     CollectObservationBodyPart(bodyPart);
        // }
        foreach (var bodyPart in bodyParts)
        {
            CollectObservationBodyPart(bodyPart.Key);
        }
    }





	/// <summary>
    /// Agent touched the target
    /// </summary>
	public void TouchedTarget(float impactForce)
	{
		AddReward(.01f * impactForce); //higher impact should be rewarded
        if(respawnTargetWhenTouched)
        {
		    GetRandomTargetPos();
        }
		Done();
	}

    Vector3 GetAvgCenterOfMassForAllRBs()
    {
        Vector3 CoM = Vector3.zero;
        float c = 0f;
        
        foreach (Rigidbody rb in allRBs)
        {
            CoM += rb.worldCenterOfMass * rb.mass;
            c += rb.mass;
        }
 
        CoM /= c;
        return body.InverseTransformPoint(CoM);
        // Debug.DrawRay(body.position, body.InverseTransformPoint(CoM), Color.red, .05f);
        // Debug.DrawRay(CoM, Vector3.up, Color.green, .05f);
        // // print(CoM);
        // print(body.InverseTransformPoint(CoM));
    }


    /// <summary>
    /// Moves target to a random position within specified radius.
    /// </summary>
    /// <returns>
    /// Move target to random position.
    /// </returns>
    public void GetRandomTargetPos()
    {
        Vector3 newTargetPos = Random.insideUnitSphere * targetSpawnRadius;
		newTargetPos.y = 5;
		target.position = newTargetPos;
    }


    //We only need to change the joint settings based on decision freq.
    public void IncrementDecisionTimer()
    {
        if(currentAgentStep == this.agentParameters.numberOfActionsBetweenDecisions || this.agentParameters.numberOfActionsBetweenDecisions == 1)
        {
            currentAgentStep = 1;
            isNewDecisionStep = true;
        }
        else
        {
            currentAgentStep ++;
            isNewDecisionStep = false;
        }
    }
    /// <summary>
    /// Apply torque according to defined goal `x, y, z` angle and force `strength`.
    /// </summary>
    // public void SetTargetJointRotation(ConfigurableJoint joint, Vector3 rot)
    public void SetTargetJointRotation(BodyPart bp, Vector3 rot)
    {
        // var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, curve.Evaluate(curveTimer));
        // var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, curve.Evaluate(curveTimer));
        // var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, curve.Evaluate(curveTimer));
        // joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
        bp.joint.targetRotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        // bp.joint.targetRotation = bp.startingLocalRot * Quaternion.Euler(rot.x, rot.y, rot.z);
        print(Quaternion.FromToRotation(bp.joint.axis, bp.joint.connectedBody.transform.rotation.eulerAngles).eulerAngles);
    }

	public override void AgentAction(float[] vectorAction, string textAction)
    {
        // print(bodyParts[body].rb.velocity.z);
        speedUI.CollectSpeed(bodyParts[body].rb.velocity.magnitude * 2.237f);
        // print(bodyParts[body].rb.velocity.magnitude * 2.237);

        if(testJointRotation)
        {
            // SetTargetRotationLocal (bodyParts[leg0_upper].joint, Quaternion.Euler (targetRotUpper.x,targetRotUpper.y,targetRotUpper.z), bodyParts[leg0_upper].startingLocalRot);
            // SetTargetRotationLocal (bodyParts[leg1_upper].joint, Quaternion.Euler (targetRotUpper.x,targetRotUpper.y,targetRotUpper.z), bodyParts[leg1_upper].startingLocalRot);
            // SetTargetRotationLocal (bodyParts[leg2_upper].joint, Quaternion.Euler (targetRotUpper.x,targetRotUpper.y,targetRotUpper.z), bodyParts[leg2_upper].startingLocalRot);
            // SetTargetRotationLocal (bodyParts[leg3_upper].joint, Quaternion.Euler (targetRotUpper.x,targetRotUpper.y,targetRotUpper.z), bodyParts[leg3_upper].startingLocalRot);

            SetTargetJointRotation(bodyParts[leg0_upper], targetRotUpper);
            SetTargetJointRotation(bodyParts[leg1_upper], targetRotUpper);
            SetTargetJointRotation(bodyParts[leg2_upper], targetRotUpper);
            SetTargetJointRotation(bodyParts[leg3_upper], targetRotUpper);
            // SetTargetJointRotation(bodyParts[leg0_lower], targetRotLower);
            // SetTargetJointRotation(bodyParts[leg1_lower], targetRotLower);
            // SetTargetJointRotation(bodyParts[leg2_lower], targetRotLower);
            // SetTargetJointRotation(bodyParts[leg3_lower], targetRotLower);
            return;
        }
        
        // GetAvgCenterOfMassForAllRBs();
        //update pos to target
		dirToTarget = target.position - bodyParts[body].rb.position;

        //if enabled the feet will light up green when the foot is grounded.
        //this is just a visualization and isn't necessary for function
        if(useFootGroundedVisualization)
        {
            foot0.material = bodyParts[leg0_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
            foot1.material = bodyParts[leg1_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
            foot2.material = bodyParts[leg2_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
            foot3.material = bodyParts[leg3_lower].groundContact.touchingGround? groundedMaterial: unGroundedMaterial;
        }







        if(isNewDecisionStep)
        {
            // // Apply action to all relevant body parts. 
            // bodyParts[leg0_upper].SetNormalizedTargetRotation(vectorAction[0], vectorAction[1], 0, vectorAction[2]);
            // bodyParts[leg1_upper].SetNormalizedTargetRotation(vectorAction[3], vectorAction[4], 0, vectorAction[5]);
            // bodyParts[leg2_upper].SetNormalizedTargetRotation(vectorAction[6], vectorAction[7], 0, vectorAction[8]);
            // bodyParts[leg3_upper].SetNormalizedTargetRotation(vectorAction[9], vectorAction[10], 0, vectorAction[11]);
            // bodyParts[leg0_lower].SetNormalizedTargetRotation(vectorAction[12], 0, 0, vectorAction[13]);
            // bodyParts[leg1_lower].SetNormalizedTargetRotation(vectorAction[14], 0, 0, vectorAction[15]);
            // bodyParts[leg2_lower].SetNormalizedTargetRotation(vectorAction[16], 0, 0, vectorAction[17]);
            // bodyParts[leg3_lower].SetNormalizedTargetRotation(vectorAction[18], 0, 0, vectorAction[19]);
            // Apply action to all relevant body parts. 
            bodyParts[leg0_upper].SetNormalizedTargetRotation(vectorAction[0], vectorAction[1], 0);
            bodyParts[leg1_upper].SetNormalizedTargetRotation(vectorAction[2], vectorAction[3], 0);
            bodyParts[leg2_upper].SetNormalizedTargetRotation(vectorAction[4], vectorAction[5], 0);
            bodyParts[leg3_upper].SetNormalizedTargetRotation(vectorAction[6], vectorAction[7], 0);
            // bodyParts[leg0_lower].SetNormalizedTargetRotation(vectorAction[8], 0, 0);
            // bodyParts[leg1_lower].SetNormalizedTargetRotation(vectorAction[9], 0, 0);
            // bodyParts[leg2_lower].SetNormalizedTargetRotation(vectorAction[10], 0, 0);
            // bodyParts[leg3_lower].SetNormalizedTargetRotation(vectorAction[11], 0, 0);
        }

            //update joint drive settings
            bodyParts[leg0_upper].UpdateJointDrive(vectorAction[8]);
            bodyParts[leg1_upper].UpdateJointDrive(vectorAction[9]);
            bodyParts[leg2_upper].UpdateJointDrive(vectorAction[10]);
            bodyParts[leg3_upper].UpdateJointDrive(vectorAction[11]);
            // bodyParts[leg0_lower].UpdateJointDrive(vectorAction[16]);
            // bodyParts[leg1_lower].UpdateJointDrive(vectorAction[17]);
            // bodyParts[leg2_lower].UpdateJointDrive(vectorAction[18]);
            // bodyParts[leg3_lower].UpdateJointDrive(vectorAction[19]);





        // if(isNewDecisionStep)
        // {
        //     // // Apply action to all relevant body parts. 
        //     // bodyParts[leg0_upper].SetNormalizedTargetRotation(vectorAction[0], vectorAction[1], 0, vectorAction[2]);
        //     // bodyParts[leg1_upper].SetNormalizedTargetRotation(vectorAction[3], vectorAction[4], 0, vectorAction[5]);
        //     // bodyParts[leg2_upper].SetNormalizedTargetRotation(vectorAction[6], vectorAction[7], 0, vectorAction[8]);
        //     // bodyParts[leg3_upper].SetNormalizedTargetRotation(vectorAction[9], vectorAction[10], 0, vectorAction[11]);
        //     // bodyParts[leg0_lower].SetNormalizedTargetRotation(vectorAction[12], 0, 0, vectorAction[13]);
        //     // bodyParts[leg1_lower].SetNormalizedTargetRotation(vectorAction[14], 0, 0, vectorAction[15]);
        //     // bodyParts[leg2_lower].SetNormalizedTargetRotation(vectorAction[16], 0, 0, vectorAction[17]);
        //     // bodyParts[leg3_lower].SetNormalizedTargetRotation(vectorAction[18], 0, 0, vectorAction[19]);
        //     // Apply action to all relevant body parts. 
        //     bodyParts[leg0_upper].SetNormalizedTargetRotation(vectorAction[0], vectorAction[1], 0);
        //     bodyParts[leg1_upper].SetNormalizedTargetRotation(vectorAction[2], vectorAction[3], 0);
        //     bodyParts[leg2_upper].SetNormalizedTargetRotation(vectorAction[4], vectorAction[5], 0);
        //     bodyParts[leg3_upper].SetNormalizedTargetRotation(vectorAction[6], vectorAction[7], 0);
        //     bodyParts[leg0_lower].SetNormalizedTargetRotation(vectorAction[8], 0, 0);
        //     bodyParts[leg1_lower].SetNormalizedTargetRotation(vectorAction[9], 0, 0);
        //     bodyParts[leg2_lower].SetNormalizedTargetRotation(vectorAction[10], 0, 0);
        //     bodyParts[leg3_lower].SetNormalizedTargetRotation(vectorAction[11], 0, 0);
        // }

        //     //update joint drive settings
        //     bodyParts[leg0_upper].UpdateJointDrive(vectorAction[12]);
        //     bodyParts[leg1_upper].UpdateJointDrive(vectorAction[13]);
        //     bodyParts[leg2_upper].UpdateJointDrive(vectorAction[14]);
        //     bodyParts[leg3_upper].UpdateJointDrive(vectorAction[15]);
        //     bodyParts[leg0_lower].UpdateJointDrive(vectorAction[16]);
        //     bodyParts[leg1_lower].UpdateJointDrive(vectorAction[17]);
        //     bodyParts[leg2_lower].UpdateJointDrive(vectorAction[18]);
        //     bodyParts[leg3_lower].UpdateJointDrive(vectorAction[19]);




        // Set reward for this step according to mixture of the following elements.
        if(rewardMovingTowardsTarget){RewardFunctionMovingTowards();}
        if(rewardFacingTarget){RewardFunctionFacingTarget();}
        if(rewardUseTimePenalty){RewardFunctionTimePenalty();}
        IncrementDecisionTimer();
    }
	
    //Reward moving towards target & Penalize moving away from target.
    void RewardFunctionMovingTowards()
    {
        //don't normalize vel. the faster it goes the more reward it should get
        //0.03f chosen via experimentation
		movingTowardsDot = Vector3.Dot(bodyParts[body].rb.velocity, dirToTarget.normalized); 
        AddReward(0.03f * movingTowardsDot);
    }

    //Reward facing target & Penalize facing away from target
    void RewardFunctionFacingTarget()
    {
        //0.01f chosen via experimentation.
		// facingDot = Vector3.Dot(dirToTarget.normalized, body.up);
        facingDot = Vector3.Dot(dirToTarget.normalized, body.up); //up is local forward because capsule is rotated

        AddReward(0.01f * facingDot);
    }

    //Time penalty - HURRY UP
    void RewardFunctionTimePenalty()
    {
        //0.001f chosen by experimentation. If this penalty is too high it will kill itself :(
        AddReward(- 0.001f); 
    }

	/// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void AgentReset()
    {
        if(dirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirToTarget);
        }
        
        foreach (var bodyPart in bodyParts.Values)
        {
            bodyPart.Reset();
        }
        currentAgentStep = 1;
        isNewDecisionStep = true;
    }










	/// <summary>
	/// Sets a joint's targetRotation to match a given local rotation.
	/// The joint transform's local rotation must be cached on Start and passed into this method.
	/// </summary>

    ///usage:  myJoint.SetTargetRotationLocal (Quaternion.Euler (0, 90, 0), startRotation);

	public void SetTargetRotationLocal (ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
	{
		if (joint.configuredInWorldSpace) {
			Debug.LogError ("SetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
		}
		SetTargetRotationInternal (joint, targetLocalRotation, startLocalRotation, Space.Self);
	}
	
	/// <summary>
	/// Sets a joint's targetRotation to match a given world rotation.
	/// The joint transform's world rotation must be cached on Start and passed into this method.
	/// </summary>
	public void SetTargetRotation (ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
	{
		if (!joint.configuredInWorldSpace) {
			Debug.LogError ("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
		}
		SetTargetRotationInternal (joint, targetWorldRotation, startWorldRotation, Space.World);
	}
	
	void SetTargetRotationInternal (ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
	{
		// Calculate the rotation expressed by the joint's axis and secondary axis
		var right = joint.axis;
		var forward = Vector3.Cross (joint.axis, joint.secondaryAxis).normalized;
		var up = Vector3.Cross (forward, right).normalized;
		Quaternion worldToJointSpace = Quaternion.LookRotation (forward, up);
		
		// Transform into world space
		Quaternion resultRotation = Quaternion.Inverse (worldToJointSpace);
		
		// Counter-rotate and apply the new local rotation.
		// Joint space is the inverse of world space, so we need to invert our value
		if (space == Space.World) {
			resultRotation *= startRotation * Quaternion.Inverse (targetRotation);
		} else {
			resultRotation *= Quaternion.Inverse (targetRotation) * startRotation;
		}
		
		// Transform back into joint space
		resultRotation *= worldToJointSpace;
		
		// Set target rotation to our newly calculated rotation
		joint.targetRotation = resultRotation;
	}
}




