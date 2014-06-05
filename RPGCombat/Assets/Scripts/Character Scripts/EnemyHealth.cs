using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour {

	// Character stats
	float currentHealth;	// Current character health
	float maxHealth;		// Maximum player health
	
	// Use this for initialization
	void Start () {
		currentHealth = maxHealth = 200.0f;		// Initiate health to 100
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
}