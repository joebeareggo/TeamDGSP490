﻿using UnityEngine;
using System.Collections;

public class MKController : EnemyController {
	
	// State variables
	// TODO: Put these in the EnemyController base file
	public enum EnemyState { Free, Blocking, Attacking, Flinching, Dodging, Dead };	// Enemy state enumeration
	public EnemyState enemyState;	// Enemy state variable
	
	Animator anim;	// Animator component
	
	EnemyHealth enemyHealth;	// Health and stamina script

	// Target variables
	GameObject target;					// Target game object
	KnightController targetController;	// Target's controller class
	
	// Movement variables
	float moveSpeed;		// Movement speed
	float hMovement;		// Horizontal movement
	float vMovement;		// Vertical movement
	Vector2 movement;		// Movement vector
	Vector2 dodgeDirection;	// Dodge vector
	Vector3 destination;	// Character destination
	
	// Attack variables
	AttackType attackType;						// Class attack type variable
	float attackDamageBasic;					// Basic attack damage
	float attackDamageHeavy;					// Heavy attack damage
	float attackRange;							// Attack range
	float attackAngle;							// Angle of basic attack
	
	// Block variables
	float blockAngle;	// Angle for blocking
	
	// Conditional variables
	bool isIdle;			// Character is idle
	bool movingForward;		// Character is moving forward
	bool isBlocking;		// Character is blocking
	bool isAttacking;		// Character is attacking
	bool isDodging;			// Character is dodging
	bool isFlinching;		// Character is flinching
	bool isDying;			// Character is dying
	bool isDead;			// Character is dead
	bool isSprinting;		// Character is sprinting
	bool blockedAttack;		// Character blocked an attack
	bool continueAttack;	// Character is combo attacking
	
	// Animator parameters
	int xVelocityHash = Animator.StringToHash ("xVelocity");
	int zVelocityHash = Animator.StringToHash ("zVelocity");
	int isIdleHash = Animator.StringToHash ("isIdle");
	int movingForwardHash = Animator.StringToHash ("movingForward");
	int isBlockingHash = Animator.StringToHash ("isBlocking");
	int isAttackingHash = Animator.StringToHash ("isAttacking");
	int isDodgingHash = Animator.StringToHash ("isDodging");
	int isFlinchingHash = Animator.StringToHash ("isFlinching");
	int isDyingHash = Animator.StringToHash ("isDying");
	int isDeadHash = Animator.StringToHash ("isDead");
	int isSprintingHash = Animator.StringToHash ("isSprinting");
	int blockedAttackHash = Animator.StringToHash ("blockedAttack");
	int continueAttackHash = Animator.StringToHash ("continueAttack");
	
	// Animator states
	int dyingStateHash = Animator.StringToHash ("Base Layer.Dying");
	
	// Timers
	float dodgeTimer;	// Dodge timer for applied force
	float attackTimer;	// Attack timer for exiting state and halting movement
	float blockTimer;	// Block timer for blocked attack recovery
	float flinchTimer;	// Flinch timer for exiting flinch state
	
	// Action times
	float timeDodge;			// Time it takes to dodge
	float timeFlinch;			// Time it takes to flinch
	float timeBasicAttack;		// Time it takes for basic attack
	float timeHeavyAttack;		// Time it takes for heavy attack
	float timeBlockedAttack;	// Time it takes to block an attack
	float timeInvincible;		// Time the character is invincible while dodging
	
	// State-specific variables
	bool attackRegistered;	// Used in the attack state logic to enable a delay timer after attack has registered
	bool attackAfterDodge;	// Used in the dodge state to transition smoothly from dodge to attack

	// Sight variables
	float sightRadius;		// How far can the character see?
	
