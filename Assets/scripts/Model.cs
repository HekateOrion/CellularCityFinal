using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Model : MonoBehaviour
{

	GameObject primaryRoadTile, secondaryRoadTile, greeneryTile, treeModel, cubePrefab;

	List<Level.Edge> edgeList;

	Texture[] apartmentTextureArray, skyscraperTextureArray, roofTextureArray;


	public Model (GameObject prt, GameObject srt, GameObject gt, GameObject tm, GameObject cp, Texture[] ata, Texture[] sta, Texture[] rta)
	{
		primaryRoadTile = prt;
		secondaryRoadTile = srt;
		greeneryTile = gt;
		treeModel = tm;
		cubePrefab = cp;
		apartmentTextureArray = ata;
		skyscraperTextureArray = sta;
		roofTextureArray = rta;

		edgeList = Level.edgeList;
	}



	public void ShowModel ()
	{

		PlaceRoadTiles ();

		PlaceGreenery ();

		PlaceBuildings ();

	}





	void PlaceRoadTiles ()
	{
		Vector3 srDisplacement = new Vector3 (0.0f, -0.01f, 0.0f);

		foreach (Level.Edge e in edgeList) {

			if (e.v1.value == -1 && e.v2.value == -1) {
				GameObject go = GameObject.Instantiate (primaryRoadTile);
				go.transform.position = (e.v1.position + e.v2.position) / 2;
				go.transform.Rotate (90.0f, 0.0f, 0.0f);
				go.tag = "cube";
				LineRenderer lr = go.GetComponent<LineRenderer> ();
				lr.startWidth = 1.0f;
				lr.endWidth = 1.0f;
				lr.SetPosition (0, e.v1.position);
				lr.SetPosition (1, e.v2.position);
				lr.alignment = LineAlignment.Local;
			} else {
				GameObject go = GameObject.Instantiate (secondaryRoadTile);
				go.transform.position = (e.v1.position + e.v2.position) / 2;
				go.transform.Rotate (90.0f, 0.0f, 0.0f);
				go.tag = "cube";
				LineRenderer lr = go.GetComponent<LineRenderer> ();
				lr.startWidth = 0.5f;
				lr.endWidth = 0.5f;
				lr.SetPosition (0, e.v1.position + srDisplacement);
				lr.SetPosition (1, e.v2.position + srDisplacement);
				lr.alignment = LineAlignment.Local;
			}

		}
	}





	void PlaceGreenery ()
	{
		float M = Level.City.GetLength (0);
		float N = Level.City.GetLength (1);

		Vector3 greeneryPosition = new Vector3 (M / 2, -0.02f, N / 2);
		Vector3 greeneryScale = new Vector3 (M, N, 1.0f);
		Color greeneryColor = new Color (0.0f, (109.0f / 255.0f), (1.0f / 255.0f));

		GameObject greenPatch = GameObject.CreatePrimitive (PrimitiveType.Quad);
		greenPatch.transform.position = greeneryPosition;
		greenPatch.transform.Rotate (90.0f, 0.0f, 0.0f);
		greenPatch.transform.localScale = greeneryScale;
		greenPatch.GetComponent<Renderer> ().material.color = greeneryColor;


		Vector3 treeScale = new Vector3 (0.2f, 0.2f, 0.2f);
		//Color treeColor = new Color ((11.0f / 255.0f), (94.0f / 255.0f), (32.0f / 255.0f));

		foreach (Level.Road g in Level.greenery) {
			GameObject tree = GameObject.Instantiate (treeModel);
			tree.transform.position = g.position;
			tree.transform.localScale = treeScale;
		}
	}





	void PlaceBuildings ()
	{
		foreach (Level.Plot p in Level.plotList) {

			foreach (Level.Building b in p.buildings) {

				int i_length = b.bBox.i_max - b.bBox.i_min + 1;
				int j_length = b.bBox.j_max - b.bBox.j_min + 1;

				float x_size = i_length - 0.3f;
				float z_size = j_length - 0.3f;
				float y_size = 1.0f;

				Texture wall, roof;

				if (i_length <= 3 || j_length <= 3) {
					y_size = Random.Range (1.0f, 3.0f);
					wall = apartmentTextureArray [Random.Range (0, apartmentTextureArray.Length)];
				} else {
					y_size = Random.Range (2.0f, 8.0f);
					wall = skyscraperTextureArray [Random.Range (0, skyscraperTextureArray.Length)];
				}

				roof = roofTextureArray [Random.Range (0, roofTextureArray.Length)];

				GameObject cube = GameObject.Instantiate (cubePrefab);
				cube.name = b.id.ToString ();
				cube.tag = "cube";
				cube.transform.position = b.position + new Vector3 (0.0f, y_size / 2.0f, 0.0f);
				cube.transform.localScale = new Vector3 (x_size, y_size, z_size);
				cube.transform.GetChild (0).GetComponent<Renderer> ().material.mainTexture = wall;
				cube.transform.GetChild (1).GetComponent<Renderer> ().material.mainTexture = wall;
				cube.transform.GetChild (2).GetComponent<Renderer> ().material.mainTexture = wall;
				cube.transform.GetChild (3).GetComponent<Renderer> ().material.mainTexture = wall;
				cube.transform.GetChild (4).GetComponent<Renderer> ().material.mainTexture = roof;

			}

		}
	}



}