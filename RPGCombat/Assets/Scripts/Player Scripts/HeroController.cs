using UnityEngine;
using System.Collections;

public class HeroController : MonoBehaviour {

	Animator anim;

	// Player Stats object
	HeroStats heroStats;

	// Player variables
	float pMoveSpeed = 2.0f;

	// State variables
	public enum PlayerState { Free, Attacking, Flinching, Dodging, Dead };	// Player states
	public PlayerState playerState;

	// Movement variables
	float hMovement;	// Horizontal movement
	float vMovement;	// Vertical movement
	Vector2 movement;	// Movement vector

	// Animator state hash variables
	int attackStateHash = Animator.StringToHash ("Base Layer.Attack");	// Attack state
	int dodgeStateHash = Animator.StringToHash ("Base Layer.Dodge");	// Dodge state
	int flinchStateHash = Animator.StringToHash ("Base Layer.Flinch");	// Flinch state
	int dyingStateHash = Animator.StringToHash ("Base Layer.Dying");	// Dying state

	// Animator conditional hash variables
	int xVelocityHash = Animator.StringToHash ("xVelocity");	// Horizontal velocity hash
	int zVelocityHash = Animator.StringToHash ("zVelocity");	// Vertical velocity hash
	
	int isIdleHash = Animator.StringToHash("isIdle");	// Idle hash
	int movingForwardHash = Animator.StringToHash ("movingForward");	// Movement direction hash
	int isAttackingHash = Animator.StringToHash ("isAttacking");	// Player attack hash
	int isDodgingHash = Animator.StringToHash ("isDodging");	// Player dodge hash
	int isSprintingHash = Animator.StringToHash ("isSprinting");	// Player sprint hash
	int isFlinchingHash = Animator.StringToHash ("isFlinching");	// Player flinch hash
	int isBlockingHash = Animator.StringToHash ("isBlocking");	// Player block hash
	int isDeadHash = Animator.StringToHash ("isDead");	// Player dead hash
	int isDyingHash = Animator.StringToHash ("isDying");	// Player dying hash

	// Conditional variables
	bool isIdle;		// Player isn't moving
	bool movingForward;	// Movement direction
	bool isAttacking;	// Player is attacking
	bool isDodging;		// Player is dodging
	bool isSprinting;	// Player is sprinting
	bool isFlinching;	// Player is flinching
	bool isBlocking;	// Player is blocking
	bool isDead;		// Player is dead
	bool isDying;		// Player is dying

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();	// Animator component
		heroStats = GetComponent<HeroStats> ();	// HeroStats component

		playerState = PlayerState.Free;	// Initiate player state to 'Free'

		// Initiate movement variables
		hMovement = 0.0f;
		vMovement = 0.0f;
		movement = Vector2.zero;

