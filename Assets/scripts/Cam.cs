using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{

	float x, y, z, leftright, updown;
	float movespeed = 50;
	float scrollspeed = 500;

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		x = Input.GetAxis ("Horizontal");
		y = Input.GetAxis ("Vertical");
		z = Input.GetAxis ("Mouse ScrollWheel");

		/*leftright = Input.GetAxis ("Mouse X");
		updown = Input.GetAxis ("Mouse Y");*/

		transform.Translate (x * movespeed * Time.deltaTime, y * movespeed * Time.deltaTime, z * scrollspeed * Time.deltaTime);
		//transform.Rotate (-1 * leftright * movespeed * Time.deltaTime, updown * movespeed * Time.deltaTime, 0);
	}
}
