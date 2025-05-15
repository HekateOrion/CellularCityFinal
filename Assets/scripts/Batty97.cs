using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Batty97 : MonoBehaviour
{
	/* Implementing the idea from 
	 * "Cellular Automata and Urban Form: A Primer"
	 * by Michael Batty
	 * 1997
	 * The paper is old, but the idea is a good starting point.
	 * The city sprawls from a single center. 
	 * That center is developed (state 1), rest of the map is undeveloped (state 0).
	 * The rule is 
	 * IF there is at least one developed cell in the Moore neighbourhood around the cell in question
	 * THEN the cell is developed with a probability of p 
	 * The paper also suggests to drop the probability with each consideration, p^n 
	 * 
	 * I'll also be using a previous cellular automata implementation of mine... 
	 * https://github.com/koguz/se354_2015/blob/master/Assets/Scripts/DungeonGenerator.cs
	 */
	// Use this for initialization
	public float probability = 0.8f;
	private int msize = 100;
	private float[,] p;
	private int gen;

	private int[,] map;
	private GameObject[,] cubes;

	void Start ()
	{
		map = new int[msize, msize];
		cubes = new GameObject[msize, msize];
		p = new float[msize, msize];
		for (int i = 0; i < msize; i++) {
			for (int j = 0; j < msize; j++) {
				p [i, j] = 0.8f;
				map [i, j] = 0;
				GameObject k = GameObject.CreatePrimitive (PrimitiveType.Cube);
				k.transform.localScale = new Vector3 (0.9f, 0.9f, 0.9f);
				k.transform.position = new Vector3 (i, 0, j);
				k.name = getName (i, j);
				cubes [i, j] = k;
				k.SetActive (false);
			}
		}
		/* For the first implementation, let's make the center of the map
		 * developed. Later on, we should be able to start with more than 
		 * one city centers, which can be merged as the automata continues
		 */
		map [msize / 2, msize / 2] = 1;
		cubes [msize / 2, msize / 2].SetActive (true);
		gen = 1;
	}

	string getName (int i, int j)
	{ // looks like we wont need this. but let's keep it.
		return "C." + i + "." + j;
	}
	
	// Update is called once per frame
	void Update ()
	{
		/* Let the automata increment one generation if the user presses space
		 * That way, we can see the changes at every state... 
		 */

		if (Input.GetKeyUp (KeyCode.Space)) {
			Debug.Log ("running CA");
			int[,] temp = new int[msize, msize];
			for (int i = 0; i < msize; i++) {
				for (int j = 0; j < msize; j++) {
					if (temp [i, j] == 0) {
						/* for this particular node, calculate T */
						if (getT (i, j) > 0 && Random.value < Mathf.Pow (p [i, j], gen)) {
							/* if there is at least 1 developed node in the
						 	* neighbourhood (computed by getT) and if the
						 	* random value is less than probability 0.8
						 	* then set new state.
						 	*/
							temp [i, j] = 1;
							cubes [i, j].SetActive (true);
						} else
							temp [i, j] = 0;
					}
				}
			}
			// copy temp to map
			for (int i = 0; i < msize; i++) {
				for (int j = 0; j < msize; j++) {
					map [i, j] = temp [i, j];
				}
			}
			gen++;
		}
	}

	private int getT (int x, int y)
	{
		int TT = 0;
		for (int i = b (x - 1); i <= b (x + 1); i++) {
			for (int j = b (y - 1); j <= b (y + 1); j++) {
				if (x == y)
					continue; // this is me
				if (map [i, j] == 1)
					TT++;
			}
		}
		return TT;
	}

	private int b (int v)
	{
		int m = msize;
		if (v < 0)
			return 0;
		else if (v >= m)
			return m - 1;
		else
			return v;
	}

}
