﻿using UnityEngine;
using System.Collections;

public class TrackPath : MonoBehaviour {
	public Transform[] waypoints3D;
	public Vector2[] segments2D;
	public float[] segmentLengths;
	public float totalLength;

	// Use this for initialization
	void Start () {
		segments2D = new Vector2[waypoints3D.Length];
		for(int i = 0; i < waypoints3D.Length; i++) {
			segments2D[i] = new Vector2(waypoints3D[i].position.x, waypoints3D[i].position.z);
		}
		segmentLengths = new float[waypoints3D.Length];
		for(int i = 0; i < segments2D.Length - 1; i++) {
			segmentLengths[i] = (segments2D[i+1] - segments2D[i]).magnitude;
			totalLength +=segmentLengths[i];
		}
		segmentLengths[segments2D.Length - 1] = (segments2D[0]-segments2D[segments2D.Length - 1]).magnitude;
		totalLength += segmentLengths[segments2D.Length - 1];
	}
	
	// Update is called once per frame
	void Update () {
		//Debug Draw Path
		for(int i = 0; i < segments2D.Length - 1; i++) {
			Vector3 start = new Vector3(segments2D[i].x, 1.0f, segments2D[i].y);
			Vector3 end = new Vector3(segments2D[i+1].x, 1.0f, segments2D[i+1].y);
			//Debug.Log("start:" + start + "  end:" + end);
			Debug.DrawLine(start, end, Color.magenta);
		}
		Vector3 start2 = new Vector3(segments2D[segments2D.Length-1].x, 1.0f, segments2D[segments2D.Length-1].y);
		Vector3 end2 = new Vector3(segments2D[0].x, 1.0f, segments2D[0].y);
		Debug.DrawLine(start2, end2, Color.magenta);
	}
	
	/* In this context a path param is a 1D measurement along the path.
	 * This calculates the nearest param on the path to position.  currentParam is used
	 * so that the search will start at a specific spot on the path and continue 
	 * consecutivly rather than search the whole path every time. */
	public float GetNearestParam(out Vector2 targetPoint, Vector2 position, float currentParam ) {
		float segmentTotal = 0f;
		int currentSegment = 0;
		if (currentParam < segmentLengths[0]) {
			currentSegment = 0;
		} else if (currentParam > totalLength) {
			Debug.LogError("Path parameter exceedes path length");
		} else {
			for(int i = 0; i < segmentLengths.Length; i++) {
				if (segmentTotal + segmentLengths[i] > currentParam) {
					currentSegment = i;
					break;
				}
				segmentTotal += segmentLengths[i];
			}
		}
		float shortestDistance2 = Mathf.Infinity; //shortest distance squared
		int targetSegment = 0;
		targetPoint = segments2D[currentSegment];
		float newDistance2; // new distanceSquared
		do {
			int nextSegment = currentSegment == segments2D.Length - 1 ? 0 : currentSegment + 1; //if current segment is the last segment then next is 0
			//Debug.Log(currentSegment + "  " + nextSegment);
			Vector2 nearestPoint = NearestPointOnLine(position, segments2D[currentSegment], segments2D[nextSegment]);
			newDistance2 = (nearestPoint - position).sqrMagnitude;
			if ( newDistance2 < shortestDistance2 ) {
				shortestDistance2 = newDistance2;
				targetPoint = nearestPoint;
				targetSegment = currentSegment;
			}
			currentSegment = currentSegment == segments2D.Length - 1 ? 0 : currentSegment + 1;// increment unless it is the last segment then go to 0
		} while (newDistance2 <= shortestDistance2);
		float newParam = 0;
		for(int i = 0; i < targetSegment; i++) {
			newParam += segmentLengths[i];
		}
		newParam += (targetPoint - segments2D[targetSegment]).magnitude;
		return newParam;
	}

	/* This will return the the nearest point on Vector(start, end) to the point p
	 */
	public Vector2 NearestPointOnLine(Vector2 p, Vector2 start, Vector2 end) {
		float l2 = Vector2.SqrMagnitude(end - start); // |end-start|^2
		if (l2 == 0.0f) return start; // edge case where start == end;
		Vector2 lineVector = end - start;
		Vector2 pointVector = p - start;
		float t = Vector2.Dot(pointVector, lineVector) / l2;
		if ( t < 0.0f ) {
			return start;
		} else if ( t > 1.0f ) {
			return end;
		} else {
			return start + t * lineVector;
		}
	}

}