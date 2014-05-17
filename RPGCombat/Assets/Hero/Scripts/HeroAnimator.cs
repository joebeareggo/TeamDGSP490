using UnityEngine;
using System.Collections;

public class HeroAnimator : MonoBehaviour {

	Animator anim;

	// Hero variables
	private float moveSpeed = 1.0f;

	// Input variables
	bool run;	// Left-shift button
	bool isMovingForward;	// Character direction
	bool combatMode;
	bool isAttacking;

	// Hash variables
	int speedHash = Animator.StringToHash ("Speed");	// Movement speed (magnitude)
	int xVelocityHash = Animator.StringToHash ("VelocityX");	// X-direction velocity
	int zVelocityHash = Animator.StringToHash ("VelocityZ");	// Y-direction velocity
	int spaceHash = Animator.StringToHash ("Space");	// Space bar
	int runHash = Animator.StringToHash ("Run");	// Run (Activated by Left-Shift)
	int forwardHash = Animator.StringToHash ("MovingForward");	// Is the player moving forward
	int combatHash = Animator.StringToHash ("CombatMode");	// Player state
	int attackHash = Animator.StringToHash ("Attack");	// Attack

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();

		run = false;	// Player running
		isMovingForward = true;	// Player moving forward
		combatMode = false;	// Player in combat mode
		isAttacking = false;	// Player not attacking
	}
	
	// Update is called once per frame
	void Update () {

	}

	// Fixed Update for Physics
	void FixedUpdate()
	{
		isAttacking = false;	// Reset attack state

		float hmovement = Input.GetAxis ("Horizontal");	// A and D press (-1.0 - 1.0)
		float vmovement = Input.GetAxis ("Vertical");	// W and S press (-1.0 - 1.0)
		Vector2 move = new Vector2 (hmovement, vmovement);	// Vector2 movement value
		
		InputHandler ();	// Handle user-input

		// Set direction
		if(vmovement >= 0)
		{
			isMovingForward = true;
		}
		else if(vmovement < 0)
		{
			isMovingForward = false;
		}

		// Check if the player is running
		if(run && isMovingForward && move.magnitude > 0)
		{
			move *= 3;	// Increase movement speed
		}
		else
		{
			run = false;
		}

		anim.SetFloat (xVelocityHash, hmovement);	// X Velocity
		anim.SetFloat (zVelocityHash, vmovement);	// Z Velocity
		anim.SetFloat (speedHash, move.magnitude);	// Character speed
		anim.SetBool (runHash, run);	// Run
		anim.SetBool (forwardHash, isMovingForward);	// Is the character moving forward or backwards?
		anim.SetBool (combatHash, combatMode);	// Combat state
		anim.SetBool (attackHash, isAttacking);	// Player attack

		transform.Translate (new Vector3 (move.x, 0.0f, move.y) * Time.deltaTime * moveSpeed);
	}

	void InputHandler()
	{
		// Left-shift key press and release events
		if(Input.GetKey (KeyCode.LeftShift))
		{
			run = true;
		}
		else
		{
			run = false;
		}
		
		// Space bar press
		if(Input.GetKeyDown (KeyCode.Space))
		{
			anim.SetBool (spaceHash, true);
		}
		else
		{
			anim.SetBool (spaceHash, false);	// Avoid continuous rolling
		}

		// Toggle Combat Mode
		if(Input.GetKeyDown (KeyCode.C))
		{
			if(combatMode == false)
			{
				combatMode = true;
			}
			else
			{
				combatMode = false;
			}
		}

		// Left-click attack
		if(Input.GetMouseButtonDown (0))
		{
			isAttacking = true;
		}
	}
}
