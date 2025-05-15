using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class primitiveCity : MonoBehaviour {

	public string levelName;

	int[,] map;

	// Use this for initialization
	void Start () {
		Load ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	int checkMain(int x, int y) {
		int r = 2;
		for (int i = x - 2; i <= x + 2; i++) {
			for (int j = y - 2; j <= y + 2; j++) {
				try {
					if(map[i,j] == -1) r = 6;
					else if(map[i,j] == -2) r = 4; 
				} 
				catch {	}
			}
		}
		return r;
	}

	void Load() {
		// Thank you: https://github.com/koguz/se354_2013/blob/master/SE354Project/Assets/Level.cs

		StreamReader dosya = File.OpenText("Assets/" + levelName);
		string icerik = dosya.ReadToEnd();
		dosya.Close ();

		string[] satirlar = icerik.Split ("\n" [0]);
		string[] sutunlar = satirlar [0].Split (" " [0]);
		int satir = satirlar.Length;
		int sutun = sutunlar.Length;

		map = new int[satir, sutun];

		for (int i = 0; i < satir; i++) {
			
			string[] hucreler = satirlar [i].Split (" " [0]);

			for (int j = 0; j < sutun; j++) {
				
				int.TryParse (hucreler [j], out map [i, j]);

				GameObject kare = GameObject.CreatePrimitive (PrimitiveType.Cube);
				kare.transform.position = new Vector3 (i, -0.04f, j);
				kare.transform.localScale = new Vector3(1.0f, 0.01f, 1.0f);
				kare.GetComponent<Renderer> ().material.color = Color.gray;

				if (map [i, j] >= 1) {
					int buildingsize = checkMain(i,j); 
					kare.transform.localScale = new Vector3 (0.9f, (1.0f + Random.value) * buildingsize, 0.9f);
					float myr = 0.1f * buildingsize;
					kare.GetComponent<Renderer> ().material.color = new Color (0.5f + myr, 0.5f + myr, 0.5f + myr);
				}

			}
		}

	}
}
