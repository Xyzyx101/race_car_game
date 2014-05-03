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

		Vector2 projection;
		if (Vector2.Dot(car2DVelocity, car2DForward) < 5.0f) {
			projection = car2DForward * 5.0f * lookAhead;
			DebugDisplay3D(projection, Color.blue);
		} else {
			projection = car2DVelocity * lookAhead;
			DebugDisplay3D(projection, Color.yellow);
		}

		AI.Goal goal = new AI.Goal();
		currentPathParam = path.GetNearestParam(ref goal, car2D + projection, currentPathParam );
		if ( (goal.position - car2D).sqrMagnitude < 100 ) {
			currentPathParam += 10f;
		}
		Vector3 start = new Vector3((car2D + projection).x, car.transform.position.y, (car2D + projection).y);
		Vector3 end = new Vector3(goal.position.x, car.transform.position.y, goal.position.y);
		Debug.DrawLine(start, end, Color.red);

		DriveActuator(goal);
	}
	void DriveActuator (AI.Goal goal) {

		// min and max refers to the speed of the car not the size of the angle
		float minForwardAngle = 120f * Mathf.Deg2Rad;
		float maxForwardAngle = car.steerMax * Mathf.Deg2Rad / 4;

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

		ControlProxy control = new ControlProxy();

		// New steer based on direction
		Vector2 targetDir = (goal.position - carPos2D).normalized;
		float goalAngle = Vector2.Angle(goal.direction, forward2D) * Mathf.Deg2Rad;

		if ( goalAngle < forwardAngle ) {
			// targetAngle will try to match position with the goal
			float targetAngle = Vector2.Angle(targetDir, forward2D) * Mathf.Deg2Rad;
			Vector3 localTarget = car.transform.InverseTransformDirection(new Vector3(targetDir.x, 0f, targetDir.y));
			control.steer = Mathf.Clamp(targetAngle * Mathf.Sign(localTarget.x) / (car.steerMax * Mathf.Deg2Rad), -1f, 1f); // note Mathf.Sign() is not Mathf.Sin()
			control.throttle = Mathf.Max(0.25f, Mathf.Abs(localTarget.z));
			if (car.speed < 0) control.steer *= -1;
		} else if ( goalAngle > backAngle ) {
			if ( debugUseBreak ) Debug.Break();
			float targetAngle = Vector2.Angle(targetDir, forward2D) * Mathf.Deg2Rad;
			Vector3 localTarget = car.transform.InverseTransformDirection(new Vector3(targetDir.x, 0f, targetDir.y));
			control.steer = Mathf.Clamp(targetAngle * Mathf.Sign(localTarget.x) / (car.steerMax * Mathf.Deg2Rad), -1f, 1f); // note Mathf.Sign() is not Mathf.Sin()
			if (car.speed < 5.0f) {
				control.steer *= -1;
				control.throttle = 1.0f;
				control.reverse = true;
			} else {
				control.throttle = 0.0f;
				control.brake = 1.0f;
			}
		} else {
			// steep turns will try to match direction with the goal not position
			Vector2 velo2D = new Vector2(car.rigidbody.velocity.x, car.rigidbody.velocity.z);
			float approachVelo = Vector2.Dot(velo2D, targetDir);
			float distToNextCorner = (goal.cornerPosition - carPos2D).magnitude;
			float turnScale = approachVelo / distToNextCorner;
			Vector3 localTarget = car.transform.InverseTransformDirection(new Vector3(goal.direction.x, 0f, goal.direction.y));
			control.steer = Mathf.Clamp(goalAngle * Mathf.Sign(localTarget.x) / (car.steerMax * Mathf.Deg2Rad) * turnScale, -1f, 1f);
			control.throttle = 1f;
		}
	
		control.gear = car.currentGear;
		if (car.engineRPM > car.autoShiftUp) {
			if (car.currentGear < car.gearRatios.Length - 1) {
				car.currentGear += 1;
			}
		} else if (car.engineRPM < car.autoShiftDown) {
			if (car.currentGear > 1) {
				car.currentGear -= 1;
			}
		}

		if (!debugUseAI) return;
		car.SetControls(control);
	}
	void DebugDisplay3D(Vector2 vector, Color myColor) {
		Vector3 offsetVec = new Vector3(0f, car.transform.position.y, 0f);
		Debug.DrawRay( car.transform.position + offsetVec, new Vector3(vector.x, 0f, vector.y), myColor);
	}
}

namespace AI {
	public struct Goal {
		public bool hasPosition;
		public Vector2 position;

		public bool hasDirection;
		public Vector2 direction;

		//public bool hasVelocity;
		//public float velocity;

		/*  Corner warning will give you the position and angle of upcoming corners.
		 *  The corner angle is a 0-1 float that scales between 0 deg and 90 deg.
		 *  cornerAngle is used to scale the target speed to the max speed you can 
		 *  safely make a corner.
		 */
		public bool hasCornerWarning;
		public Vector2 cornerPosition;
		public float cornerAngle;
	}
}
