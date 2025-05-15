using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Level : MonoBehaviour
{

	static int ID = 0;


	#region CLASSES

	public class MatrixCell
	{
		public int id;
		public int value;
		public Node correspondingNode;
	}


	public interface Node
	{
	}

	public class Road : Node
	{
		public int id;
		public int value;
		public CellCoordinates cell;
		public Vector3 position;
		public List<Road> neighbours;
	}

	public class Plot : Node
	{
		public int id;
		public List<Building> buildings;
		public List<CellCoordinates> cells;
		public BoundingBox bBox;
		public int threshold;

		public Plot ()
		{
			id = ++ID;
			buildings = new List<Building> ();
			cells = new List<CellCoordinates> ();
			bBox = new BoundingBox ();
			threshold = 0;
		}

		bool FindBoundingBox ()
		{
			if (cells == null || cells.Count == 0)
				return false;

			bBox.i_min = cells [0].i;
			bBox.i_max = cells [0].i;
			bBox.j_min = cells [0].j;
			bBox.j_max = cells [0].j;

			foreach (CellCoordinates cc in cells) {
				if (cc.i < bBox.i_min)
					bBox.i_min = cc.i;
				if (cc.i > bBox.i_max)
					bBox.i_max = cc.i;
				if (cc.j < bBox.j_min)
					bBox.j_min = cc.j;
				if (cc.j > bBox.j_max)
					bBox.j_max = cc.j;
			}
			return true;
		}

		public void FindThreshold ()
		{
			threshold = 0;

			if (!FindBoundingBox ())
				return;


			float i_length = bBox.i_max - bBox.i_min + 1;
			float j_length = bBox.j_max - bBox.j_min + 1;

			if (i_length < 4 || j_length < 4) {
				threshold = 4;
			} else {
				if (i_length < j_length) {
					threshold = Mathf.CeilToInt (i_length / 2) * Mathf.CeilToInt (i_length / 2);
				} else {
					threshold = Mathf.CeilToInt (j_length / 2) * Mathf.CeilToInt (j_length / 2);
				}
			}

			if (cells.Count / (i_length * j_length) < 0.5f)
				threshold = threshold / 2;
		}
	}

	public class Building : Node
	{
		public int id;
		public Vector3 position;
		public Plot plot;
		public List<CellCoordinates> cells;
		public BoundingBox bBox;

		public Building (Plot parentPlot)
		{
			id = ID++;
			position = new Vector3 ();
			plot = parentPlot;
			cells = new List<CellCoordinates> ();
			bBox = new BoundingBox ();
		}

		public void FindBoundingBox ()
		{
			if (cells == null || cells.Count == 0)
				return;

			bBox.i_min = cells [0].i;
			bBox.i_max = cells [0].i;
			bBox.j_min = cells [0].j;
			bBox.j_max = cells [0].j;

			foreach (CellCoordinates cc in cells) {
				if (cc.i < bBox.i_min)
					bBox.i_min = cc.i;
				if (cc.i > bBox.i_max)
					bBox.i_max = cc.i;
				if (cc.j < bBox.j_min)
					bBox.j_min = cc.j;
				if (cc.j > bBox.j_max)
					bBox.j_max = cc.j;
			}
		}
	}


	public class BoundingBox
	{
		public int i_min, i_max, j_min, j_max;
	}

	public class CellCoordinates
	{
		public int i, j;

		public CellCoordinates (int i, int j)
		{
			this.i = i;
			this.j = j;
		}
	}


	public class Edge
	{
		public Road v1;
		public Road v2;
	}

	#endregion

	 


	int M = 100, N = 100;
	public static MatrixCell[,] City;
	static MatrixCell[,] CityCopy;

	public static List<Road> primaryRoad, secondaryRoad, greenery;
	public static List<Edge> edgeList;
	public static List<Plot> plotList;

	public GameObject primaryRoadTile, secondaryRoadTile, greeneryTile, treeModel, cubePrefab;
	public Texture[] apartmentTextureArray, skyscraperTextureArray, roofTextureArray;

	Text MValue, NValue;
	Slider MSlider, NSlider;
	Dropdown FileDropdown;




	void Start ()
	{
		DirectoryInfo directoryInfo = new DirectoryInfo (Application.dataPath + "/maps");
		FileInfo[] fileInfo = directoryInfo.GetFiles ("*.txt", SearchOption.AllDirectories);

		FileDropdown = GameObject.Find ("FileDropdown").GetComponent<Dropdown> ();
		FileDropdown.options.Clear ();

		FileDropdown.options.Add (new Dropdown.OptionData ("Select file..."));
		foreach (FileInfo file in fileInfo) {
			Dropdown.OptionData optionData = new Dropdown.OptionData (file.Name);
			FileDropdown.options.Add (optionData);
		}
		FileDropdown.value = 0;

		MSlider = GameObject.Find ("MSlider").GetComponent<Slider> ();
		NSlider = GameObject.Find ("NSlider").GetComponent<Slider> ();

		MValue = GameObject.Find ("MValue").GetComponent<Text> ();
		NValue = GameObject.Find ("NValue").GetComponent<Text> ();

		MValue.text = M.ToString ();
		NValue.text = N.ToString ();

	}






	void CleanMap ()
	{
		GameObject[] list = GameObject.FindGameObjectsWithTag ("cube");
		foreach (GameObject go in list)
			Destroy (go);
	}

	void ResetMatrix ()
	{		
		City = new MatrixCell[M, N];
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				City [i, j] = new MatrixCell ();
			}
		}
	}

	void ResetLists ()
	{
		primaryRoad = new List<Road> ();
		secondaryRoad = new List<Road> ();
		greenery = new List<Road> ();
		edgeList = new List<Edge> ();
		plotList = new List<Plot> ();
	}



	void PlaceCubes ()
	{
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {

				if (City [i, j].value < 0) {

					GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
					cube.tag = "cube";
					cube.transform.position = new Vector3 (i, 0.0f, j);
					cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
					if (City [i, j].value == -1)
						cube.GetComponent<Renderer> ().material.color = Color.green;
					if (City [i, j].value == -2)
						cube.GetComponent<Renderer> ().material.color = Color.white;
				}					
			}
		}
	}



	void CreateCityCopy ()
	{
		CityCopy = new MatrixCell[M, N];

		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				
				CityCopy [i, j] = new MatrixCell ();

				CityCopy [i, j].correspondingNode = City [i, j].correspondingNode;
				CityCopy [i, j].id = City [i, j].id;
				CityCopy [i, j].value = City [i, j].value;
			}
		}

	}



	void Save ()
	{
		if (CityCopy == null || CityCopy.GetLength (0) == 0 || CityCopy.GetLength (1) == 0)
			return;

		string filename = DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond + ".txt";

		string content = "";

		for (int i = 0; i < M - 1; i++) {
			string line = "";
			for (int j = 0; j < N - 1; j++) {
				line += CityCopy [i, j].value + " ";
			}
			line += CityCopy [i, N - 1].value + "\n";
			content += line;
		}
		string lastline = "";
		for (int j = 0; j < N - 1; j++) {
			lastline += CityCopy [M - 1, j].value + " ";
		}
		lastline += CityCopy [M - 1, N - 1].value;
		content += lastline;


		StreamWriter file = File.CreateText ("Assets/maps/" + filename);
		file.Write (content);
		file.Close ();
	}



	void Load ()
	{
		if (FileDropdown.value == 0)
			return;

		string mapName = FileDropdown.options [FileDropdown.value].text;

		StreamReader file = File.OpenText ("Assets/maps/" + mapName);
		string content = file.ReadToEnd ();
		file.Close ();

		string[] lines = content.Split ("\n" [0]);
		string[] columns = lines [0].Split (" " [0]);

		M = lines.Length;
		N = columns.Length;

		City = new MatrixCell[M, N];

		for (int i = 0; i < M; i++) {
			string[] cells = lines [i].Split (" " [0]);
			for (int j = 0; j < N; j++) {
				City [i, j] = new MatrixCell ();
				int.TryParse (cells [j], out City [i, j].value);
			}
		}
	}




	public void click_SaveMap ()
	{
		Save ();
	}

	public void click_LoadMap ()
	{
		CleanMap ();

		// Load() da matrisi oluşturduğu ve sıfırladığı için ResetMatrix() i eklemedim.
		ResetLists ();

		Load ();

		PlaceCubes ();
	}


	public void click_RunAutomata ()
	{
		CleanMap ();
		
		ResetMatrix ();
		ResetLists ();

		Automata au = new Automata ();
		au.RunAutomata ();

		CreateCityCopy ();

		PlaceCubes ();
	}


	public void click_CreateGraph ()
	{
		CleanMap ();
		
		RoadGraph rg = new RoadGraph ();
		rg.CreateGraph ();

		BuildingGraph bg = new BuildingGraph ();
		bg.CreateGraph ();
	}


	public void click_ShowModel ()
	{
		CleanMap ();

		Model m = new Model (primaryRoadTile, secondaryRoadTile, greeneryTile, treeModel, cubePrefab, apartmentTextureArray, skyscraperTextureArray, roofTextureArray);
		m.ShowModel ();
	}






	public void valueChange_MSlider ()
	{
		M = (int)MSlider.value;
		MValue.text = M.ToString ();
	}

	public void valueChange_NSlider ()
	{
		N = (int)NSlider.value;
		NValue.text = N.ToString ();
	}



}
