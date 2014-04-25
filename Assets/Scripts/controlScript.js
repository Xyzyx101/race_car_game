#pragma strict

var moveSpeed : float = 600;
var turnSpeed : float = 2;

function Start () {

}

function Update () {
	
	var turnAmount : float = Input.GetAxis ("Horizontal") * turnSpeed;
	transform.Rotate(Vector3(0,turnAmount), Space.World);
	
	var moveAmount : float = Input.GetAxis("Vertical") * -moveSpeed;
	rigidbody.AddRelativeForce(moveAmount,0,0);
	
	
}