		isIdle = true;	// Initiate idle
		movingForward = true;	// Initiate forward
		isAttacking = false;	// Initiate player not attacking
		isDodging = false;	// Initiate player not dodging
		isBlocking = false;	// Initiate player not blocking
		isDead = false;	// Initiate player alive
	}
	
	// Update is called once per frame
	void Update () {

		ResetVariables ();	// Reset necessary variables

		UserInput ();	// Get user input

		// Player death
		if(heroStats.GetHealth () <= 0 && !isDead)
		{
			playerState = PlayerState.Dead;	// Player is dead
			isDying = true;
			isDead = true;

			isFlinching = true;	// Avoid flinch loop
		}

		// Handle player state
		switch(playerState)
		{
		case PlayerState.Free:
			FreeLogic();
			break;
		case PlayerState.Attacking:
			AttackingLogic ();
			break;
		case PlayerState.Flinching:
			FlinchingLogic ();
			break;
		case PlayerState.Dodging:
			DodgingLogic ();
			break;
		case PlayerState.Dead:
			DeathLogic();
			break;
		}

		SetAnimatorConditionals ();	// Set the conditional variables for the animator
	}

	/*
	 * Free State
	 * 
	 * Player can move freely.
	 * Player is not attacking, flinching, or dodging.
	 * Player can attack, flinch, or move.
	 */
	void FreeLogic()
	{
		// Initiate movement variables
		hMovement = Input.GetAxis ("Horizontal");
		vMovement = Input.GetAxis ("Vertical");
		movement = new Vector2 (hMovement, vMovement);

		// Player is moving
		if(movement.magnitude > 0)
		{
			isIdle = false;

			// Check movement direction
			if(vMovement >= 0)
			{
				movingForward = true;

				// Check for sprint
				if(isSprinting)
				{
					movement *= 3.0f;
				}
			}
			else if(vMovement < 0)
			{
				movingForward = false;	// Player is moving backward
				isSprinting = false;	// Player cannot sprint backward
			}
		}
		else
		{
			isIdle = true;	// Player isn't moving
			movingForward = true;	// Set forward as default position for idle
			isSprinting = false;	// Player cannot be sprinting
		}

		// Move player
		transform.Translate (new Vector3(movement.x, 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
	}

	/*
	 * Attacking State
	 * 
	 * Player is in the middle of an attack.
	 * Player cannot move or dodge.
	 * Player can flinch.
	 */
	void AttackingLogic()
	{
		// Ensure the animation has started
		if (isAttacking && anim.GetCurrentAnimatorStateInfo (0).nameHash == attackStateHash)
		{
			isAttacking = false;	// Set to false to avoid infinite attack loop
		}

		// Check if an attack animation is done playing
		if(!isAttacking && anim.GetCurrentAnimatorStateInfo(0).nameHash != attackStateHash)
		{
			playerState = PlayerState.Free;	// Set player state to 'Free'
		}
	}

	/*
	 * Flinching State
	 * 
	 * Player is recovering from an attack.
	 * Player cannot move, dodge, or attack.
	 */
	void FlinchingLogic()
	{
		// TODO: Adjust logic for multiple types of recoveries (i.e. flinch, knock-back, knock-down)

		// Ensure the animation has started'
		if(isFlinching && anim.GetCurrentAnimatorStateInfo (0).nameHash == flinchStateHash)
		{
			isFlinching = false;	// Set to false to avoid infinite flinch loop
		}

		// Check if flinch animation is done playing
		if(!isFlinching && anim.GetCurrentAnimatorStateInfo(0).nameHash != flinchStateHash)
		{
			playerState = PlayerState.Free;	// Set player state to 'Free'
		}
	}

	/*
	 * Dodging State
	 * 
	 * Player is dodging an attack.
	 * Player cannot move, flinch, or attack.
	 * Player is invincible.
	 */
	void DodgingLogic()
	{
		// TODO: Handle dodging physics, set player invincible.

		// Ensure the animation has started
		if(isDodging && anim.GetCurrentAnimatorStateInfo (0).nameHash == dodgeStateHash)
		{
			isDodging = false;	// Set to false to avoid infinite dodge loop
		}

		// Check if dodging animation is done playing
		if(!isDodging && anim.GetCurrentAnimatorStateInfo (0).nameHash != dodgeStateHash)
		{
			playerState = PlayerState.Free;	// Set player state to 'Free'
		}
	}

	/*
	 * Death State
	 * 
	 * Player cannot move, flinch, or attack.
	 */
	void DeathLogic()
	{
		if(isDying && anim.GetCurrentAnimatorStateInfo (0).nameHash == dyingStateHash)
		{
			isDying = false;	// Avoid death loop
		}

		isFlinching = false;	// Avoid flinching loop
	}

	/*
	 * Reset Variables
	 * 
	 * This function resets all of the necessary variables.
	 */
	void ResetVariables()
	{
		// Movement variables
		hMovement = 0.0f;
		vMovement = 0.0f;
		movement = Vector2.zero;
	}

	/*
	 * User Input
	 * 
	 * This function handles keyboard and mouse events.
	 */
	void UserInput()
	{
		// Attack on Left-Click
		if(Input.GetMouseButtonDown (0))
		{
			// Check if our player is an attack-capable state
			if(CanAttack())
			{
				playerState = PlayerState.Attacking;	// Set player state to 'Attacking'
				isAttacking = true;	// Triggers the animation
			}
		}

		// Block on left-mouse hold
		if(Input.GetMouseButton (1))
		{
			isBlocking = true;
		}
		else
		{
			isBlocking = false;
		}

		// Sprint on Left Shift
		// Sprinting not allowed while blocking
		if(Input.GetKey (KeyCode.LeftShift) && !isBlocking)
		{
			isSprinting = true;
		}
		else
		{
			isSprinting = false;
		}

		// Dodge on space bar
		if(Input.GetKeyDown (KeyCode.Space))
		{
			if(CanDodge())
			{
				playerState = PlayerState.Attacking;
				isDodging = true;
			}
		}

		// Testing conditions
		// Player flinch
		if(Input.GetKeyDown (KeyCode.F))
		{
			TakeHit (Vector2.zero, 30.0f);	// Function for player being attacked
		}
	}

	/*
	 * Set Animator Conditionals
	 * 
	 * This function uses hash values to set all of the conditional 
	 * variables in the animator.
	 */
	void SetAnimatorConditionals()
	{
		anim.SetFloat (xVelocityHash, hMovement);
		anim.SetFloat (zVelocityHash, vMovement);

		anim.SetBool (isIdleHash, isIdle);
		anim.SetBool (movingForwardHash, movingForward);
		anim.SetBool (isAttackingHash, isAttacking);
		anim.SetBool (isDodgingHash, isDodging);
		anim.SetBool (isSprintingHash, isSprinting);
		anim.SetBool (isFlinchingHash, isFlinching);
		anim.SetBool (isBlockingHash, isBlocking);
		anim.SetBool (isDeadHash, isDead);
		anim.SetBool (isDyingHash, isDying);
	}

	// Can the player attack?
	public bool CanAttack()
	{
		// Check for attack-capable states
		if(playerState == PlayerState.Free && !isBlocking)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// Can the player dodge?
	public bool CanDodge()
	{
		// Check for dodge-capable states
		if(playerState == PlayerState.Free)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// Can the player block the attack?
	public void CanBlock(Vector2 source)
	{
		// TODO: Compare source to the player's rotation
	}

	// Player is attacked
	public void TakeHit(Vector2 source, float damage)
	{
		// Player is free and not blocking
		if(playerState == PlayerState.Free && !isBlocking)
		{
			// Player is injured from the attack
			// TODO: Adjust player health and set state to flinching

			heroStats.SetHealth (heroStats.GetHealth () - damage);	// Set health to current health minus the attack's damage
			playerState = PlayerState.Flinching;
			isFlinching = true;
		}
		// Player is attacking
		else if(playerState == PlayerState.Attacking)
		{
			// Player is injured from the attack
			heroStats.SetHealth (heroStats.GetHealth () - damage);	// Take damage
			playerState = PlayerState.Flinching;	// Start flinching logic
			isFlinching = true;		// Enable flinching
			isAttacking = false;	// Set attackign to false
		}
		// Player is free and blocking
		else if(playerState == PlayerState.Free && isBlocking)
		{
			// Check if player can block attack
			// i.e. is the player facing the attacking opponent
		}
		// Player is flinching
		else if(playerState == PlayerState.Flinching)
		{
			// Player can't be injured
		}
		// Player is dodging
		else if(playerState == PlayerState.Dodging)
		{
			// Player can't be injured
		}
	}
}
