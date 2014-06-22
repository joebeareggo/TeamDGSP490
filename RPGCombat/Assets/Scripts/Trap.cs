using UnityEngine;
using System.Collections;


public class Trap : MonoBehaviour {
	public int RandomNumber(int min, int max)
	{
		Random random = new Random();
		return random(min, max);
	}
	public float delayTime;


	// Use this for initialization
	void Start () {
		StartCoroutine (Go ());

	}

	IEnumerator Go()
	{
		while(true)
		{
			int counter = RandomNumber(1,15);
			animation.Play();
			yield return new WaitForSeconds(counter);
		}
	}

	// Update is called once per frame
	void Update () {
	
	}
}
