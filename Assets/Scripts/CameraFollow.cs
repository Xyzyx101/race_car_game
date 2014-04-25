using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {

	public Transform target;
	public float damping;
	public LayerMask ignoreLayers = -1;

	private float range;
	private float height;
	private Vector3 targetVel = Vector3.zero;
	private Vector3 prevVel = Vector3.zero;
	private LayerMask raycastLayers = -1;
	
	void Start () {
		Vector3 cameraLocalPosition = Vector3.zero;
		cameraLocalPosition -= range * Vector3.forward;
		Vector3 cameraWorldPosition = target.TransformPoint(cameraLocalPosition);
		transform.position = cameraWorldPosition;
		raycastLayers = ~ignoreLayers;
	}

	void FixedUpdate () {
		targetVel = Vector3.Lerp(prevVel, target.rigidbody.velocity, damping * Time.fixedDeltaTime);
		targetVel.y = 0f;
		prevVel = targetVel;
	}
	void LateUpdate () {
		Vector3 targetWorldPoint = target.position + targetVel;
		transform.LookAt(targetWorldPoint);

		float velFactor = Mathf.Clamp01(targetVel.magnitude / 100f);
		this.camera.fieldOfView = Mathf.Lerp(45f, 72f, velFactor);
		height = Mathf.Lerp(2f, 3f, velFactor);
		range = Mathf.Lerp(4f, 12f, velFactor);

		Vector3 cameraWorldPosition;

		cameraWorldPosition = target.position - targetVel.normalized * range;
		cameraWorldPosition += height * Vector3.up;

		//quick fix to make sure the camera is not on the other side of a wall
		RaycastHit hit;
		Vector3 targetDirection = targetWorldPoint - cameraWorldPosition;
		if(Physics.Raycast(cameraWorldPosition, targetDirection, out hit, range * 0.5f, raycastLayers)) {
			//Debug.Log("raycast hit");
			cameraWorldPosition = hit.point;
		}
		transform.position = cameraWorldPosition;		
	}
}
