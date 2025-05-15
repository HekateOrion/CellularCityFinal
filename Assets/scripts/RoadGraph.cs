using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;


public class RoadGraph : MonoBehaviour
{


	#region CLASSES AND VARIABLES

	int ID = 0;

	Level.MatrixCell[,] map;
	int M, N;

	List<Level.Road> primaryRoad, secondaryRoad, greenery;
	List<Level.Edge> edgeList;

	#endregion



	public RoadGraph ()
	{
		map = Level.City;
		M = Level.City.GetLength (0);
		N = Level.City.GetLength (1);		
		
		primaryRoad = Level.primaryRoad;
		secondaryRoad = Level.secondaryRoad;
		greenery = Level.greenery;
		edgeList = Level.edgeList;
	}




	#region METHODS

	public void CreateGraph ()
	{		
		MatrixToNode ();

		CreateRoadGraphs ();


		RemoveTriangles ();

		bool check = false;
		do {
			check = RemoveSquares ();
		} while(check);

		ConnectBrokenPrimaryRoad ();

		RemoveIsolated ();


		SmoothingPrimary ();
		SmoothingPrimary ();
		SmoothingPrimary ();

		CombineRoads ();

		SmoothingSecondary ();
		SmoothingSecondary ();
		SmoothingSecondary ();


		RemoveObstructingGreenery ();


		PlaceNodes ();
		PlaceEdges ();
	}




	#region Utility Methods

