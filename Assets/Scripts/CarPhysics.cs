using UnityEngine;
using System.Collections;

public class CarPhysics : MonoBehaviour {

	public Wheel[] wheels;

	public float steerMax = 28f;
	public float brakeTorqueMax = 3000f;
	public float eBrakeTorqueMax = 2000f;

	public Vector3 dragMultiplier = new Vector3(0.02f, 0.01f, 0.01f);
	//public float rollingResistance = 0.3f;

	public Transform centerOfMass;

	public AnimationCurve torqueCurve;
	public float[] gearRatios;
	public float diffRatio;
	public int currentGear = 1;
	public bool automaticTrans = true;
	public float autoShiftUp = 4200;
	public float autoShiftDown = 2800;
	public float idleRPM = 700;
	public float redline = 7000;
	
	public float engineRPM = 0f;
	public float speed = 0f;
	public float inertiaFactor = 1.5f;
	public float slipVeloForward = 0;
	

	public float steer = 0;
	//private float forward = 0;
	//private float back = 0;
	public float throttle = 0;
	public float brake = 0;
	public bool reverse = false;
	public bool eBrakeOn = false;
	private int driveWheelCount = 0;

	void Start () {
		rigidbody.centerOfMass = centerOfMass.localPosition;
		torqueCurve.AddKey(700,700); //rpm,torque in nm
		torqueCurve.AddKey(4000,855);
		torqueCurve.AddKey(6200,600);

		foreach(Wheel wheel in wheels) {
			wheel.localOrigin = wheel.mesh.transform.localPosition;
			if ( wheel.driveWheel ) {
				driveWheelCount++;
			}
		}
		rigidbody.inertiaTensor *= inertiaFactor;
	}

	void Update () {
		speed = Vector3.Dot(rigidbody.velocity, transform.forward);
		UpdateWheelPosition();
		UpdateForwardSlipVelo();
	}
	public void SetControls (ControlProxy control) {
		steer = control.steer;
		throttle = control.throttle;
		brake = control.brake;
		reverse = control.reverse;
		currentGear = control.gear;
		eBrakeOn = control.eBrake;
	}
	void FixedUpdate () {

		UpdateEngineTorque();

		//actually apply brakes and steering here
		foreach(Wheel wheel in wheels) {
			wheel.collider.brakeTorque = brakeTorqueMax * brake;
			if( eBrakeOn && wheel.eBrake ) {
				wheel.collider.brakeTorque += eBrakeTorqueMax;
			}
			if( wheel.steerWheel ) {
				wheel.collider.steerAngle = steerMax * steer;
			}
		}
		Vector3 relativeVelocity = transform.InverseTransformDirection(rigidbody.velocity);
		UpdateDrag(relativeVelocity);
		//bumps are ignoreed if the car is going really slow
		if (speed > 2) UpdateBumps();
	}
	void UpdateEngineTorque()
	{
		float wheelRPM = 0.0f;
		foreach(Wheel wheel in wheels) {
			if( wheel.driveWheel ) {
				wheelRPM += wheel.collider.rpm;
			}
		}
		wheelRPM /= driveWheelCount;
		if (currentGear == 0) {
			engineRPM = throttle * (redline - idleRPM) + idleRPM;
		} else {
			engineRPM = Mathf.Clamp(wheelRPM * gearRatios[currentGear] * diffRatio, idleRPM, redline);
		}
		float totalWheelTorque = torqueCurve.Evaluate(engineRPM) * gearRatios[currentGear] * diffRatio * throttle;
		float wheelResistance = CalculateRollingResistance();
		float minTorque = rigidbody.mass * throttle / driveWheelCount;

		if ( reverse ) {
			totalWheelTorque *= -1;
			wheelResistance *= -1;
		}
		foreach(Wheel wheel in wheels) {
			if( wheel.driveWheel ) {
				wheel.collider.motorTorque = (totalWheelTorque - ( wheelResistance * wheel.collider.radius)) / driveWheelCount;
				wheel.collider.motorTorque = wheel.collider.motorTorque < minTorque ? minTorque : wheel.collider.motorTorque;
				Debug.Log("torque:" + totalWheelTorque + "  resistance:" + wheelResistance * wheel.collider.radius);
				
			}
		}
	}
	float CalculateRollingResistance () {
		float rollingResistance = 0;
		foreach(Wheel wheel in wheels) {
			rollingResistance += wheel.baseRollingResistance + wheel.terrainResistanceEffect * wheel.terrainResistanceInfluence;
			//Debug.Log("wheelRollRes:" + rollingResistance);
		}
		rollingResistance /= wheels.Length;
		//Vector3 rollingFactor = -transform.forward * rollingResistance * rigidbody.mass;
		//rollingFactor = Vector3.zero;
		return rollingResistance * rigidbody.mass;
	}
	void UpdateDrag (Vector3 relativeVelocity) {
		Vector3 relativeDrag = new Vector3( -relativeVelocity.x * Mathf.Abs(relativeVelocity.x),
		                               -relativeVelocity.y * Mathf.Abs(relativeVelocity.y),
		                               -relativeVelocity.z * Mathf.Abs(relativeVelocity.z));
		Vector3 drag = Vector3.Scale(relativeDrag, dragMultiplier);
		rigidbody.AddForce( drag , ForceMode.Force);
	}
	void UpdateWheelPosition () {
		WheelHit hit;
		float travel;
		bool grounded;
		Quaternion wheelRotation;
		foreach(Wheel wheel in wheels) {
			grounded = wheel.collider.GetGroundHit(out hit);
			if( grounded ) {
				float raycastY = -wheel.collider.transform.InverseTransformPoint(hit.point).y;
				travel = raycastY - wheel.collider.radius;
			} else {
				travel = wheel.collider.suspensionDistance;
			}

			float degPerSec;
			degPerSec = wheel.collider.rpm * 6; //   * 360 deg / 60 sec per minute = 6
			wheelRotation = Quaternion.AngleAxis(-degPerSec * Time.deltaTime, Vector3.left);
			wheel.mesh.transform.localRotation *= wheelRotation;

			if (wheel.steerWheel) {
				Vector3 steerVector = wheel.mesh.transform.localEulerAngles;
				steerVector.y = wheel.collider.steerAngle + steerVector.z; //corrects wierdness when wheel is upsidedown
				wheel.mesh.transform.localEulerAngles = steerVector;
			}

			Vector3 newPosition = wheel.localOrigin;
			newPosition.y -= travel;
			wheel.mesh.transform.localPosition = newPosition;
		}
	}
	
