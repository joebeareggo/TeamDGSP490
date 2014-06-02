﻿using UnityEngine;
using System.Collections;

public class OldController : MonoBehaviour {
	
	Animator anim;

	// Knight target
	GameObject target;
	KnightController targetController;
	KnightHealth targetHealth;
	
	// Player Stats object
	EnemyHealth enemyHealth;
	
	// Player variables
	float pMoveSpeed = 180000.0f;
	float pDamage = 25.0f;
	string attackType;	// Player attack type
	float sightRadius = 10.0f;	// Can see 10 units
	float sightFOV = 120.0f;	// Can see 120 degrees
	
	// State variables
	public enum PlayerState { Free, Attacking, Flinching, Dodging, Dead };	// Player states
	public PlayerState playerState;
	
	// Movement variables
	float hMovement;	// Horizontal movement
	float vMovement;	// Vertical movement
	Vector2 movement;	// Movement vector
	Vector2 dodgeDirection;	// direction for dodging
	
	// Animator state hash variables
	int attackStateHash = Animator.StringToHash ("Base Layer.Attack");	// Attack state
	int heavyAttackStateHash = Animator.StringToHash ("Base Layer.Heavy Attack");	// Heavy attack state
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
	int blockedAttackHash = Animator.StringToHash ("blockedAttack");	// Player blocked attack hash
	
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
	bool blockedAttack;	// Player blocked attack

	bool isActive;		// Player is active
	
	// Timers
	float restTimer;	// Time in free state
	float dodgeTimer;	// Time for dodge physics
	float attackTimer;	// Time for attack to hit
	float blockTimer;	// Time to stall for blocked attack

	// AI Timers
	float aiTimer;	// Time to transition states and make new decision

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();	// Animator component
		enemyHealth = GetComponent<EnemyHealth> ();	// EnemyHealth component

		// Target variables
		target = GameObject.FindGameObjectWithTag ("Knight");
		targetController = target.GetComponent<KnightController> ();
		targetHealth = target.GetComponent<KnightHealth> ();
		
		playerState = PlayerState.Free;	// Initiate player state to 'Free'
		
		attackType = "Basic";	// Initiate attack type to basic
		
		// Initiate movement variables
		hMovement = 0.0f;
		vMovement = 0.0f;
		movement = Vector2.zero;
		dodgeDirection = Vector2.zero;
		
		isIdle = true;	// Initiate idle
		movingForward = true;	// Initiate forward
		isAttacking = false;	// Initiate player not attacking
		isDodging = false;	// Initiate player not dodging
		isBlocking = false;	// Initiate player not blocking
		isDead = false;	// Initiate player alive
		isDying = false;	// Initiate player alive
		blockedAttack = false;	// Initiate player not blocking attack
		
		restTimer = 0.0f;	// Initiate rest timer to zero
		dodgeTimer = 0.0f;	// Initiate dodge timer to zero
		attackTimer = 0.0f;	// Initiate attack timer to zero
		blockTimer = 0.0f;	// Initiate block timer to zero

		aiTimer = 0.0f;	// Initiate ai timer to zero