	float distance (Vector3 v1, Vector3 v2)
	{
		return (float)Mathf.Sqrt ((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
	}


	void ConnectNodes (Level.Road v1, Level.Road v2)
	{
		/* v1 ve v2 node'larını aralarında edge oluşturarak bağlar */

		if (v1.neighbours == null)
			v1.neighbours = new List<Level.Road> ();
		if (v2.neighbours == null)
			v2.neighbours = new List<Level.Road> ();

		v1.neighbours.Add (v2);
		v2.neighbours.Add (v1);

		Level.Edge e = new Level.Edge ();
		e.v1 = v1;
		e.v2 = v2;
		edgeList.Add (e);
	}


	void RemoveConnection (Level.Road n1, Level.Road n2)
	{
		if (n1.neighbours == null || n2.neighbours == null)
			return;

		int i = 0;
		for (; i < n1.neighbours.Count; i++) {
			if (n1.neighbours [i].id == n2.id)
				break;
		}
		n1.neighbours.RemoveAt (i);

		i = 0;
		for (; i < n2.neighbours.Count; i++) {
			if (n2.neighbours [i].id == n1.id)
				break;
		}
		n2.neighbours.RemoveAt (i);

		i = 0;
		for (; i < edgeList.Count; i++) {
			if ((edgeList [i].v1.id == n1.id && edgeList [i].v2.id == n2.id) || (edgeList [i].v1.id == n2.id && edgeList [i].v2.id == n1.id))
				break;
		}
		edgeList.RemoveAt (i);
	}


	bool isConnected (Level.Road v1, Level.Road v2)
	{
		/* v1, v2 ile bağlı mı?
		 * v2'nin neighbour listesinde v1 varsa "bağlıdır" deriz
		 * v1'nin neighbour listesinde v2 varsa "bağlıdır" deriz
		 * */

		if (v2.neighbours == null || v1.neighbours == null)
			return false;

		bool v1_in_v2 = false, v2_in_v1 = false;

		foreach (Level.Road n in v2.neighbours) {
			if (n.id == v1.id)
				v1_in_v2 = true;
		}
		foreach (Level.Road n in v1.neighbours) {
			if (n.id == v2.id)
				v2_in_v1 = true;
		}

		if (v1_in_v2 && v2_in_v1)
			return true;
		
		return false;
	}


	void BreakHypotenuse (Level.Road n1, Level.Road n2, Level.Road n3)
	{
		float d12, d23, d13;

		d12 = (n1.position - n2.position).magnitude;
		d23 = (n2.position - n3.position).magnitude;
		d13 = (n1.position - n3.position).magnitude;

		if (d12 >= d23 && d12 >= d13) {
			RemoveConnection (n1, n2);
			return;
		}
		if (d23 >= d12 && d23 >= d13) {
			RemoveConnection (n2, n3);
			return;
		}
		if (d13 >= d12 && d13 >= d23) {
			RemoveConnection (n1, n3);
			return;
		}
	}

	#endregion


	#region Creating Road Graphs

	void MatrixToNode ()
	{
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {

				if (map [i, j].value == -1) {
					
					try {
						if (map [i, j - 1].value == -1 && map [i + 1, j].value == -1 && map [i + 1, j - 1].value == -1) {

							Level.Road node = new Level.Road ();

							map [i, j].id = ++ID;
							map [i, j].correspondingNode = node;

							node.id = map [i, j].id;
							node.value = map [i, j].value;

							Level.CellCoordinates cc = new Level.CellCoordinates (i, j);
							node.cell = cc;

							node.position = new Vector3 (i, 0.0f, j);

							primaryRoad.Add (node);
						}
					} catch (IndexOutOfRangeException e) {
					}
				}

				if (map [i, j].value == -2) {
					
					Level.Road node = new Level.Road ();

					map [i, j].id = ++ID;
					map [i, j].correspondingNode = node;

					node.id = map [i, j].id;
					node.value = map [i, j].value;

					Level.CellCoordinates cc = new Level.CellCoordinates (i, j);
					node.cell = cc;

					node.position = new Vector3 (i, 0.0f, j);

					secondaryRoad.Add (node);
				}
			}
		}

		CreateGreenery ();
	}


	void CreateGreenery ()
	{
		int i, j, neighbourCount;

		foreach (Level.Road node in primaryRoad) {

			i = (int)node.position.x;
			j = (int)node.position.z;
			neighbourCount = 0;

			for (int k = i - 1; k <= i + 1; k++) {
				for (int l = j - 1; l <= j + 1; l++) {
					if (k == i && l == j)
						continue;
					try {
						if (map [k, l].correspondingNode != null && map [k, l].value == -1)
							neighbourCount++;
					} catch (IndexOutOfRangeException e) {
					}
				}
			}

			if (neighbourCount == 8) {
				greenery.Add (node);
			}
		}

		foreach (Level.Road node in greenery) {
			primaryRoad.Remove (node);
			node.value = -3;
			map [(int)node.position.x, (int)node.position.z].value = -3;
		}
	}


	void CreateRoadGraphs ()
	{
		// 1. yollar : 4-connected komşusu 2'den az ise çapraz komşu ara.
		// 2. yollar : 4-connected komşusu 2'den az ise çapraz komşu ara.
		// Greenery  : 4-connected komşusu 2'den az ise çapraz komşu ara.

		int i, j;


		#region Primary Road

		foreach (Level.Road currentNode in primaryRoad) {

			i = (int)currentNode.position.x;
			j = (int)currentNode.position.z;

			for (int k = i - 1; k <= i + 1; k++) {
				for (int l = j - 1; l <= j + 1; l++) {

					if ((k != i && l != j) || (k == i && l == j))
						continue;

					try {
						if (map [k, l].correspondingNode != null && map [k, l].value == map [i, j].value) {

							Level.Road otherNode = (Level.Road)map [k, l].correspondingNode;

							if (!isConnected (currentNode, otherNode))
								ConnectNodes (currentNode, otherNode);
						}
					} catch (IndexOutOfRangeException e) {
					}
				}
			}


			if (currentNode.neighbours == null || (currentNode.neighbours != null && currentNode.neighbours.Count < 2)) {

				for (int k = i - 1; k <= i + 1; k++) {
					for (int l = j - 1; l <= j + 1; l++) {
						
						if (k == i || l == j)
							continue;

						try {
							if (map [k, l].correspondingNode != null && map [k, l].value == map [i, j].value) {

								Level.Road otherNode = (Level.Road)map [k, l].correspondingNode;

								if (!isConnected (currentNode, otherNode))
									ConnectNodes (currentNode, otherNode);
							}
						} catch (IndexOutOfRangeException e) {
						}
					}
				}
			}
		}
		#endregion

		#region Secondary Road

		foreach (Level.Road currentNode in secondaryRoad) {

			i = (int)currentNode.position.x;
			j = (int)currentNode.position.z;

			for (int k = i - 1; k <= i + 1; k++) {
				for (int l = j - 1; l <= j + 1; l++) {

					if ((k != i && l != j) || (k == i && l == j))
						continue;

					try {
						if (map [k, l].correspondingNode != null && map [k, l].value == map [i, j].value) {

							Level.Road otherNode = (Level.Road)map [k, l].correspondingNode;

							if (!isConnected (currentNode, otherNode))
								ConnectNodes (currentNode, otherNode);
						}
					} catch (IndexOutOfRangeException e) {
					}
				}
			}


			if (currentNode.neighbours == null || (currentNode.neighbours != null && currentNode.neighbours.Count < 2)) {

				for (int k = i - 1; k <= i + 1; k++) {
					for (int l = j - 1; l <= j + 1; l++) {

						if (k == i || l == j)
							continue;

						try {
							if (map [k, l].correspondingNode != null && map [k, l].value == map [i, j].value) {

								Level.Road otherNode = (Level.Road)map [k, l].correspondingNode;

								if (!isConnected (currentNode, otherNode))
									ConnectNodes (currentNode, otherNode);
							}
						} catch (IndexOutOfRangeException e) {
						}
					}
				}
			}
		}
		#endregion
	}

	#endregion


	#region Making the Necessary Adjustments

	void RemoveIsolated ()
	{		
		List<Level.Road> toBeRemoved = new List<Level.Road> ();
		foreach (Level.Road n in primaryRoad) {
			if (n.neighbours == null) {
				toBeRemoved.Add (n);
			}
		}
		foreach (Level.Road n in toBeRemoved) {
			primaryRoad.Remove (n);
		}

		toBeRemoved = new List<Level.Road> ();
		foreach (Level.Road n in secondaryRoad) {
			if (n.neighbours == null) {
				toBeRemoved.Add (n);
			}
		}
		foreach (Level.Road n in toBeRemoved) {
			secondaryRoad.Remove (n);
		}
	}


	void RemoveTriangles ()
	{	
		// EDGE'LERi KALDıRıR

		foreach (Level.Road node in primaryRoad) {
			if (node.neighbours != null && node.neighbours.Count > 1) {
				for (int i = 0; i < node.neighbours.Count - 1; i++) {
					for (int j = i + 1; j < node.neighbours.Count; j++) {
						if (isConnected (node.neighbours [i], node.neighbours [j]))
							BreakHypotenuse (node, node.neighbours [i], node.neighbours [j]);
					}
				}
			}
		}

		foreach (Level.Road node in secondaryRoad) {
			if (node.neighbours != null && node.neighbours.Count > 1) {
				for (int i = 0; i < node.neighbours.Count - 1; i++) {
					for (int j = i + 1; j < node.neighbours.Count; j++) {
						if (isConnected (node.neighbours [i], node.neighbours [j]))
							BreakHypotenuse (node, node.neighbours [i], node.neighbours [j]);
					}
				}
			}
		}
	}


	bool RemoveSquares ()
	{
		// NODELARı KALDıRıR

		List<Level.Road> toBeRemoved = new List<Level.Road> ();

		foreach (Level.Road node in primaryRoad) {

			if (node.neighbours != null && node.neighbours.Count == 2) {	// node'un kendisi junction değilse

				int junctionCount = 0;
				foreach (Level.Road neighbour in node.neighbours) {	// komşuları junction mı?
					if (neighbour.neighbours.Count >= 3)
						junctionCount++;
				}

				if (junctionCount == 2) {
					// eğer node iki kavşağın arasında ise test et: bu iki komşunun ortak komşusu var mı?
					Level.Road n1 = node.neighbours [0];
					Level.Road n2 = node.neighbours [1];
					

					bool loop = false;
					foreach (Level.Road n in n1.neighbours) {
						foreach (Level.Road m in n2.neighbours) {
							if (n.id != node.id && n.id == m.id) {
								loop = true;
								break;
							}
						}
					}

					if (loop)
						toBeRemoved.Add (node);
				}
			}
		}

		bool nodesRemoved = false;

		if (toBeRemoved.Count > 0) {

			nodesRemoved = true;
			foreach (Level.Road node in toBeRemoved) {
				if (node.neighbours == null)
					continue;
				int neighbourCount = node.neighbours.Count;
				for (int i = 0; i < neighbourCount; i++) {
					RemoveConnection (node, node.neighbours [0]);
				}
				primaryRoad.Remove (node);
			}
		}

		return nodesRemoved;
	}


	void ConnectBrokenPrimaryRoad ()
	{
		List<Level.Road> oneLinkedNodeList = new List<Level.Road> ();
		float range = 5.0f;

		foreach (Level.Road node in primaryRoad) {
			if (node.neighbours != null && node.neighbours.Count == 1) {
				Level.Road n = node;
				oneLinkedNodeList.Add (n);
			}
		}

		foreach (Level.Road node in oneLinkedNodeList) {			
			if (node.neighbours.Count == 1) {
				Level.Road minNode = null;
				float dist, minDist = range + 100.0f;

				foreach (Level.Road check in oneLinkedNodeList) {
					if (node.id != check.id) {
						dist = distance (node.position, check.position);
						if (dist < range && dist < minDist) {
							minDist = dist;
							minNode = check;
						}
					}
				}

				if (minNode != null)
					ConnectNodes (node, minNode);
			}
		}
	}


	void RemoveObstructingGreenery ()
	{
		List<Level.Road> toBeRemovedList = new List<Level.Road> ();
		bool toBeRemoved;

		float minDist = 1.0f;


		foreach (Level.Road g in greenery) {

			toBeRemoved = false;

			int i = (int)g.position.x;
			int j = (int)g.position.z;
			int neighbourCount = 0;

			try {
				if (Level.City [i - 1, j].value == -3)
					neighbourCount++;				
			} catch (IndexOutOfRangeException e) {
			}

			try {
				if (Level.City [i, j + 1].value == -3)
					neighbourCount++;				
			} catch (IndexOutOfRangeException e) {
			}

			try {
				if (Level.City [i + 1, j].value == -3)
					neighbourCount++;				
			} catch (IndexOutOfRangeException e) {
			}

			try {
				if (Level.City [i, j - 1].value == -3)
					neighbourCount++;				
			} catch (IndexOutOfRangeException e) {
			}

			if (neighbourCount < 4) {

				// kontrol etme kısmı

				// ALAN SıNıRLAMASı YAPıLSA Mı???

				foreach (Level.Road r in primaryRoad) {
					if (distance (g.position, r.position) <= minDist) {
						toBeRemoved = true;
						break;
					}
				}
				if (!toBeRemoved)
					foreach (Level.Road r in secondaryRoad) {
						if (distance (g.position, r.position) <= minDist) {
							toBeRemoved = true;
							break;
						}
					}

				if (toBeRemoved)
					toBeRemovedList.Add (g);
			}
		}


		foreach (Level.Road g in toBeRemovedList) {
			greenery.Remove (g);
		}
	}

	#endregion


	#region Combining and Smoothing the Roads

	void CombineRoads ()
	{
		float range = 3.0f;

		foreach (Level.Road sn in secondaryRoad) {

			if (sn.neighbours.Count == 1) {

				float minDist = range + 100.0f;
				Level.Road minNode = null;

				// BU KıSMı HıZLANDıRSAK iYi OLUR. MATRiSE DÖNÜŞ YAPıLABiLiR Mi?
				// YAPıLDı. ÇOK HıZLANDıRMADı; ELDE EDiLEN SONUÇ ORiNALDEN DAHA iYi DEĞiLDi, YOLLAR PEK iYi BAĞLANMADı

				foreach (Level.Road pn in primaryRoad) {
					float dist = distance (sn.position, pn.position);
					if (dist <= range && dist < minDist) {
						minDist = dist;
						minNode = pn;
					}
				}

				if (minNode != null) {
					ConnectNodes (sn, minNode);
				}
			}
		}
	}


	void BreakLongPrimaryLinks ()
	{
		float threshold = 1.5f;
		List<Level.Road> newNodeList = new List<Level.Road> ();

		foreach (Level.Road node in primaryRoad) {
			
			if (node.neighbours.Count == 2) {
				
				Level.Road neighbour_0 = node.neighbours [0];
				Level.Road neighbour_1 = node.neighbours [1];

				if ((node.position - neighbour_0.position).magnitude > threshold) {
					
					Level.Road newNode = new Level.Road ();
					newNode.id = ++ID;
					newNode.value = -1;
					newNode.position = new Vector3 ((node.position.x + neighbour_0.position.x) / 2, 0.0f, (node.position.z + neighbour_0.position.z) / 2);

					RemoveConnection (node, neighbour_0);
					ConnectNodes (newNode, node);
					ConnectNodes (newNode, neighbour_0);

					newNodeList.Add (newNode);
				}

				if ((node.position - neighbour_1.position).magnitude > threshold) {

					Level.Road newNode = new Level.Road ();
					newNode.id = ++ID;
					newNode.value = -1;
					newNode.position = new Vector3 ((node.position.x + neighbour_1.position.x) / 2, 0.0f, (node.position.z + neighbour_1.position.z) / 2);

					RemoveConnection (node, neighbour_1);
					ConnectNodes (newNode, node);
					ConnectNodes (newNode, neighbour_1);

					newNodeList.Add (newNode);
				}
			}
		}

		primaryRoad.AddRange (newNodeList);
	}


	void SmoothingPrimary ()
	{
		float xSum, zSum;

		foreach (Level.Road node in primaryRoad) {

			if (node.neighbours.Count == 2) {
				xSum = 0.0f;
				zSum = 0.0f;

				foreach (Level.Road neighbour in node.neighbours) {
					xSum += neighbour.position.x;
					zSum += neighbour.position.z;
				}

				node.position.x = xSum / node.neighbours.Count;
				node.position.y = 0.0f;
				node.position.z = zSum / node.neighbours.Count;
			}
		}

		BreakLongPrimaryLinks ();
	}


	void BreakLongSecondaryLinks ()
	{
		float threshold = 1.5f;
		List<Level.Road> newNodeList = new List<Level.Road> ();

		foreach (Level.Road node in secondaryRoad) {

			if (node.neighbours.Count == 2) {

				Level.Road neighbour_0 = node.neighbours [0];
				Level.Road neighbour_1 = node.neighbours [1];

				if ((node.position - neighbour_0.position).magnitude > threshold) {

					Level.Road newNode = new Level.Road ();
					newNode.id = ++ID;
					newNode.value = -2;
					newNode.position = new Vector3 ((node.position.x + neighbour_0.position.x) / 2, 0.0f, (node.position.z + neighbour_0.position.z) / 2);

					RemoveConnection (node, neighbour_0);
					ConnectNodes (newNode, node);
					ConnectNodes (newNode, neighbour_0);

					newNodeList.Add (newNode);
				}

				if ((node.position - neighbour_1.position).magnitude > threshold) {

					Level.Road newNode = new Level.Road ();
					newNode.id = ++ID;
					newNode.value = -2;
					newNode.position = new Vector3 ((node.position.x + neighbour_1.position.x) / 2, 0.0f, (node.position.z + neighbour_1.position.z) / 2);

					RemoveConnection (node, neighbour_1);
					ConnectNodes (newNode, node);
					ConnectNodes (newNode, neighbour_1);

					newNodeList.Add (newNode);
				}
			}
		}

		secondaryRoad.AddRange (newNodeList);
	}


	void SmoothingSecondary ()
	{
		float xSum, zSum;

		foreach (Level.Road node in secondaryRoad) {

			if (node.neighbours.Count >= 2) {
				xSum = 0.0f;
				zSum = 0.0f;

				foreach (Level.Road neighbour in node.neighbours) {
					xSum += neighbour.position.x;
					zSum += neighbour.position.z;
				}

				node.position.x = xSum / node.neighbours.Count;
				node.position.y = 0.0f;
				node.position.z = zSum / node.neighbours.Count;
			}
		}

		BreakLongSecondaryLinks ();
	}

	#endregion


	#region Graphical Representation

	void PlaceNodes ()
	{
		float size = 0.3f;

		foreach (Level.Road node in primaryRoad) {
			GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
			cube.name = node.id.ToString ();
			cube.tag = "cube";
			cube.transform.position = node.position;
			cube.transform.localScale = new Vector3 (size, size, size);
			cube.GetComponent<Renderer> ().material.color = Color.blue;
		}
		foreach (Level.Road node in secondaryRoad) {
			GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
			cube.name = node.id.ToString ();
			cube.tag = "cube";
			cube.transform.position = node.position;
			cube.transform.localScale = new Vector3 (size, size, size);
			cube.GetComponent<Renderer> ().material.color = Color.white;				
		}
		foreach (Level.Road node in greenery) {
			GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
			cube.name = node.id.ToString ();
			cube.tag = "cube";
			cube.transform.position = node.position;
			cube.transform.localScale = new Vector3 (size, size, size);
			cube.GetComponent<Renderer> ().material.color = Color.green;
		}
	}



	void PlaceEdges ()
	{
		foreach (Level.Edge e in edgeList) {

			GameObject go = new GameObject ();
			go.tag = "cube";
			LineRenderer lr = go.AddComponent<LineRenderer> ();
			lr.startWidth = 0.1f;
			lr.endWidth = 0.1f;
			lr.SetPosition (0, e.v1.position);
			lr.SetPosition (1, e.v2.position);

		}
	}

	#endregion







	#endregion




}