	// Use this for initialization
	void Start () {
		
		anim = GetComponent<Animator> ();	// Get Animator component
		
		enemyHealth = GetComponent<EnemyHealth> ();	// Get EnemyHealth script component

		target = GameObject.FindGameObjectWithTag ("Knight");		// Find 'Knight' game object
		targetController = target.GetComponent<KnightController>();	// Get the target's controller component
		
		enemyState = EnemyState.Free;	// Initiate player state to free
		
		// Movement variables
		moveSpeed = 120000.0f;				// Character movement speed
		hMovement = 0.0f;					// Horizontal movement
		vMovement = 0.0f;					// Vertical movement
		movement = Vector2.zero;			// Movement vector
		dodgeDirection = Vector2.zero;		// Dodge vector
		destination = transform.position;	// Character destination
		
		// Attacking variables
		attackType = AttackType.Basic;	// Initate attack type to  basic
		attackDamageBasic = 25.0f;		// Set basic damage
		attackDamageHeavy = 40.0f;		// Set heavy damage
		attackRange = 6.0f;				// Attack range set to 3
		attackAngle = 60.0f;			// 60 degree attack angle
		
		// Blocking variables
		blockAngle = 90.0f;				// 90 degree blocking angle
		
		// Conditional variables
		isIdle = true;					// Character is idle
		movingForward = true;			// Character moving forward
		isBlocking = false;				// Character not blocking
		isAttacking = false;			// Character not attacking
		isDodging = false;				// Character not dodging
		isFlinching = false;			// Character not flinching
		isDying = false;				// Character not dying
		isDead = false;					// Character not dead
		isSprinting = false;			// Character not sprinting
		blockedAttack = false;			// Character not recovering from block
		continueAttack = false;			// Character not combo attacking
		
		// Timers
		dodgeTimer = 0.0f;
		attackTimer = 0.0f;
		blockTimer = 0.0f;
		flinchTimer = 0.0f;
		
		// Action times
		timeDodge = 0.4f;
		timeFlinch = 0.8f;
		timeBasicAttack = 0.5f;
		timeHeavyAttack = 0.85f;
		timeBlockedAttack = 0.3f;
		timeInvincible = 0.3f;
		
		// State-specific variables
		attackRegistered = false;
		attackAfterDodge = false;

		// Sight variables
		sightRadius = 20.0f;
	}
	
	// Update is called once per frame
	void Update () {
		
		AILogic ();	// Handle AI decision making
		
		// State handler
		switch(enemyState)
		{
		case  EnemyState.Free:
			FreeLogic();
			break;
		case EnemyState.Blocking:
			BlockingLogic();
			break;
		case EnemyState.Attacking:
			AttackingLogic();
			break;
		case EnemyState.Dodging:
			DodgingLogic();
			break;
		case EnemyState.Flinching:
			FlinchingLogic();
			break;
		case EnemyState.Dead:
			DeadLogic();
			break;
		}
		
		SetAnimatorParameters ();	// Set parameters for the animator component
	}
	
	/* * * * * * * * * * * * * * * * * Logic Handling * * * * * * * * * * * * * * * * */
	
	// Free state logic
	void FreeLogic()
	{
		// Character is moving
		if(movement.magnitude > 0)
		{
			isIdle = false;	// Character no longer idle
			
			// Check movement direction
			if(vMovement >= 0.0f)
			{
				movingForward = true;	// Not moving backwards
				
				// Is the player sprinting?
				if(isSprinting)
				{
					movement *= 2.0f;	// Increase movement speed
				}
				// Isn't sprinting
				else
				{
					isSprinting = false;
				}
			}
			else if(vMovement < 0.0f)
			{
				movingForward = false;	// Moving backwards
				isSprinting = false;	// Can't sprint backwards
			}
		}
		// Player isn't moving
		else
		{
			isIdle = true;
			movingForward = true;	// Default for idle state
			isSprinting = false;
		}
		
		// Move the character
		rigidbody.AddRelativeForce (new Vector3 (movement.x, 0.0f, movement.y) * Time.deltaTime * moveSpeed);
	}
	
	// Blocking state logic
	void BlockingLogic()
	{
		// TODO: If hit with heavy attack by opponent, apply backwards force to the player during block duration
		
		// Check if the player is blocking an attack
		if(blockedAttack)
		{
			blockTimer += Time.deltaTime;	// Increase block timer by update time
		}
		
		if(blockTimer >= timeBlockedAttack)
		{
			blockedAttack = false;
			blockTimer = 0.0f;
		}
		
		// Change state if the player isn't blocking
		if(!isBlocking && !blockedAttack)
		{
			ChangeToState(EnemyState.Free);
		}
	}
	
