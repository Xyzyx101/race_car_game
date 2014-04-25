using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour {
	public TrackPath path;
	public CarPhysics car;
	public float currentPathParam = 0f;

	// Use this for initialization
	void Start () {
		//Testing 

	}
	
	// Update is called once per frame
	void Update () {
		Vector2 targetPoint;
		Vector2 car2D = new Vector2(car.transform.position.x, car.transform.position.z);
		currentPathParam = path.GetNearestParam(out targetPoint, car2D, currentPathParam );
		Vector3 start = car.transform.position;
		Vector3 end = new Vector3(targetPoint.x, 1.0f, targetPoint.y);
		Debug.DrawLine(start, end, Color.red);
	}
}
