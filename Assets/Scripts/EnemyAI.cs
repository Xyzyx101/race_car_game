using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour {
	public TrackPath path;
	public CarPhysics car;
	public float currentPathParam = 0f;
	public float lookAhead = 1.0f;
	public bool debugUseAI = true;

	private Vector2 car2DVelocity;

	// Use this for initialization
	void Start () {
		//Testing 

	}
	
	// Update is called once per frame
	void Update () {
		car2DVelocity = new Vector2(car.rigidbody.velocity.x, car.rigidbody.velocity.z);
		Vector2 car2D = new Vector2(car.transform.position.x, car.transform.position.z);
		Vector3 carDebugDraw = new Vector3(car2D.x + car2DVelocity.x, car.transform.position.y, car2D.y + car2DVelocity.y);
		Debug.DrawLine(car.transform.position, carDebugDraw, Color.blue);
		Vector2 projection = car2D + car2DVelocity * lookAhead;
		Vector2 targetPoint;
		currentPathParam = path.GetNearestParam(out targetPoint, projection, currentPathParam );
		Vector3 start = car.transform.position;
		Vector3 end = new Vector3(targetPoint.x, car.transform.position.y, targetPoint.y);
		Debug.DrawLine(start, end, Color.red);

		DriveActuator(targetPoint);
	}
	void DriveActuator (Vector2 target) {
		//if (Mathf.Abs(car.slipVeloForward) > 1f) Debug.Break();

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
		Vector3 localTargetNorm = localTarget.normalized;

		//Debug.Log("localTarget:" + localTarget + "  localTargetNorm" + localTargetNorm);
		// FIXME
		if (localTarget - localTargetNorm != Vector3.zero) Debug.Break();

		ControlProxy control = new ControlProxy();
		control.steer = Mathf.Clamp(targetAngle * Mathf.Rad2Deg * Mathf.Sign(localTarget.x) / car.steerMax, -1f, 1f);

		if ( targetAngle < forwardAngle ) {
			Debug.Log("forward and turn");

			control.throttle = Mathf.Clamp01(Mathf.Cos(targetAngle));
			control.throttle = localTarget.z;
			
		} else if ( targetAngle > backAngle ) {
			Debug.Log("backwards");
			if (car.speed < 5.0f) {
				control.throttle = 1.0f;
				control.reverse = true;
			} else {
				control.throttle = 0.0f;
				control.brake = 1.0f;
			}

		} else {
			Debug.Log("brake and turn");
			control.throttle = 0f;

			//float requiredBrake = 1 - Mathf.Cos(targetAngle);
			float requiredBrake = localTarget.z;
			float maxBrake = Mathf.Lerp(1.0f, 0.2f, speedFactor);
			if ( requiredBrake < 0 ) control.eBrake = true;
			control.brake = Mathf.Clamp(requiredBrake, 0, maxBrake);

			control.steer = Mathf.Clamp(Mathf.Abs(control.steer), 0, 1 - control.brake) * Mathf.Sign(localTarget.x);

			Debug.Log(Mathf.Cos (targetAngle));

		}

		//FIXME
		control.gear = 3;

		Debug.Log("steer:" + control.steer + "  throttle:" + control.throttle + "  brake:" + control.brake);

		if (!debugUseAI) return;
		car.SetControls(control);
	}
	void DebugDisplay3D(Vector2 vector, Color myColor) {
		Vector3 offsetVec = new Vector3(0f, 1f, 0f);
		Debug.DrawRay( car.transform.position + offsetVec, new Vector3(vector.x, 0f, vector.y) + offsetVec, myColor);
	}
}

/* for reference only
 * public class ControlProxy {
	const int neutral = -1;
	public float throttle;
	public float steer;
	public float brake;
	public int gear;
	public bool reverse;
	public bool eBrake;
}*/