	// Attacking state logic
	void AttackingLogic()
	{
		// Execute on state execution
		if(attackTimer == 0.0f)
		{
			continueAttack = false;	// Combo initially disabled
			
			anim.SetBool (continueAttackHash, continueAttack);	// Disable combo animation
		}
		
		// TODO: Enable character turning for the first half of the attack
		
		attackTimer += Time.deltaTime;
		
		// If the attack has not yet registered with opponents
		if(!attackRegistered)
		{
			float currentDamage = 0.0f;

			// Basic attack time
			if(attackType == AttackType.Basic && attackTimer < timeBasicAttack)
			{
				currentDamage = attackDamageBasic;
			}
			// Heavy attack time
			else if(attackType == AttackType.Heavy && attackTimer < timeHeavyAttack)
			{
				currentDamage = attackDamageHeavy;
			}
			// Register attack
			else
			{	
				attackRegistered = true;	// Attack was registered 

				GameObject[] knights = GameObject.FindGameObjectsWithTag("Knight");
				foreach(GameObject knight in knights)
				{
					Vector3 direction = knight.transform.position - transform.position;	// Get the direction to the enemy
					float angle = Vector3.Angle(direction, transform.forward);	// Get the angle to the enemy
					
					// If the enemy is within the attack FOV and within range of attack
					if(angle < attackAngle * 0.5 && (knight.transform.position - transform.position).magnitude <= attackRange)
					{
						RaycastHit hit;
						
						// Check if the 
						if(Physics.Raycast (transform.position + transform.up, direction.normalized, out hit, attackRange))
						{
							// Target isn't being obstructed by an object
							if(hit.collider.gameObject == knight)
							{
								Debug.Log ("Knight hit!");
								
								knight.GetComponent<KnightController>().TakeHit (transform.position, currentDamage, attackType);
							}
						}
					}
				}
			}
		}
		// .3 second delay before automatic change to free state
		else if(attackType == AttackType.Basic && attackTimer >= timeBasicAttack + 0.3f)
		{
			// Check if combo is active
			if(continueAttack)
			{
				ChangeToState (EnemyState.Attacking);
				
				anim.SetBool (continueAttackHash, continueAttack);	// Change to next attack
			}
			else
				ChangeToState (EnemyState.Free);	// Refer back to free state
		}
		// .5 second delay before atomatic change to free state
		else if(attackType == AttackType.Heavy && attackTimer >= timeHeavyAttack + 0.3f)
		{
			ChangeToState (EnemyState.Free);
		}
	}
	
	// Dodging state logic
	void DodgingLogic()
	{
		// Reduce stamina upon entry
		if(dodgeTimer == 0.0f)
		{
			attackAfterDodge = false;	// Dodge to attack initially disabled
		}
		
		dodgeTimer += Time.deltaTime;	// Increase dodge timer
		
		// Set directional movement for dodge animation
		hMovement = dodgeDirection.x;
		vMovement = dodgeDirection.y;
		
		// Enable physics for dodge and avoid sliding
		if(dodgeTimer <= timeDodge)
		{
			// Add force relative to the character's rotation
			rigidbody.AddRelativeForce (new Vector3(dodgeDirection.x, 0.0f, dodgeDirection.y) 
			                            * 4.0f * Time.deltaTime * moveSpeed);
		}
		// Transition to attack if player attacked during dodge
		else if(dodgeTimer > timeDodge + 0.15f && attackAfterDodge == true)
		{
			ChangeToState (EnemyState.Attacking);
		}
		// Dodge complete after
		else if(dodgeTimer > timeDodge + 0.15f)
		{
			ChangeToState (EnemyState.Free);	// Change to free state
		}
		
		
	}
	
	// Flinching state logic
	void FlinchingLogic()
	{
		flinchTimer += Time.deltaTime;
		
		if(flinchTimer > timeFlinch + 0.3f)
		{
			ChangeToState (EnemyState.Free);	// Change to free state
		}
	}
	
	// Dead state logic
	void DeadLogic()
	{
		if(isDying && anim.GetCurrentAnimatorStateInfo (0).nameHash == dyingStateHash)
		{
			isDying = false;	// Avoid death loop
		}
	}
	
	
	/* * * * * * * * * * * * * * * * * State Handling * * * * * * * * * * * * * * * * */
	
