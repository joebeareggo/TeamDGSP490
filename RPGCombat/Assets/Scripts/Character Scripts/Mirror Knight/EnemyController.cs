using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {
	
	Animator anim;
	
	// Knight target
	GameObject target;
	KnightController targetController;
	KnightHealth targetHealth;
	
	// Player Stats object
	EnemyHealth enemyHealth;
	
	// Player variables
	float pMoveSpeed = 180000.0f;
	string attackType;	// Player attack type
	float sightRadius = 10.0f;	// Can see 10 units
	float sightFOV = 120.0f;	// Can see 120 degrees
	
	// State variables
	public enum EnemyState { Free, Attacking, RunningAttack, Blocking, Flinching, Dodging, Dead };	// Player states
	public EnemyState enemyState;
	
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
		
		enemyState = EnemyState.Blocking;	// Initiate player state to 'Free'
		
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
		
		// Enemy death
		if(enemyHealth.GetHealth () <= 0 && !isDead)
		{
			enemyState = EnemyState.Dead;	// Player is dead
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

		// Face target in certain states
		if(enemyState == EnemyState.Free || enemyState == EnemyState.Dodging || 
		   enemyState == EnemyState.RunningAttack || enemyState == EnemyState.Blocking)
		{
			transform.LookAt (target.transform.position);
		}
		
		// Make sure AI has been activated
		if(isActive)
		{
			// Handle player state
			switch(enemyState)
			{
			case EnemyState.Free:
				FreeLogic();
				break;
			case EnemyState.Blocking:
				BlockingLogic();
				break;
			case EnemyState.Attacking:
				AttackingLogic ();
				break;
			case EnemyState.RunningAttack:
				RunningAttackLogic();
				break;
			case EnemyState.Flinching:
				FlinchingLogic ();
				break;
			case EnemyState.Dodging:
				DodgingLogic ();
				break;
			case EnemyState.Dead:
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
					
					enemyHealth.SetStamina (enemyHealth.GetStamina () - (30.0f * Time.deltaTime));	// Reduce player stamina
					
					// Attack if within range
					if(dtop < 3.0f)
					{
						isAttacking = true;
						enemyState = EnemyState.Attacking;
					}
				}
				else
				{
					isSprinting = false;	// Player can't sprint with no stamina
				}
			}
			else if(vMovement < 0)
			{
				movingForward = false;	// Player is moving backward
				isSprinting = false;	// Player cannot sprint backward
			}
		}
		else // Player isn't moving
		{
			isIdle = true;	// Player isn't moving
			movingForward = true;	// Set forward as default position for idle
			isSprinting = false;	// Player cannot be sprinting
		}
		
		// Move player
		//transform.Translate (new Vector3(movement.x, 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
		//rigidbody.AddForce (new Vector3 (movement.x , 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
		rigidbody.AddRelativeForce (new Vector3(movement.x, 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
	}
	
	/*
	 * Blocking State
	 * 
	 * Enemy is blocking.
	 * Enemy
	 */
	void BlockingLogic()
	{
		isBlocking = true;	// blocking animation

		// Attack is being blocked
		if(blockedAttack)
		{
			BlockAttack ();

			aiTimer = 0.0f;
		}
		// Enable decision making
		else
		{
			// Increase AI decision timer
			aiTimer += Time.deltaTime;

			// Stamina remaining
			if(enemyHealth.GetStamina () > 0.0f)
			{
				// Target is free and not blocking
				if(targetController.playerState == KnightController.PlayerState.Free || 
				   targetController.isBlocking == false)
				{
					// Attacking range
					if(DistanceToTarget() < 3.0f)
					{
						// Make decisionevery .75 seconds
						if(aiTimer >= 0.75f)
						{
							int rand = Random.Range (0, 3);

							// 50% attack
							if(rand == 0)
							{
								isAttacking = true;
								isBlocking = false;
								enemyState = EnemyState.Attacking;

								aiTimer = 0.0f;	// Reset timer
							}
							// 50% blocking
							if(rand == 1 || rand == 2)
							{
								isBlocking = true;
								enemyState = EnemyState.Blocking;

								aiTimer = 0.0f;
							}
						}
					}
					// Out of attacking range
					else
					{
						// Enough stamina for a running attacking
						if(enemyHealth.GetStamina () > 40.0f)
						{
							// Running attack
							isSprinting = true;
							isBlocking = false;
							enemyState = EnemyState.RunningAttack;

							aiTimer = 0.0f;
						}
						else
						{
							// Rest to regain stamina
							isBlocking = false;
							enemyState = EnemyState.Free;

							aiTimer = 0.0f;
						}
					}
				}
				// Target is free and not blocking
				else if(targetController.playerState == KnightController.PlayerState.Free ||
				        targetController.isBlocking == true)
				{
					// Attacking range
					if(DistanceToTarget() < 3.0f)
					{
						// make decision every .75 seconds
						if(aiTimer >= 0.75f)
						{
							int rand = Random.Range (0, 4);

							// 50% stay in block
							if(rand == 0 || rand == 1)
							{
								isBlocking = true;
								enemyState = EnemyState.Blocking;

								aiTimer = 0.0f;
							}
							// 25% attack
							else if(rand == 2)
							{
								isBlocking = false;
								isAttacking = true;
								enemyState = EnemyState.Attacking;

								aiTimer = 0.0f;
							}
							// 25% dodge
							else if(rand == 3)
							{
								isBlocking = false;
								isDodging = true;
								enemyState = EnemyState.Dodging;
								aiTimer = 0.0f;
							}
						}
					}
					// Out of attacking range
					else
					{
						// Decision every .75 seconds
						if(aiTimer >= 0.75f)
						{
							// Enough stamina for a running attack
							if(enemyHealth.GetStamina () > 40.0f)
							{
								int rand = Random.Range (0, 2);

								// 50% rest in free
								if(rand == 0)
								{
									isBlocking = false;
									enemyState = EnemyState.Free;

									aiTimer = 0.0f;
								}
								// 50% running attack
								else if(rand == 1)
								{
									isBlocking = false;
									isSprinting = true;
									enemyState = EnemyState.RunningAttack;

									aiTimer = 0.0f;
								}
							}
							// Low stamina
							else
							{
								// Try to rest and regain stamina
								isBlocking = false;
								enemyState = EnemyState.Free;
							}
						}
					}
				}
				// Target is in the middle of an attacking
				else if(targetController.playerState == KnightController.PlayerState.Attacking)
				{
					// Attacking range
					if(DistanceToTarget() < 3.0f)
					{
						// Decision every .75 seconds
						if(aiTimer >= 0.75)
						{
							// Lots of stamina
							if(enemyHealth.GetStamina () > 40.0f)
							{
								int rand = Random.Range (0, 2);

								// 50% block
								if(rand == 0)
								{
									isBlocking = true;
									enemyState = EnemyState.Blocking;

									aiTimer = 0.0f;
								}
								// 50% dodge
								else if(rand == 1)
								{
									isBlocking = false;
									isDodging = true;
									enemyState = EnemyState.Dodging;

									aiTimer = 0.0f;
								}
							}
							// Lower stamina
							else
							{
								int rand = Random.Range (0, 3);

								// 33% block
								if(rand == 0)
								{
									isBlocking = true;
									enemyState = EnemyState.Blocking;

									aiTimer = 0.0f;
								}
								// 66% dodge
								else if(rand == 1)
								{
									isBlocking = false;
									isDodging = true;
									enemyState = EnemyState.Dodging;

									aiTimer = 0.0f;
								}
							}
						}
					}
					// Out of attacking range
					else
					{
						// Decision every .75 seconds
						if(aiTimer >= 0.75f)
						{
							isBlocking = false;
							enemyState = EnemyState.Free;

							aiTimer = 0.0f;
						}
					}
				}
				// Target is in the middle of a dodge
				else if(targetController.playerState == KnightController.PlayerState.Dodging)
				{
					// Attacking range
					if(DistanceToTarget() < 3.0f)
					{
						// Decision every .75 seconds
						if(aiTimer >= 0.75f)
						{
							int rand = Random.Range (0, 2);

							// 50% blocking
							if(rand == 0)
							{
								isBlocking = true;
								enemyState = EnemyState.Blocking;

								aiTimer = 0.0f;
							}
							// 50% attacking
							else if(rand == 1)
							{
								isBlocking = false;
								isAttacking = true;
								enemyState = EnemyState.Attacking;

								aiTimer = 0.0f;
							}
						}
					}
					// Out of attacking range
					else
					{
						// Decision every .75 seconds
						if(aiTimer >= 0.75f)
						{
							// Plenty of stamina
							if(enemyHealth.GetStamina () > 40.0f)
							{
								int rand = Random.Range(0, 2);

								// Run attack
								if(rand == 0)
								{
									isBlocking = false;
									isSprinting = true;
									enemyState = EnemyState.RunningAttack;

									aiTimer = 0.0f;
								}
								// Free
								if(rand == 1)
								{
									isBlocking = false;
									enemyState = EnemyState.Free;
								}
							}
							// Low stamina
							else
							{
								// Rest
								isBlocking = false;
								enemyState = EnemyState.Free;
							}
						}
					}
				}
			}
			// Out of stamina
			else
			{
				isBlocking = false;
				enemyState = EnemyState.Free;

				aiTimer = 0.0f;
			}
		}
	}

	void BlockAttack()
	{
		blockTimer += Time.deltaTime;

		// End blocking animation
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
			
			float dmg = 0.0f;
			if(attackType == "Basic")
			{
				dmg = 25.0f;
			}
			else if(attackType == "Heavy")
			{
				dmg = 40.0f;
			}
			
			targetController.TakeHit(new Vector2(transform.position.x, transform.position.z), 25.0f);	// Damage player
			
			
			// TODO: AI decide next state
			enemyState = EnemyState.Free;	// Set player state to 'Free'
		}
	}

	/*
	 * Running Attack State
	 * 
	 * Player is running towards the target to attack.
	 */
	void RunningAttackLogic()
	{
		// Run towards target and attack if stamina isn't gone
		if(enemyHealth.GetStamina () > 0.0f)
		{
			// Reduce stamina
			enemyHealth.SetStamina (enemyHealth.GetStamina () - (30.0f * Time.deltaTime));

			hMovement = 0.0f;
			vMovement = 1.0f;
			movement = new Vector2(hMovement, vMovement);
			movement *= 2.5f;	// Sprinting speed

			isSprinting = true;	// Sprint towards player
			isIdle = false;	// Not idle
			movingForward = true;	// Moving forward

			// Within attacking range
			if(DistanceToTarget() < 3.0f)
			{
				isAttacking = true;	// Set animation to attacking

				enemyState = EnemyState.Attacking; // Enter attack state
			}

			rigidbody.AddRelativeForce (new Vector3(movement.x, 0.0f, movement.y) * Time.deltaTime * pMoveSpeed);
		}
		else // Change state
		{
			isSprinting = false;	// Can't sprint with no stamina
			enemyState = EnemyState.Free;	// Change state to free
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
			// TODO: Change state
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
			enemyHealth.SetStamina (enemyHealth.GetStamina () - 20.0f);	// Reduce stamina
			isDodging = false;	// Set to false to avoid infinite dodge loop
		}
		
		// Check if dodging animation is done playing
		if(!isDodging && anim.GetCurrentAnimatorStateInfo (0).nameHash != dodgeStateHash)
		{
			// Within attacking range
			if(DistanceToTarget() < 3.0f)
			{
				int rand = Random.Range (0, 4);

				// Stamina left over
				if(enemyHealth.GetStamina () > 0.0f)
				{
					// 50% chance attack
					if(rand == 0 || rand == 1)
					{
						isAttacking = true;
						enemyState = EnemyState.Attacking;
					}
					// 25% block
					else if(rand == 2)
					{
						isBlocking = true;
						enemyState = EnemyState.Blocking;
					}
					// 25% free
					else if(rand == 3)
					{
						enemyState = EnemyState.Free;
					}
				}
				// Out of stamina
				else
				{
					enemyState = EnemyState.Free;
				}
			}
			else
			{
				int rand = Random.Range (0, 3);

				// Stamina remaining
				if(enemyHealth.GetStamina () > 0.0f)
				{
					// Running attack
					if(rand == 0 || rand == 1)
					{
						enemyState = EnemyState.RunningAttack;
					}
					// Free
					else if(rand == 2)
					{
						enemyState = EnemyState.Free;
					}
				}
				// Out of stamina
				else
				{
					enemyState = EnemyState.Free;
				}
			}
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
		if(enemyState != EnemyState.Free && enemyState != EnemyState.Flinching)
		{
			restTimer = 0.0f;
		}
		
		// Reset attack timer
		if(enemyState != EnemyState.Attacking)
		{
			attackTimer = 0.0f;
		}
		
		// Reset block timer
		if(enemyState != EnemyState.Free)
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
		return true;
	}
	
	// Can the player dodge?
	public bool CanDodge()
	{
		return true;
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

	}
	
	// Stamina regeneration logic
	public void StaminaRegeneration()
	{
		// Regeneration during 'Free' or 'Flinching' states
		if(enemyState == EnemyState.Free || enemyState == EnemyState.Flinching)
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

	// Distance to target object
	float DistanceToTarget()
	{
		return (transform.position - target.transform.position).magnitude;
	}
}