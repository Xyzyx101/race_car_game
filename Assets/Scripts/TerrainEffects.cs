using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainEffects : MonoBehaviour {

	public Texture2D terrainTex;
	public bool bump;
	public CarPhysics car;

	public enum Type {Asphalt, Dirt, Grass, Sand, Ice, Snow};
	public class Effect {
		public Type type;
		public float grip;
		public float bumpInterval;
		public float bumpPower;
		public float rollingCoefficient;
		public Vector3 color;
	}
	private Dictionary<Type, Effect> effectDict = new Dictionary<Type, Effect>();
	private float lastBump;

	// Use this for initialization
	void Start () {
		InitTerrain();
	}
	
	void Update () {
		foreach(Wheel wheel in car.wheels) {
			Vector3 castPoint = wheel.collider.transform.position;
			Vector3 dir = wheel.collider.transform.TransformDirection(-Vector3.up);
			float distance = wheel.collider.radius + wheel.collider.suspensionDistance;

			RaycastHit hit;
			if( Physics.Raycast(castPoint, dir, out hit, distance) ) {
				Debug.DrawRay(castPoint, dir, Color.black);
				Vector2 texUV = hit.textureCoord;
				int texCoordX = Mathf.CeilToInt(texUV.x * terrainTex.width);
				int texCoordY = Mathf.CeilToInt(texUV.y * terrainTex.height);
				
				Vector4 terrainMapColor = terrainTex.GetPixel(texCoordX, texCoordY);
				Vector3 colorVector = terrainMapColor;
				Effect effect = CalcClosestEffect(colorVector);

				if (wheel.currentTerrain == effect.type) continue;
				
				wheel.currentTerrain = effect.type;

				//this changes the rolling resistance of the car
				wheel.terrainRollingCoefficient = effect.rollingCoefficient;

				//this changes the grip of the tires
				WheelFrictionCurve tempCurve = wheel.collider.forwardFriction;
				tempCurve.stiffness = effect.grip;
				wheel.collider.forwardFriction = tempCurve;

				tempCurve = wheel.collider.sidewaysFriction;
				tempCurve.stiffness = effect.grip;
				wheel.collider.sidewaysFriction = tempCurve;

				//this makes the terrain bumpy
				wheel.bumpInterval = effect.bumpInterval;
				wheel.bumpPower = effect.bumpPower;
			}
		}
		
	}
	void FixedUpdate () {
	}
	void InitTerrain () {
		//Asphalt
		Effect effect =  new Effect();
		effect.type = Type.Asphalt;
		effect.grip = 1.0f;
		effect.bumpInterval = 0.0f;
		effect.bumpPower = 0.0f;
		effect.rollingCoefficient = 0.01f;
		effect.color = new Vector3(0.5f, 0.5f, 0.5f); //med grey
		effectDict.Add(Type.Asphalt, effect);

		//Dirt
		effect =  new Effect();
		effect.type = Type.Dirt;
		effect.grip = 0.7f;
		effect.bumpInterval = 0.7f;
		effect.bumpPower = 1.2f;
		effect.rollingCoefficient = 0.25f;
		effect.color = new Vector3(1.0f, 0.0f, 0.0f); //red
		effectDict.Add(Type.Dirt, effect);

		//grass
		effect = new Effect();
		effect.type = Type.Grass;
		effect.grip = 0.8f;
		effect.bumpInterval = 0.3f;
		effect.bumpPower = 0.6f;
		effect.rollingCoefficient = 0.08f;
		effect.color = new Vector3(0.0f,1.0f, 0.0f); // green
		effectDict.Add(Type.Grass, effect);

		//snow
		effect = new Effect();
		effect.type = Type.Snow;
		effect.grip = 0.6f;
		effect.bumpInterval = 0.3f;
		effect.bumpPower = 0.4f;
		effect.rollingCoefficient = 0.15f;
		effect.color = new Vector3(1.0f, 1.0f, 1.0f); // white
		effectDict.Add(Type.Snow, effect);

		//ice
		effect = new Effect();
		effect.type = Type.Ice;
		effect.grip = 0.2f;
		effect.bumpInterval = 0.0f;
		effect.bumpPower = 0.0f;
		effect.rollingCoefficient = 0.0f;
		effect.color = new Vector3(0f, 1.0f, 1.0f); //cyan
		effectDict.Add(Type.Ice, effect);

		//sand
		effect = new Effect();
		effect.type = Type.Sand;
		effect.grip = 0.7f;
		effect.bumpInterval = 0.4f;
		effect.bumpPower = 0.4f;
		effect.rollingCoefficient = 0.35f;
		effect.color = new Vector3(1.0f, 1.0f, 0f); //yellow
		effectDict.Add(Type.Sand, effect);

	}
	public Effect CalcClosestEffect(Vector3 mapColor) {
		Effect closestEffect = effectDict[Type.Asphalt];
		float closestDistanceSquared = Mathf.Infinity;
		foreach(KeyValuePair<Type, Effect> entry in effectDict) {
			float distanceSquared = (entry.Value.color - mapColor).sqrMagnitude;
			if (distanceSquared < closestDistanceSquared) {
				closestEffect = entry.Value;
				closestDistanceSquared = distanceSquared;
			}
		}
		return closestEffect;
	}
}