	/*
	 * ChangeToState
	 * 
	 * Set variables for state change.
	 */
	void ChangeToState(EnemyState state)
	{
		switch(state)
		{
		case  EnemyState.Free:
			FreeStateChange();
			break;
		case EnemyState.Blocking:
			BlockingStateChange();
			break;
		case EnemyState.Attacking:
			AttackingStateChange();
			break;
		case EnemyState.Dodging:
			DodgingStateChange();
			break;
		case EnemyState.Flinching:
			FlinchingStateChange();
			break;
		case EnemyState.Dead:
			DeadStateChange();
			break;
		}
	}
	
	// Change to free state
	void FreeStateChange()
	{
		enemyState = EnemyState.Free;
		
		isAttacking = false;
		isDodging = false;
		isFlinching = false;
		isBlocking = false;
		blockedAttack = false;
	}
	
	// Change to blocking state
	void BlockingStateChange()
	{
		enemyState = EnemyState.Blocking;
		
		isAttacking = false;
		isDodging = false;
		isSprinting = false;
		isFlinching = false;
		isBlocking = true;
		blockedAttack = false;
		
		blockTimer = 0.0f;
	}
	
	// Change to attacking state
	void AttackingStateChange()
	{
		enemyState = EnemyState.Attacking;
		
		isAttacking = true;
		isDodging = false;
		isSprinting = false;
		isFlinching = false;
		isBlocking = false;
		blockedAttack = false;
		
		attackRegistered = false;
		
		attackTimer = 0.0f;
	}
	
	// Change to dodging state
	void DodgingStateChange()
	{
		enemyState = EnemyState.Dodging;
		
		isAttacking = false;
		isDodging = true;
		isFlinching = false;
		isBlocking = false;
		blockedAttack = false;
		
		dodgeTimer = 0.0f;
	}
	
	// Change to flinching state
	void FlinchingStateChange()
	{
		enemyState = EnemyState.Flinching;
		
		isAttacking = false;
		isDodging = false;
		isSprinting = false;
		isFlinching = true;
		isBlocking = false;
		blockedAttack = false;
		
		flinchTimer = 0.0f;
	}
	
	void DeadStateChange()
	{
		enemyState = EnemyState.Dead;
		
		isDying = true;
		isDead = true;
	}


	/* * * * * * * * * * * * * * * AI Logic * * * * * * * * * * * * * * * * * * * */

	/*
	 * AILogic
	 * 
	 * This method handles the decision making for the character.
	 */
	void AILogic()
	{
		CanSee ();	// Check if player can be seen and set desination

		// Face the player if possible
		if(CanRotate ())
		{
			// TODO: Turn the character with a turn speed
			transform.LookAt (new Vector3(destination.x, transform.position.y, destination.z));
		}

		// Handle AI for specific states
		switch(enemyState)
		{
		case EnemyState.Free:
			AIFree ();
			break;
		case EnemyState.Attacking:
			AIAttacking();
			break;
		case EnemyState.Blocking:
			AIBlocking ();
			break;
		case EnemyState.Dodging:
			AIDodging ();
			break;
		}
	}

	// AI while free
	void AIFree()
	{
		// TODO: Move towards player until 2.0f
		// TODO: Wait until magnitude is greater than 4.0f to move again
		if((destination - transform.position).magnitude > 2.0f)
		{
			SetMovement (0.0f, 1.0f);	// Moving forward towards destination
		}
		else
		{
			SetMovement (0.0f, 0.0f);	// Stop moving
		}
	}

	// AI while attacking
	void AIAttacking()
	{

	}

	// AI while blocking
	void AIBlocking()
	{

	}

	// AI while dodging
	void AIDodging()
	{

	}
	
	
	/* * * * * * * * * * * * * * * Helper Functions * * * * * * * * * * * * * * * */

	/*
	 * SetMovement
	 * 
	 * This method sets the movement variables
	 */
	void SetMovement(float x, float y)
	{
		hMovement = x;
		vMovement = y;
		movement = new Vector2 (x, y);
	}

