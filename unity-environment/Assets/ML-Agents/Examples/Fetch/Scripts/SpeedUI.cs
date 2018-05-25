using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class SpeedUI : MonoBehaviour {

	public float topSpeed;
	public TextMeshProUGUI speedTextUI;
	public DogAgent fastestDog;
	// Use this for initialization
	void Start () {
		topSpeed = 0;
		UpdateSpeedUI(0);
	}
	
	// // Update is called once per frame
	// void Update () {
		
	// }

	void UpdateSpeedUI(float speed)
	{
		speedTextUI.text = topSpeed.ToString();
	}

	public void CollectSpeed(float speed)
	{
		if(speed > topSpeed)
		{
			// topSpeed = speed;
			topSpeed = Mathf.RoundToInt(speed);
			UpdateSpeedUI(speed);
		}
	}
}
