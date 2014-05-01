using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour {
	public TrackPath path;
	public CarPhysics car;
	public float currentPathParam = 0f;
	public float lookAhead = 1.0f;
	public bool debugUseAI = true;
	public bool debugUseBreak = true;
	public float maxSidewaysSlip;

	private Vector2 car2DVelocity;

	void Start () {
		maxSidewaysSlip = 0f;
		foreach(Wheel wheel in car.wheels) {
			maxSidewaysSlip += wheel.collider.sidewaysFriction.asymptoteSlip;
		}
		maxSidewaysSlip /= car.wheels.Length;
	}
	
	void Update () {
		car2DVelocity = new Vector2(car.rigidbody.velocity.x, car.rigidbody.velocity.z);
		Vector2 car2D = new Vector2(car.transform.position.x, car.transform.position.z);
		Vector2 car2DForward = new Vector2(car.transform.forward.x, car.transform.forward.z).normalized;
		DebugDisplay3D(car2DForward, Color.yellow);

		Vector2 projection = car2DForward * Mathf.Max(5f, Vector2.Dot(car2DVelocity, car2DForward)) * lookAhead;
		
		DebugDisplay3D(projection, Color.blue);
		Vector2 targetPoint;
		currentPathParam = path.GetNearestParam(out targetPoint, car2D + projection, currentPathParam );
		if ( (targetPoint - car2D).sqrMagnitude < 100 ) {
			currentPathParam += 10f;
		}
		Vector3 start = new Vector3((car2D + projection).x, car.transform.position.y, (car2D + projection).y);
		Vector3 end = new Vector3(targetPoint.x, car.transform.position.y, targetPoint.y);
		Debug.DrawLine(start, end, Color.red);

		DriveActuator(targetPoint);
	}
	void DriveActuator (Vector2 target) {

		// min and max refers to the speed of the car not the size of the angle
		float minForwardAngle = 120f * Mathf.Deg2Rad;
		float maxForwardAngle = car.steerMax * Mathf.Deg2Rad / 2;

		float minBackAngle = 120f * Mathf.Deg2Rad;
		float maxBackAngle = 170f * Mathf.Deg2Rad;

		float speedFactor = Mathf.Clamp01(car.speed * 0.036f - 0.2f); // scales from 20 to 120 kph

		float backAngle = Mathf.Lerp(minBackAngle, maxBackAngle, speedFactor);
		Vector3 backArc = car.transform.TransformDirection(new Vector3(Mathf.Sin(backAngle), 0f, Mathf.Cos(backAngle)));
		Debug.DrawRay(car.transform.position, backArc, Color.cyan);

		float forwardAngle = Mathf.Lerp(minForwardAngle, maxForwardAngle, speedFactor);
		Vector3 forwardArc = car.transform.TransformDirection(new Vector3(Mathf.Sin(forwardAngle), 0f, Mathf.Cos(forwardAngle)));
		Debug.DrawRay(car.transform.position, forwardArc, Color.cyan);

		Vector2 forward2D = new Vector2(car.transform.forward.x, car.transform.forward.z).normalized;
		Vector2 carPos2D = new Vector2(car.transform.position.x, car.transform.position.z);

		Vector2 targetDir = (target - carPos2D).normalized;
		DebugDisplay3D(forward2D, Color.yellow);
		DebugDisplay3D(targetDir, Color.grey);
		//Debug.Log("forward:" + forward2D + "target:" + targetDir);
		//Debug.Log (Vector2.Dot(forward2D, targetDir));
		float targetAngle = Vector2.Angle(targetDir, forward2D) * Mathf.Deg2Rad;

		Vector3 localTarget = car.transform.InverseTransformDirection(new Vector3(targetDir.x, 0f, targetDir.y));

		ControlProxy control = new ControlProxy();
		control.steer = Mathf.Clamp(targetAngle * Mathf.Rad2Deg * Mathf.Sign(localTarget.x) / car.steerMax, -1f, 1f);

		if ( targetAngle < forwardAngle ) {
			Debug.Log("forward and turn");
			control.throttle = Mathf.Max(0.25f, Mathf.Abs(localTarget.z));

		} else if ( targetAngle > backAngle ) {
			Debug.Log("backwards");
			if ( debugUseBreak ) Debug.Break();
			
			if (car.speed < 5.0f) {
				control.steer *= -1;
				control.throttle = 1.0f;
				control.reverse = true;
			} else {
				control.throttle = 0.0f;
				control.brake = 1.0f;
			}

		} else {
			Debug.Log("brake and turn");
			control.throttle = 0f;
			control.brake = Mathf.Max(0f, localTarget.z);
			if ( control.brake < 0 ) {
				Debug.Log("max brake!!!!!!!!");
				//control.brake = 1.0f;
				//control.eBrake = true;
			};
			control.steer = Mathf.Clamp(Mathf.Abs(control.steer), 0, 1 - control.brake) * Mathf.Sign(localTarget.x);

			// Scale by the ratio of sideways slip to avoid spinouts
			control.brake *= Mathf.Clamp01(1 - car.slipVeloSideways / maxSidewaysSlip);
			//control.steer *= Mathf.Clamp01(1 - car.slipVeloSideways / maxSidewaysSlip);

		}
		if ( Mathf.Abs(car.slipVeloSideways) > 20) {
			if ( debugUseBreak ) Debug.Break();
		}

		//Debug.Log("brake:" + control.brake + "  steer:" + control.steer + "  ebrake:" + control.eBrake + "  slipF:" + car.slipVeloForward + "  slipS:" + car.slipVeloSideways);
		
		//FIXME
		control.gear = 4;

		//Debug.Log("steer:" + control.steer + "  throttle:" + control.throttle + "  brake:" + control.brake);

		if (!debugUseAI) return;
		car.SetControls(control);
	}
	void DebugDisplay3D(Vector2 vector, Color myColor) {
		Vector3 offsetVec = new Vector3(0f, car.transform.position.y, 0f);
		Debug.DrawRay( car.transform.position + offsetVec, new Vector3(vector.x, 0f, vector.y), myColor);
	}
}