	/*
	 * TakeHit
	 * 
	 * This method handles logic from being being attacked.
	 */
	public override void TakeHit(Vector3 origin, float damage, AttackType type)
	{
		// Handle flinch-capable states
		switch(enemyState)
		{
		case EnemyState.Free:
			// Character can always take damage in free state
			TakeDamage (damage);
			break;
		case EnemyState.Blocking:
			// Is the character blocking in the direction of the attack?
			if(CanBlock(origin))
			{
				// Character can only lose stamina when not already blocking an attack
				if(!blockedAttack)
					blockedAttack = true;
			}
			// Character facing the wrong direction
			else
			{
				TakeDamage (damage);
			}
			break;
		case EnemyState.Attacking:
			// Character can always take damage while attacking
			TakeDamage (damage);
			break;
		case EnemyState.Dodging:
			// Is the character invincible?
			if(dodgeTimer > timeInvincible)
				TakeDamage (damage);
			break;
		}
	}
	
	// Can the character block the attack?
	bool CanBlock(Vector3 origin)
	{
		Vector3 direction = origin - transform.position;			// Direction to attack origin
		float angle = Vector3.Angle (direction, transform.forward);	// Angle of origin from character
		
		// Compare angle to block angle
		if(angle < blockAngle * 0.5f)
			return true;
		else
			return false;
	}
	
	/*
	 * TakeDamage
	 * 
	 * This method handles any damage dealt to the player.
	 */
	void TakeDamage(float damage)
	{
		enemyHealth.SetHealth (enemyHealth.GetHealth () - damage);

		// Check death state
		if (enemyHealth.GetHealth () <= 0.0f)
			ChangeToState (EnemyState.Dead);

		Debug.Log (damage + " damage received... " + enemyHealth.GetHealth () + " health remaining.");
	}
	
	/*
	 * SetAnimatorParameters
	 * 
	 * This method sets all of the parameters for the animator component.
	 */
	void SetAnimatorParameters()
	{
		anim.SetFloat (xVelocityHash, hMovement);
		anim.SetFloat (zVelocityHash, vMovement);
		anim.SetBool (isIdleHash, isIdle);
		anim.SetBool (movingForwardHash, movingForward);
		anim.SetBool (isBlockingHash, isBlocking);
		anim.SetBool (isAttackingHash, isAttacking);
		anim.SetBool (isDodgingHash, isDodging);
		anim.SetBool (isSprintingHash, isSprinting);
		anim.SetBool (isFlinchingHash, isFlinching);
		anim.SetBool (isDyingHash, isDying);
		anim.SetBool (isDeadHash, isDead);
		anim.SetBool (blockedAttackHash, blockedAttack);
		//anim.SetBool (continueAttackHash, continueAttack);	// Special case: we are setting this value in the attack state logic
	}
	
	/*
	 * CanRotate
	 * 
	 * This method determines when the character can rotate.
	 */
	public bool CanRotate()
	{
		bool canRotate = false;	// Initiate false
		
		// Handle rotatable states
		switch(enemyState)
		{
		case EnemyState.Free:
			// Character can always rotate in free state
			canRotate = true;
			break;
		case EnemyState.Attacking:
			// Character can rotate briefly at the beginning of the attack
			if(attackType == AttackType.Basic && attackTimer <= 0.3f)
				canRotate = true;
			else if(attackType == AttackType.Heavy && attackTimer <= 0.5f)
				canRotate = true;
			else
				canRotate = false;
			break;
		case EnemyState.Blocking:
			// Character can rotate if not recovering from an attack
			if(!blockedAttack)
				canRotate = true;
			else
				canRotate = false;
			break;
		case EnemyState.Dodging:
			// Character can always rotate while dodging
			canRotate = true;
			break;
		case EnemyState.Flinching:
			// Character can never rotate while flinching
			canRotate = false;
			break;
		case EnemyState.Dead:
			// Character can never rotate while flinching
			canRotate = false;
			break;
		}
		
		return canRotate;
	}

	/*
	 * CanSeeTarget
	 * 
	 * This method uses raycasting to find the target's location.
	 * The target's location is used to move the character and set
	 * attack destinations.
	 */
	bool CanSee()
	{
		Vector3 direction = target.transform.position - transform.position;

		RaycastHit hit;

		// 360 degree FOV initially
		if(Physics.Raycast(transform.position + transform.up, direction.normalized, out hit, sightRadius))
		{
			if(hit.collider.gameObject == target)
			{
				destination = target.transform.position;

				return true;
			}
		}

		return false;
	}
}
