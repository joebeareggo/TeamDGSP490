using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour {

	// Player stats
	float currentHealth;	// Current player health
	float maxHealth;		// Maximum player health
	float currentStamina;	// Current player stamina
	float maxStamina;		// Maximum player stamina
	
	// Use this for initialization
	void Start () {
		currentHealth = maxHealth = 100.0f;		// Initiate health to 100
		currentStamina = maxStamina = 100.0f;	// Initiate stamina to 100
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	// Get current health
	public float GetHealth()
	{
		return currentHealth;
	}
	
	// Set current health
	public void SetHealth(float value)
	{
		// Health value can't be negative
		if(value < 0)
		{
			currentHealth = 0;
		}
		// Health can't be greater than max
		else if(value > maxHealth)
		{
			currentHealth = maxHealth;
		}
		else
		{
			currentHealth = value;
		}
	}
	
	// Get max health
	public float GetMaxHealth()
	{
		return maxHealth;
	}
	
	// Set max health
	public void SetMaxHealth(float value)
	{
		// Health can't be negative or zero
		if(value < 1)
		{
			maxHealth = 1;
		}
		else
		{
			maxHealth = value;
		}
		
		// Ensure current health doesn't exceed maximum health
		if(currentHealth > maxHealth)
		{
			currentHealth = maxHealth;
		}
	}
	
	// Get current stamina
	public float GetStamina()
	{
		return currentStamina;
	}
	
	// Set current stamina
	public void SetStamina(float value)
	{
		// Stamina can't be negative
		if(value < 0)
		{
			currentStamina = 0;
		}
		// Stamina can't exceed maximum stamina
		else if(value > maxStamina)
		{
			currentStamina = maxStamina;
		}
		else
		{
			currentStamina = value;
		}
	}
	
	// Get maximum stamina
	public float GetMaxStamina()
	{
		return maxStamina;
	}
	
	// Set maximum stamina
	public void SetMaxStamina(float value)
	{
		// Max stamina must be greater than zero
		if(value <= 0)
		{
			maxStamina = 1;
		}
		else
		{
			maxStamina = value;
		}
		
		// Check that current stamina value doens't exceed maximum
		if(currentStamina > maxStamina)
		{
			currentStamina = maxStamina;
		}
	}
}