	void UpdateForwardSlipVelo () {
		WheelHit hit;
		bool grounded = false;
		foreach(Wheel wheel in wheels) {
			grounded = wheel.collider.GetGroundHit(out hit);
			if ( grounded ) {
				//slipVelo = Mathf.Sqrt(hit.forwardSlip * hit.forwardSlip + hit.sidewaysSlip * hit.sidewaysSlip);
				slipVeloForward = hit.forwardSlip;
			}
		}
		slipVeloForward /= wheels.Length;
	}

	void UpdateBumps () {
		//float bumpInterval = 0.1f;
		//float bumpPower = 500f;
		float bumpRadius = 0f;
		float upModifier = 0f;
		foreach(Wheel wheel in wheels) {
			if (!wheel.collider.isGrounded) continue;
			if (wheel.bumpPower == 0.0f) continue;
			if (Time.timeSinceLevelLoad > wheel.nextBump) {
				wheel.nextBump = Time.timeSinceLevelLoad + (wheel.bumpInterval * Random.value);
				Vector3 bumpPosition = wheel.collider.transform.position;
				float bumpPower = wheel.bumpPower * rigidbody.mass / wheels.Length * Random.value;
				bumpPosition.y -= wheel.collider.radius;
				rigidbody.AddExplosionForce(bumpPower, bumpPosition, bumpRadius, upModifier, ForceMode.Impulse);
			}
		}
	}

	void OnGUI(){
		string gearDisplay;
		if( reverse ) {
			gearDisplay = "R";
		} else if ( currentGear == 0 ) {
			gearDisplay = "N";
		} else {
			gearDisplay = currentGear.ToString();
		}
		GUI.Box (new Rect (0,0,200,50), "Speed KPH:" + speed * 3.6f + "\nGear:" + gearDisplay + "\nEngine RPM:" + engineRPM);
	}
}

[System.Serializable]
public class Wheel {
	public WheelCollider collider;
	public GameObject mesh;
	public bool steerWheel;
	public bool driveWheel;
	public bool eBrake;

	public TerrainEffects.Type currentTerrain;
	public float baseRollingResistance;
	public float terrainResistanceEffect;
	[HideInInspector]
	public float terrainResistanceInfluence;
	[HideInInspector]
	public float bumpInterval;
	[HideInInspector]
	public float bumpPower;
	[HideInInspector]
	public float nextBump = 0.0f;
	[HideInInspector]
	public Vector3 localOrigin;		
}
public class ControlProxy {
	const int neutral = -1;
	public float throttle;
	public float steer;
	public float brake;
	public int gear;
	public bool reverse;
	public bool eBrake;
}
