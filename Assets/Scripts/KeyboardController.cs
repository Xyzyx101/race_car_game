﻿using UnityEngine;
using System.Collections;

public class KeyboardController : MonoBehaviour {

	public CarPhysics car;
	//time to change gears
	public float shiftTime = 0.8f;

	//time to reach full or 0 throttle
	public float throttleTime = 1.0f;
	public float throttleReleaseTime = 0.5f;
	public float throttleReleaseTimeBraking = 0.1f;

	//time to reach full or 0 throttle when the wheels are slipping
	public float throttleTimeSlip = 5.0f;
	public float throttleReleaseTimeSlip = 0.1f;

	//time to go from center to full stear and from full to center
	public float steerTime = 1.2f;
	public float steerReleaseTime = 0.6f;

	//stearing gets slower as the car moves faster	
	public float steerVeloFactor = 0.1f;
	public float steerReleaseVeloFactor = 0.0f;

	//this is used to help correct fish tailing due to key controls
	public float steerCorrectionFactor = 0.01f;

	public float brakeTime = 0.2f;
	public float brakeReleaseTime = 0.1f;

	private bool automaticTrans;
	private bool autoShiftUp;
	private bool autoShiftDown;

	private float steerInput;
	private bool forwardInput;
	private bool backInput;

	public bool canDrive = true;

	// Update is called once per frame
	void Update () {
		ControlProxy control  = new ControlProxy();
		control.eBrake = Input.GetKey (KeyCode.Space);

		SetSteer(ref control);
		
		forwardInput = Input.GetKey(KeyCode.UpArrow);
		backInput = Input.GetKey(KeyCode.DownArrow);

		/*this will temporarily disable the controls so that you must release forward and back 
		 * to put the car in gear.  Stops accidently putting the car in reverse while braking*/
		if (!canDrive) {
			if (!forwardInput && !backInput) {
				canDrive = true;
			} else {
				forwardInput = false;
				backInput = false;
			}
		}

		control.reverse = car.reverse;
		if (car.reverse) {
			bool x = forwardInput;
			forwardInput = backInput;
			backInput = x;
		}

		SetGear(ref control);
		SetThrottle(ref control);
		SetBrakes(ref control);

		car.SetControls(control);
	}
	void SetSteer (ref ControlProxy control) {
		steerInput = 0;
		if (Input.GetKey (KeyCode.LeftArrow)) {
			steerInput = -1;
		}
		if (Input.GetKey (KeyCode.RightArrow)) {
			steerInput = 1;
		}

		// Steering
		Vector3 carDir = transform.forward;
		float fVelo = rigidbody.velocity.magnitude;
		Vector3 veloDir = rigidbody.velocity.normalized;
		float angle = -Mathf.Asin(Mathf.Clamp( Vector3.Cross(veloDir, carDir).y, -1, 1));
		float optimalSteering = angle / (car.steerMax * Mathf.Deg2Rad);
		if (fVelo < 1) optimalSteering = 0;

		float steer = steerInput;
		float adjustedSteerTime = 0;
		if ( steerInput == 0 ) {
			adjustedSteerTime = 1 / (steerReleaseTime + (Mathf.Abs(car.speed) * steerReleaseVeloFactor));
			if ( car.steer < 0 ) {
				steer = car.steer + adjustedSteerTime * Time.deltaTime;
				if ( steer > 0 ) steer = 0;
			} else {
				steer = car.steer - adjustedSteerTime * Time.deltaTime;
				if ( steer < 0 ) steer = 0;
			}
		} else {
			if ( steerInput > car.steer ) {
				if ( car.steer < 0 ) {
					adjustedSteerTime = 1 / (steerReleaseTime + (car.speed * steerReleaseVeloFactor));
				} else {
					adjustedSteerTime = 1 / (steerTime + (car.speed * steerVeloFactor));
				}
				if ( car.steer < optimalSteering ) {
					adjustedSteerTime *= 1 + (optimalSteering - car.steer) * steerCorrectionFactor;
				}
			} else if ( steerInput < car.steer ) {
				if ( car.steer > 0 ) {
					adjustedSteerTime = 1 / (steerReleaseTime + (car.speed * steerReleaseVeloFactor));
				} else {
					adjustedSteerTime = 1 / (steerTime + (car.speed * steerVeloFactor));
				}
				if ( car.steer > optimalSteering ) {
					adjustedSteerTime *= 1 + (car.steer - optimalSteering) * steerCorrectionFactor;
				}
			}
			steer = Mathf.Clamp(car.steer + steerInput * adjustedSteerTime * Time.deltaTime, -1.0f, 1.0f);
		}
		control.steer = steer;
	}
	void SetGear (ref ControlProxy control) {
		int newGear = car.currentGear;
		if( car.currentGear == 0 ) {
			control.reverse = false;
			if( canDrive ) {
				if( forwardInput ) {
					newGear = 1;
				} else if( backInput ) {
					newGear = 1;
					control.reverse = true;
				}
			}
		} else if ( car.automaticTrans ) {
			if (car.engineRPM > car.autoShiftUp) {
				if (car.currentGear < car.gearRatios.Length - 1) {
					newGear = car.currentGear + 1;
				}
			} else if (car.engineRPM < car.autoShiftDown) {
				if (car.currentGear > 1) {
					newGear = car.currentGear - 1;
				}
			}
			if ( Mathf.Abs(car.speed) < 2.0f && !forwardInput && backInput ) {
				newGear = 0;
				canDrive = false;
			}
		} else {
			//TODO manual transmission
		}
		control.gear = newGear;
	}
	void SetThrottle(ref ControlProxy control) {
		float finalThrottleTime;
		float finalThrottleReleaseTime;
		if ( car.slipVeloForward < -1.8f) {
			finalThrottleTime = throttleTimeSlip;
			finalThrottleReleaseTime = throttleTimeSlip;
		} else {
			finalThrottleTime = throttleTime;
			finalThrottleReleaseTime = throttleReleaseTime;
		}

		if( forwardInput ) {
			control.throttle = Mathf.Clamp01(car.throttle + finalThrottleTime * Time.deltaTime);
		} else if (backInput) {
			control.throttle = Mathf.Clamp01(car.throttle - throttleReleaseTimeBraking * Time.deltaTime);
		} else {
			control.throttle = Mathf.Clamp01(car.throttle - finalThrottleReleaseTime * Time.deltaTime);
		}
	}
	void SetBrakes(ref ControlProxy control) {
		if ( backInput ) {
			control.brake = Mathf.Clamp01(car.brake + brakeTime * Time.deltaTime);
		} else {
			control.brake = Mathf.Clamp01(car.brake - brakeReleaseTime * Time.deltaTime);
		}
	}
}