		isActive = false;	// Initiate inactive
	}
	
	// Update is called once per frame
	void Update () {
		
		ResetVariables ();	// Reset necessary variables
		
		//CombatAI ();	// Get user input
		
		// Player death
		if(enemyHealth.GetHealth () <= 0 && !isDead)
		{
			playerState = PlayerState.Dead;	// Player is dead
			isDying = true;
			isDead = true;
			
			isFlinching = true;	// Avoid flinch loop
		}

		// Activate AI if player has been spotted
		// ** Take Hit will also set isActive to true **
		if(!isActive && CanSeeTarget ())
		{
			isActive = true;
		}

		// Make sure AI has been activated
		if(isActive)
		{
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
		}
		
		StaminaRegeneration ();
		
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
		// Get distance to the target
		float dtop = (transform.position - target.transform.position).magnitude;

		// Face the target
		transform.LookAt (target.transform.position);

		// If the AI is in the idle state & not blocking, check distance, 
		// and if distance is greater than 7, 
		if(isIdle && !isBlocking)
		{
			// If enemy can attack
			if(enemyHealth.GetStamina() > 0.0f)
			{
				// Distance greater than 7
				// Sprint or walk towards target
				if(dtop > 7.0f)
				{
					hMovement = 0.0f;
					vMovement = 1.0f;
					movement = new Vector2(hMovement, vMovement);

					int rand = Random.Range (0, 5);

					// 40% chance to sprint towards player and attack
					if(rand < 2)
					{
						isSprinting = true;
					}
				}
			}
			else
			{
				// If the player is too close to the target, flee
				if(dtop < 5.0f)
				{
					hMovement = 0.0f;
					vMovement = -1.0f;
					movement = new Vector2(hMovement, vMovement);
				}
			}
		}
		else
		{
			// Stop blocking if player retreats
			if(isBlocking && dtop > 6.0f)
			{
				isBlocking = false;
			}

			// If the target gets too far, sprint after him
			if(dtop > 10.0f)
			{
				isSprinting = true;
			}

			// Move towards player if distance is too great
			if(dtop > 2.0f)
			{
				hMovement = 0.0f;
				vMovement = 1.0f;
				movement = new Vector2(hMovement, vMovement);
			}

			// Close combat logic
			// TODO: decision making
			// If target is attacking, make decision
			// If target is not currently a threat, wait an interval before making a decision

			if(dtop < 4.0f)
			{

			}
		}

		// If already sprinting, keep moving towards the player
		if(isSprinting)
		{
			hMovement = 0.0f;
			vMovement = 1.0f;
			movement = new Vector2(hMovement, vMovement);
		}

		// Player is moving
		if(movement.magnitude > 0)
		{
			isIdle = false;
			
			// Check movement direction
			if(vMovement >= 0)
			{
				movingForward = true;
				
				// Check for sprint
				// Player must have required stamina
				if(isSprinting && enemyHealth.GetStamina () > 0.0f)
				{
					restTimer = 0.0f;	// Reset rest timer
					movement *= 2.5f;	// Increase movement speed
					
					enemyHealth.SetStamina (enemyHealth.GetStamina () - 0.25f);	// Reduce player stamina

					// Attack if within range
					if(dtop < 3.0f)
					{
						isAttacking = true;
						playerState = PlayerState.Attacking;
					}
				}
				else
				{
					isSprinting = false;	// Player can't sprint backwards
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
		
		// Disable movement while blocking
		if(isBlocking || blockedAttack)
		{
			movement *= 0.0f;
		}
		
		BlockLogic ();	// Handle blocking logic
		
		// Move player
		//transform.Translate (new Vector3(movement.x, 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
		//rigidbody.AddForce (new Vector3 (movement.x , 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
		rigidbody.AddRelativeForce (new Vector3(movement.x, 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
	}
	
	/*
	 * Block Logic
	 * 
	 * Player is blocking an attack.
	 * Must be in free state.
	 */
	void BlockLogic()
	{
		// Check if the player is blocking an attack
		if(blockedAttack)
		{
			blockTimer += Time.deltaTime;	// Increase block timer by update interval
		}
		
		if(blockTimer >= 0.3f)
		{
			blockedAttack = false;
			blockTimer = 0.0f;
		}
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
		// Set spriting to false
		isSprinting = false;

		// Set attack type
		if(anim.GetCurrentAnimatorStateInfo(0).nameHash == attackStateHash)
		{
			attackType = "Basic";
		}
		else if(anim.GetCurrentAnimatorStateInfo(0).nameHash == heavyAttackStateHash)
		{
			attackType = "Heavy";
		}
		
		// Ensure the animation has started
		if (isAttacking && (anim.GetCurrentAnimatorStateInfo (0).nameHash == attackStateHash
		                    || anim.GetCurrentAnimatorStateInfo (0).nameHash == heavyAttackStateHash))
		{
			isAttacking = false;	// Set to false to avoid infinite attack loop
		}
		
		// Check if an attack animation is done playing
		//if(!isAttacking && anim.GetCurrentAnimatorStateInfo(0).nameHash != attackStateHash 
		//   && anim.GetCurrentAnimatorStateInfo(0).nameHash != heavyAttackStateHash)
		// Attacking trigger timer
		if(attackTimer <= 0.60f && attackType == "Basic")
		{
			attackTimer += Time.deltaTime;	// Increase attack timer
		}
		else if(attackTimer <= 0.85f && attackType == "Heavy")
		{
			attackTimer += Time.deltaTime;	// Increase attack timer by update value
		}
		else
		{
			// Damage enemies
			// TODO: Change to array type, compare distance, compare direction / range
			GameObject knight = GameObject.FindGameObjectWithTag ("Knight");
			KnightController knightController = knight.GetComponent<KnightController>();
			
			float dmg = 0.0f;
			if(attackType == "Basic")
			{
				dmg = 25.0f;
			}
			else if(attackType == "Heavy")
			{
				dmg = 40.0f;
			}
			
			knightController.TakeHit(new Vector2(transform.position.x, transform.position.z), 25.0f);


			// TODO: AI decide next state
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
		// TODO: Add timer
		if(!isFlinching && anim.GetCurrentAnimatorStateInfo(0).nameHash != flinchStateHash)
		{
			// TODO: AI decide next state
			if(enemyHealth.GetStamina () > 0)
			{
				int rand = Random.Range (0, 5);

				// 40% block
				if(rand < 2)
				{
					isBlocking = true;
					playerState = PlayerState.Free;
				}
				// 40% dodge
				else if(rand > 2)
				{
					isDodging = true;
					playerState = PlayerState.Dodging;

					// Dodge right
					if(rand == 3)
					{
						dodgeDirection = new Vector2(1.0f, 0.0f);
					}
					// Dodge left
					else if(rand == 4)
					{
						dodgeDirection = new Vector2(-1.0f, 0.0f);
					}
				}
				else if(rand == 3)
				{
					playerState = PlayerState.Free;
				}
			}
			else
			{
				playerState = PlayerState.Free;
			}
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
		// Look at the target
		transform.LookAt (target.transform.position);

		dodgeTimer += Time.deltaTime;	// Increase dodge timer by update speed
		
		// Enable dodge timer so that the player doesn't slide after dodge
		if(dodgeTimer <= 0.5f)
		{
			hMovement = dodgeDirection.x;
			vMovement = dodgeDirection.y;
			movement = dodgeDirection;
			
			rigidbody.AddRelativeForce (new Vector3 (dodgeDirection.x, 0.0f, dodgeDirection.y) * 4.0f * Time.deltaTime * pMoveSpeed);
		}
		
		// Ensure the animation has started
		if(isDodging && anim.GetCurrentAnimatorStateInfo (0).nameHash == dodgeStateHash)
		{
			isDodging = false;	// Set to false to avoid infinite dodge loop
		}
		
		// Check if dodging animation is done playing
		if(!isDodging && anim.GetCurrentAnimatorStateInfo (0).nameHash != dodgeStateHash)
		{
			float dtop = (transform.position - target.transform.position).magnitude;

			// TODO: AI decide next state
			if(dtop < 3.0f)
			{
				int rand = Random.Range (0, 2);

				if(rand == 0)
				{
					isAttacking = true;
					playerState = PlayerState.Attacking;
				}
				else if(rand == 1)
				{
					// Block if player has stamina
					if(enemyHealth.GetStamina () > 0)
					{
						isBlocking = true;
					}

					playerState = PlayerState.Free;
				}
			}
			// Too far for combat
			else
			{
				int rand = Random.Range (0, 2);

				// Sprint and attack
				if(rand == 0)
				{
					isSprinting = true;
				}

				playerState = PlayerState.Free;
			}
			
			dodgeTimer = 0.0f;	// Reset dodge timer
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
		
		// Reset rest timer if not in 'Free' or 'Flinching' states
		if(playerState != PlayerState.Free && playerState != PlayerState.Flinching)
		{
			restTimer = 0.0f;
		}
		
		// Reset attack timer
		if(playerState != PlayerState.Attacking)
		{
			attackTimer = 0.0f;
		}
		
		// Reset block timer
		if(playerState != PlayerState.Free)
		{
			blockTimer = 0.0f;
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
		anim.SetBool (blockedAttackHash, blockedAttack);
	}
	
	// Can the player attack?
	public bool CanAttack()
	{
		// Check for attack-capable states
		// Player has stamina
		// Player isn't blocking an attack
		if(playerState == PlayerState.Free && enemyHealth.GetStamina() > 0
		   && blockedAttack == false)
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
		// Player isn't moving backwards
		// Player is moving
		// Player has stamina
		// Player isn't blocking an attack
		if(playerState == PlayerState.Free && vMovement >= 0.0f && 
		   movement.magnitude > 0.0f && enemyHealth.GetStamina() > 0.0f
		   && blockedAttack == false)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	// Can the player block the attack?
	public bool CanBlock(Vector2 source)
	{
		// TODO: Compare source to the player's rotation
		
		return true;
	}
	
	// Player is attacked
	public void TakeHit(Vector2 source, float damage)
	{
		// If the AI is not yet active, active it.
		if(!isActive)
		{
			isActive = true;
		}

		// Player is free and not blocking
		if(playerState == PlayerState.Free && !isBlocking)
		{
			enemyHealth.SetHealth (enemyHealth.GetHealth () - damage);	// Set health to current health minus the attack's damage
			playerState = PlayerState.Flinching;
			isFlinching = true;
		}
		// Player is attacking
		else if(playerState == PlayerState.Attacking)
		{
			// Player is injured from the attack
			enemyHealth.SetHealth (enemyHealth.GetHealth () - damage);	// Take damage
			playerState = PlayerState.Flinching;	// Start flinching logic
			isFlinching = true;		// Enable flinching
			isAttacking = false;	// Set attackign to false
		}
		// Player is free and blocking
		else if(playerState == PlayerState.Free && isBlocking)
		{
			// Check if player can block attack
			// i.e. is the player facing the attacking opponent
			if(CanBlock (source))
			{
				enemyHealth.SetStamina (enemyHealth.GetStamina () - 20.0f);	// Reduce stamina

				// Set blocking to false if no stamina
				if(enemyHealth.GetStamina() <= 0)
				{
					isBlocking = false;
				}
				else
				{
					blockedAttack = true;	// Enable blocked attack animation
				}
			}
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
	
	// Stamina regeneration logic
	public void StaminaRegeneration()
	{
		// Regeneration during 'Free' or 'Flinching' states
		if(playerState == PlayerState.Free || playerState == PlayerState.Flinching)
		{
			restTimer += Time.deltaTime;	// Increase free state duration timer by delta time
			
			if(restTimer >= 3.0f)
			{
				enemyHealth.SetStamina (enemyHealth.GetStamina () + 30.0f * Time.deltaTime);
			}
		}
	}

	/*
	 * Can See
	 * 
	 * This function performs raycasting to see if the target is in sight.
	 */
	bool CanSeeTarget()
	{
		Vector3 direction = target.transform.position - transform.position;	// Find the direction to the target
		float angle = Vector3.Angle (direction, transform.forward);	// Get the angle of the direction

		// Check to see if the target is in the field of view
		if(angle < sightFOV * 0.5f)
		{
			RaycastHit hit;	// Raycast for intersection

			// Check if the raycast hits the player
			if(Physics.Raycast(transform.position + transform.up, direction.normalized, out hit, sightRadius))
			{
				// Raycast hit the target
				if(hit.collider.gameObject == target)
				{
					Debug.Log ("Target spotted!");

					return true;
				}
			}
		}

		return false;
	}

	/*
	 * Combat AI
	 * 
	 * This function controls the combat AI.
	 */
	void CombatAI()
	{
		// AI isn't yet active and the player can be seen
		if(!isActive && CanSeeTarget ())
		{
			isActive = true;	// Enable AI, movement, combat...
		}

		if(isActive)
		{
			float dtop = (transform.position - target.transform.position).magnitude;	// Distance to player
			
			// Initiate movement to zero
			hMovement = 0.0f;
			vMovement = 0.0f;

			// Face the target
			// Player must be in free state
			if(playerState == PlayerState.Free)
			{
				transform.LookAt (target.transform.position);
			}
			
			// Move towards the player if distance is too great
			if(dtop > 2.0f)
			{
				// Already facing the player, so move forward
				vMovement = 1.0f;
				hMovement = 0.0f;
			}
			
			movement = new Vector2 (hMovement, vMovement);	// Movement vector
			
			/*
			 * Free State AI
			 * 
			 * Sprint to player if he is too far.
			 * 
			 */
			if(playerState == PlayerState.Free && !isBlocking)
			{
				if(!isSprinting && dtop > 7.0f)
				{
					isSprinting = true;
				}
				else if(isSprinting && dtop <= 3.0f)
				{
					if(CanAttack ())
					{
						playerState = PlayerState.Attacking;
						isAttacking = true;
						
						enemyHealth.SetStamina (enemyHealth.GetStamina () - 15.0f);
					}
				}
				else if(!isSprinting && dtop <= 7.0f && dtop >= 3.0f)
				{
					isSprinting = false;
				}
				else if(!isSprinting && dtop < 3.0f)
				{
					isBlocking = true;
				}
			}
			else if(playerState == PlayerState.Free && isBlocking)
			{
				// Use delayed timer to make decisions
				aiTimer += Time.deltaTime;
				
				int ran = 0;
				// Target is in free state and not blocking
				if(targetController.playerState == KnightController.PlayerState.Free && !targetController.isBlocking)
				{
					ran = Random.Range (0, 10);

					if(ran > 7)
					{
						isAttacking = true;
					}
				}
			}
			else
			{
				isSprinting = false;
			}


		}	// End if isActive
		
		/*
		// Attack on Left-Click
		if(Input.GetMouseButtonDown (0))
		{
			// Check if our player is an attack-capable state
			if(CanAttack())
			{
				playerState = PlayerState.Attacking;	// Set player state to 'Attacking'
				isAttacking = true;	// Triggers the animation
				
				enemyHealth.SetStamina (enemyHealth.GetStamina () - 15.0f);	// Decrease stamina
			}
		}
		
		// Block on left-mouse hold
		// Stamina required for block
		// Player state free
		if(Input.GetMouseButton (1) && enemyHealth.GetStamina () > 0.0f
		   && playerState == PlayerState.Free)
		{
			isBlocking = true;
			
			// Reset rest timer
			restTimer = 0.0f;
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
				playerState = PlayerState.Dodging;
				isDodging = true;
				
				dodgeDirection = movement.normalized;	// Set dodge direction to the movement direction
				
				// Reduce stamina
				enemyHealth.SetStamina (enemyHealth.GetStamina() - 20.0f);
			}
		}
		
		// Testing conditions
		// Player flinch
		if(Input.GetKeyDown (KeyCode.F))
		{
			TakeHit (Vector2.zero, 30.0f);	// Function for player being attacked
		}
		*/
	}
}