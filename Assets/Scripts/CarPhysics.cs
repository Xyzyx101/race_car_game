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
	public float redline = 8000;
	public float engineEfficiancy = 0.83f;
	
	public float engineRPM = 0f;
	public float speed = 0f;
	public float inertiaFactor = 1.5f;
	public float slipVeloForward = 0;
	public float slipVeloSideways = 0f;
	

	public float steer = 0;
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
		torqueCurve.AddKey(8000,0);

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
		UpdateSlipVelo();
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
		float delta = 1 - (engineRPM - idleRPM) / ( redline - idleRPM);
		float totalWheelTorque = torqueCurve.Evaluate(engineRPM) * gearRatios[currentGear] * diffRatio * throttle * engineEfficiancy * delta;

		float forcePerWheel = rigidbody.mass * -Physics.gravity.y / wheels.Length;
		float torquePerWheel = totalWheelTorque / driveWheelCount;
		foreach(Wheel wheel in wheels) {
			if( wheel.driveWheel ) {
				float rollingResCoef = wheel.baseRollingCoefficient + wheel.terrainRollingCoefficient * (1 - wheel.terrainBonus);
				float wheelResistance = Mathf.Clamp(rollingResCoef * forcePerWheel, 0f, torquePerWheel);
				wheel.collider.motorTorque = torquePerWheel - wheelResistance;
				if (reverse) wheel.collider.motorTorque *= -1;
			}
		}
	}
	void UpdateDrag (Vector3 relativeVelocity) {
		Vector3 relativeDrag = new Vector3( -relativeVelocity.x * Mathf.Abs(relativeVelocity.x),
		                               -relativeVelocity.y * Mathf.Abs(relativeVelocity.y),
		                               -relativeVelocity.z * Mathf.Abs(relativeVelocity.z));
		Vector3 drag = transform.TransformDirection(Vector3.Scale(relativeDrag, dragMultiplier));
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
	
	void UpdateSlipVelo () {
		WheelHit hit;
		bool grounded = false;
		slipVeloForward = 0f;
		slipVeloSideways = 0f;
		foreach(Wheel wheel in wheels) {
			grounded = wheel.collider.GetGroundHit(out hit);
			if ( grounded ) {
				slipVeloForward += hit.forwardSlip;
				slipVeloSideways += hit.sidewaysSlip;
			}
		}
		slipVeloForward /= wheels.Length;
		slipVeloSideways /= wheels.Length;
	}

	void UpdateBumps () {
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

	public float baseRollingCoefficient;
	/* terrainBonus is the percentage of terrain resistance that is ignored. 0 for slicks, 0.1 for road tires, 0.6 for knobby offroad tires for example*/
	public float terrainBonus;
	[HideInInspector]
	public float terrainRollingCoefficient;
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
