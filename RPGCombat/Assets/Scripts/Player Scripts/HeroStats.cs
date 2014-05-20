﻿using UnityEngine;
using System.Collections;

public class HeroStats : MonoBehaviour {

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

	// GUI elements
	void OnGUI()
	{
		// Health bar
		// Only show if health is greater than 0
		if(currentHealth > 0)
		{
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (0, 0, Color.red);
			tex.Apply ();
			GUI.skin.box.normal.background = tex;
			GUI.Box (new Rect (Screen.width * 0.45f * (1.0f - currentHealth / maxHealth) + 25.0f, 25.0f, 
			                  (Screen.width * 0.45f) * (currentHealth / maxHealth), 25.0f), "");
		}

		// Stamina bar
		// Only show if stamina is greater than 0
		if(currentStamina > 0)
		{
			Texture2D tex = new Texture2D(1, 1);
			tex.SetPixel (0, 0, Color.green);
			tex.Apply ();
			GUI.skin.box.normal.background = tex;
			GUI.Box (new Rect(Screen.width * 0.55f, 25.0f, 
			                  (Screen.width * 0.45f - 25.0f) * (currentStamina / maxStamina), 25.0f), "");
		}
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
	}

	// 
}
