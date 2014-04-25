using UnityEngine;
using System.Collections;

public class wheelColliderControl : MonoBehaviour {

	public Wheel[] wheels;

	public float steerMax = 28;
	//public float motorMax = 10;
	public float brakeTorqueMax = 100;

	public Vector3 dragMultiplier = new Vector3(0.02f, 0.01f, 0.01f);
	public float rollingResistance = 0.03f;

	public Transform centerOfMass;

	public AnimationCurve torqueCurve;
	public float[] gearRatios;
	public float diffRatio;
	public int currentGear = 2;
	public float engineRPM = 0;
	public float speedKPH = 0;
	public float inertiaFactor = 1.5f;

	private float steer = 0;
	private float forward = 0;
	private float back = 0;
	private float throttle = 0;
	private float brake = 0;
	private bool reverse = false;
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
		speedKPH = rigidbody.velocity.magnitude * 3.6f;
		UpdateWheelPosition();
	}
	void SetControls () {
		Debug.Log("Set Controls");
	}
	void FixedUpdate () {
		steer = Mathf.Clamp(Input.GetAxis("Horizontal"), -1, 1);
		forward = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1);
		back = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0);

		float speed = rigidbody.velocity.sqrMagnitude;

		if( speed < 0.1f ) {
		 	if (back < 0 ) {
				reverse = true;
			}
			if (forward > 0 ) {
				reverse = false;
			}
		}

		if ( reverse ) {
			throttle = 0.7f * back;
			brake = forward;
		} else {
			throttle = forward;
			brake = -back;
		}

		UpdateEngineTorque();
		UpdateGear();

		foreach(Wheel wheel in wheels) {
			wheel.collider.brakeTorque = brakeTorqueMax * brake;
			if( wheel.steerWheel ) {
				wheel.collider.steerAngle = steerMax * steer;
			}
		}


		Vector3 relativeVelocity = transform.InverseTransformDirection(rigidbody.velocity);
		UpdateResistance(relativeVelocity);

	}
	void UpdateEngineTorque()
	{

		/*engineRPM = Mathf.Clamp((colliderBR.rpm + colliderBL.rpm) / 2 * gearRatios[currentGear] * diffRatio, 700, 7000);
		float wheelTorque = torqueCurve.Evaluate(engineRPM) * gearRatios[currentGear] * diffRatio * throttle;
		colliderBR.motorTorque = wheelTorque / 2;
		colliderBL.motorTorque = wheelTorque / 2;*/

		float wheelRPM = 0.0f;
		foreach(Wheel wheel in wheels) {
			if( wheel.driveWheel ) {
				wheelRPM += wheel.collider.rpm;
			}
		}
		wheelRPM /= driveWheelCount;
		engineRPM = Mathf.Clamp(wheelRPM * gearRatios[currentGear] * diffRatio, 700, 7000);
		
		float totalWheelTorque = torqueCurve.Evaluate(engineRPM) * gearRatios[currentGear] * diffRatio * throttle;
		foreach(Wheel wheel in wheels) {
			if( wheel.driveWheel ) {
				wheel.collider.motorTorque = totalWheelTorque / driveWheelCount;
			}
		}

	}
	void UpdateGear()
	{
		float maxRPM = 4200;
		float minRPM = 2800;
		if (engineRPM > maxRPM) {
			if (currentGear < 6) {
				++currentGear;
			}
		} else if (engineRPM < minRPM) {
			if (currentGear > 1) {
				--currentGear;
			}
		}
	}

	void UpdateResistance (Vector3 relativeVelocity) {
		Vector3 relativeDrag = new Vector3( -relativeVelocity.x * Mathf.Abs(relativeVelocity.x),
		                               -relativeVelocity.y * Mathf.Abs(relativeVelocity.y),
		                               -relativeVelocity.z * Mathf.Abs(relativeVelocity.z));
		Vector3 dragFactor = Vector3.Scale(relativeDrag, dragMultiplier);
		Vector3 rollingFactor = new Vector3(0, 0, -relativeVelocity.z * rollingResistance);
		rigidbody.AddForce(transform.TransformDirection(dragFactor + rollingFactor) * rigidbody.mass * Time.deltaTime);
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
			degPerSec = wheel.collider.rpm * 6;
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

	void OnGUI(){
		GUI.Box (new Rect (0,0,200,50), "Speed KPH:" + speedKPH + "\nGear:" + currentGear + "\nEngine RPM:" + engineRPM);
	}
}

/*[System.Serializable]
public class Wheel {
	public WheelCollider collider;
	public GameObject mesh;
	public bool steerWheel;
	public bool driveWheel;
	[HideInInspector]
	public Vector3 localOrigin;	
}*/
