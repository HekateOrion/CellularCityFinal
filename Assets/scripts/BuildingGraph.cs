using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class BuildingGraph : MonoBehaviour
{
	
	int M, N;
	List<Level.Plot> plotList = Level.plotList;



	class IndexCouple
	{
		public int bas;
		public int son;

		public IndexCouple ()
		{
		}

		public IndexCouple (int bas, int son)
		{
			this.bas = bas;
			this.son = son;
		}
	}

	class Rectangle
	{
		public IndexCouple horizontal;
		public IndexCouple vertical;
		public int cellCount;

		public Rectangle (IndexCouple horizontal, IndexCouple vertical)
		{
			this.horizontal = horizontal;
			this.vertical = vertical;
		}

		public void FindCellCount ()
		{
			cellCount = (horizontal.son - horizontal.bas + 1) * (vertical.son - vertical.bas + 1);
		}
	}


	class IndexRepeat
	{
		public int currentIndex;
		public int startingIndex;
		public int repeat;

		public IndexRepeat (int currentIndex, int repeat)
		{
			this.currentIndex = currentIndex;
			this.startingIndex = -1;
			this.repeat = repeat;
		}
	}


	
	public BuildingGraph ()
	{
		M = Level.City.GetLength (0);
		N = Level.City.GetLength (1);

	}



	public void CreateGraph ()
	{
		ConvertRoadCellsToBuilding ();

		RemoveRoadBlockingBuildings ();

		CreatePlots ();

		CreateBuildings ();
	}




	#region Utility Methods

	float distance (Vector3 v1, Vector3 v2)
	{
		return (float)Mathf.Sqrt ((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
	}


	void floodFill (int row, int column, Level.Plot plot)
	{        
		if (Level.City [row, column].value == 1 && Level.City [row, column].id == 0) {
			
			Level.City [row, column].id = plot.id;
			Level.City [row, column].correspondingNode = plot;

			Level.CellCoordinates cc = new Level.CellCoordinates (row, column);
			plot.cells.Add (cc);

			try {
				floodFill (row - 1, column, plot);
			} catch (IndexOutOfRangeException exception) {
			}
			try {
				floodFill (row + 1, column, plot);
			} catch (IndexOutOfRangeException exception) {
			}
			try {
				floodFill (row, column - 1, plot);
			} catch (IndexOutOfRangeException exception) {
			}
			try {
				floodFill (row, column + 1, plot);
			} catch (IndexOutOfRangeException exception) {
			}
		}        
	}


	void Divide (Level.Building building, int threshold, List<Level.Building> newBuildingList)
	{
		float i_dist = building.bBox.i_max - building.bBox.i_min;
		float j_dist = building.bBox.j_max - building.bBox.j_min;

		if (i_dist == 0 && j_dist == 0)
			return;

		if (building.cells.Count <= threshold)
			return;



		Level.Building newBuilding = new Level.Building (building.plot);
		newBuildingList.Add (newBuilding);

		if (i_dist > j_dist) {
			foreach (Level.CellCoordinates cc in building.cells) {	// horizontal iKiYE BÖLÜYORUZ
				if (cc.i > building.bBox.i_min + (i_dist / 2)) {
					newBuilding.cells.Add (cc);
					Level.City [cc.i, cc.j].id = newBuilding.id;
					//Level.City [cc.i, cc.j].correspondingNode = newBuilding;
				}
			}
		} else {
			foreach (Level.CellCoordinates cc in building.cells) {	// vertical iKiYE BÖLÜYORUZ
				if (cc.j > building.bBox.j_min + (j_dist / 2)) {
					newBuilding.cells.Add (cc);
					Level.City [cc.i, cc.j].id = newBuilding.id;
					//Level.City [cc.i, cc.j].correspondingNode = newBuilding;
				}
			}
		}

		foreach (Level.CellCoordinates ncc in newBuilding.cells) {
			building.cells.Remove (ncc);
		}

		newBuilding.FindBoundingBox ();
		building.FindBoundingBox ();
	}


	void FindPosition (Level.Building building)
	{
		float x_sum = 0.0f, z_sum = 0.0f;

		foreach (Level.CellCoordinates cc in building.cells) {
			x_sum += cc.i;
			z_sum += cc.j;
		}
		building.position.x = x_sum / building.cells.Count;
		building.position.y = 0.0f;
		building.position.z = z_sum / building.cells.Count;
	}


	void Normalize (Level.Building building)
	{
		int ID = building.id;
		List<Level.CellCoordinates> squareNeighbours = new List<Level.CellCoordinates> ();



		// KARE KOMŞULUKLAR BULUNUYOR

		for (int i = building.bBox.i_min; i <= building.bBox.i_max; i++) {
			for (int j = building.bBox.j_min; j <= building.bBox.j_max; j++) {

				if (Level.City [i, j].value == 1 && Level.City [i, j].id == ID) {
					try {
						if ((Level.City [i, j + 1].value == 1 && Level.City [i, j + 1].id == ID) && (Level.City [i + 1, j + 1].value == 1 && Level.City [i + 1, j + 1].id == ID) && (Level.City [i + 1, j].value == 1 && Level.City [i + 1, j].id == ID)) {

							squareNeighbours.Add (new Level.CellCoordinates (i, j));
							squareNeighbours.Add (new Level.CellCoordinates (i, j + 1));
							squareNeighbours.Add (new Level.CellCoordinates (i + 1, j + 1));
							squareNeighbours.Add (new Level.CellCoordinates (i + 1, j));
						}
					} catch (IndexOutOfRangeException e) {
					}
				}
			}
		}



		// KARE KOMŞULUK YOKSA...

		if (squareNeighbours.Count == 0) {
			NoRectangle (building);
			return;
		}



		// SADECE KARE KOMŞULUKLARDAN OLUŞAN YENi BiRHÜCRE LiSTESi OLUŞTURULUYOR

		List<Level.CellCoordinates> newCCList = new List<Level.CellCoordinates> ();

		foreach (Level.CellCoordinates cc in squareNeighbours) {
			if (!newCCList.Exists (item => (item.i == cc.i && item.j == cc.j)))
				newCCList.Add (cc);
		}



		// FAZLALıK HÜCRELERi MATRiSTEN DE KıRPıYORUZ

		foreach (Level.CellCoordinates cc in building.cells) {
			if (!newCCList.Exists (item => (item.i == cc.i && item.j == cc.j))) {
				Level.City [cc.i, cc.j].id = -1;
			}
		}



		building.cells = newCCList;
		building.FindBoundingBox ();



		// EĞER ŞEKiL YiNE DÜZGÜN DÖRTGEN DEĞiLSE...

		if (building.cells.Count < (building.bBox.i_max - building.bBox.i_min + 1) * (building.bBox.j_max - building.bBox.j_min + 1)) {  // DÜZGÜN DÖRTGEN DEĞiL

			MaxRectangle (building);
		}

	}


	void MaxRectangle (Level.Building building)
	{
		int ID = building.id;

		List<IndexCouple> verticalIndexCoupleList = new List<IndexCouple> ();
		List<IndexCouple> horizontalIndexCoupleList = new List<IndexCouple> ();


		// DiKEY iNDiS iÇiN iLK VE SON ÇiFTLERiNi BUL

		for (int i = building.bBox.i_min; i <= building.bBox.i_max; i++) {

			IndexCouple vic = new IndexCouple (-1, -1);

			for (int j = building.bBox.j_min; j <= building.bBox.j_max; j++) {

				if (vic.bas == -1 && (Level.City [i, j].value == 1 && Level.City [i, j].id == ID)) {
					vic.bas = j;
					continue;
				}
				if (vic.bas != -1 && (Level.City [i, j].value != 1 || (Level.City [i, j].value == 1 && Level.City [i, j].id != ID))) {
					vic.son = j - 1;
					verticalIndexCoupleList.Add (vic);
					vic = new IndexCouple (-1, -1);
				}
			}

			if (vic.bas != -1 && vic.son == -1) {
				vic.son = building.bBox.j_max;
				verticalIndexCoupleList.Add (vic);
			}
		}



		// YATAY iNDiS iÇiN iLK VE SON ÇiFTLERiNi BUL

		for (int j = building.bBox.j_min; j <= building.bBox.j_max; j++) {

			IndexCouple hic = new IndexCouple (-1, -1);

			for (int i = building.bBox.i_min; i <= building.bBox.i_max; i++) {

				if (hic.bas == -1 && (Level.City [i, j].value == 1 && Level.City [i, j].id == ID)) {
					hic.bas = i;
					continue;
				}
				if (hic.bas != -1 && (Level.City [i, j].value != 1 || (Level.City [i, j].value == 1 && Level.City [i, j].id != ID))) {
					hic.son = i - 1;
					horizontalIndexCoupleList.Add (hic);
					hic = new IndexCouple (-1, -1);
				}
			}

			if (hic.bas != -1 && hic.son == -1) {
				hic.son = building.bBox.i_max;
				horizontalIndexCoupleList.Add (hic);
			}
		}




		// HER iKi LiSTEDE DE TEKRARLAYAN ELEMANLARı KALDıR

		List<IndexCouple> newICList = new List<IndexCouple> ();
		foreach (IndexCouple ic in verticalIndexCoupleList) {
			if (!newICList.Exists (item => (item.bas == ic.bas && item.son == ic.son)))
				newICList.Add (ic);
		}
		verticalIndexCoupleList = newICList;

		newICList = new List<IndexCouple> ();
		foreach (IndexCouple ic in horizontalIndexCoupleList) {
			if (!newICList.Exists (item => (item.bas == ic.bas && item.son == ic.son)))
				newICList.Add (ic);
		}
		horizontalIndexCoupleList = newICList;



		// çaprazla. çaprazlanan elemanları teker teker gez, tüm hücreler bina hücresi mi diye bak

		List<Rectangle> rectangleList = new List<Rectangle> ();


		foreach (IndexCouple hic in horizontalIndexCoupleList) {
			foreach (IndexCouple vic in verticalIndexCoupleList) {

				bool candidate = true;

				for (int i = hic.bas; i <= hic.son; i++) {
					for (int j = vic.bas; j <= vic.son; j++) {
						if (!(Level.City [i, j].value == 1 && Level.City [i, j].id == ID)) {
							candidate = false;
							break;
						}
					}
				}

				if (candidate) {
					Rectangle r = new Rectangle (hic, vic);
					r.FindCellCount ();
					rectangleList.Add (r);
				}
			}
		}



		// max hücre sayısı olanı bul

		Rectangle max = rectangleList [0];

		foreach (Rectangle r in rectangleList) {
			if (r.cellCount > max.cellCount)
				max = r;
		}



		// final sınırlarını belirle

		List<Level.CellCoordinates> newCCList = new List<Level.CellCoordinates> ();

		for (int i = max.horizontal.bas; i <= max.horizontal.son; i++) {
			for (int j = max.vertical.bas; j <= max.vertical.son; j++) {
				newCCList.Add (new Level.CellCoordinates (i, j));
			}
		}

		building.cells = newCCList;
		building.FindBoundingBox ();
	}


	void NoRectangle (Level.Building building)
	{
		// en uzun zinciri bul

		int ID = building.id;
		List<IndexRepeat> horizontalIndexRepeat = new List<IndexRepeat> ();
		List<IndexRepeat> verticalIndexRepeat = new List<IndexRepeat> ();


		for (int i = building.bBox.i_min; i <= building.bBox.i_max; i++) {

			IndexRepeat ir = new IndexRepeat (i, 0);
			horizontalIndexRepeat.Add (ir);

			for (int j = building.bBox.j_min; j <= building.bBox.j_max; j++) {
				
				if (Level.City [i, j].value == 1 && Level.City [i, j].id == ID) {

					if (ir.startingIndex == -1)
						ir.startingIndex = j;
					
					ir.repeat++;

				} else if (ir.repeat > 0) {
					ir = new IndexRepeat (i, 0);
					horizontalIndexRepeat.Add (ir);
				}
			}
		}


		for (int j = building.bBox.j_min; j <= building.bBox.j_max; j++) {

			IndexRepeat ir = new IndexRepeat (j, 0);
			verticalIndexRepeat.Add (ir);

			for (int i = building.bBox.i_min; i <= building.bBox.i_max; i++) {

				if (Level.City [i, j].value == 1 && Level.City [i, j].id == ID) {

					if (ir.startingIndex == -1)
						ir.startingIndex = i;
					
					ir.repeat++;

				} else if (ir.repeat > 0) {
					ir = new IndexRepeat (j, 0);
					verticalIndexRepeat.Add (ir);
				}

			}
		}


		IndexRepeat horizontalMax = horizontalIndexRepeat [0];
		foreach (IndexRepeat ir in horizontalIndexRepeat) {
			if (ir.repeat > horizontalMax.repeat)
				horizontalMax = ir;
		}

		IndexRepeat verticalMax = verticalIndexRepeat [0];
		foreach (IndexRepeat ir in verticalIndexRepeat) {
			if (ir.repeat > verticalMax.repeat)
				verticalMax = ir;
		}


		List<Level.CellCoordinates> newCCList = new List<Level.CellCoordinates> ();

		if (horizontalMax.repeat >= verticalMax.repeat) {

			for (int j = horizontalMax.startingIndex; j < horizontalMax.startingIndex + horizontalMax.repeat; j++) {
				newCCList.Add (new Level.CellCoordinates (horizontalMax.currentIndex, j));
			}

		} else {

			for (int i = verticalMax.startingIndex; i < verticalMax.startingIndex + verticalMax.repeat; i++) {
				newCCList.Add (new Level.CellCoordinates (i, verticalMax.currentIndex));
			}
		}

		building.cells = newCCList;
		building.FindBoundingBox ();

	}


	#endregion




	void ConvertRoadCellsToBuilding ()
	{		
		for (int i = 1; i < M - 1; i++) {
			for (int j = 1; j < N - 1; j++) {
				
				if (Level.City [i, j].value == -1 && Level.City [i, j].correspondingNode == null) {
					Level.City [i, j].value = 1;
				}
			}
		}
	}



	void RemoveRoadBlockingBuildings ()
	{
		float minDist_primary = (0.5f + Mathf.Sqrt (2)) / 2;
		float minDist_secondary = (0.25f + Mathf.Sqrt (2)) / 2;

		
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				
				if (Level.City [i, j].value == 1) {

					foreach (Level.Road n in Level.primaryRoad) {						
						if (distance (new Vector3 (i, 0.0f, j), n.position) <= minDist_primary) {
							Level.City [i, j].value = 0;
							break;
						}
					}
					foreach (Level.Road n in Level.secondaryRoad) {						
						if (distance (new Vector3 (i, 0.0f, j), n.position) <= minDist_secondary) {
							Level.City [i, j].value = 0;
							break;
						}
					}
				}
			}
		}
	}



	void CreatePlots ()
	{
		// FLOOD FiLL AŞAMASı : tüm hücreler işaretleniyor, node'lar oluşturuluyor

		for (int i = 0; i < M; i++) {

			for (int j = 0; j < N; j++) {   
			             
				if (Level.City [i, j].value == 1 && Level.City [i, j].id == 0) {

					Level.Plot plot = new Level.Plot ();
					plotList.Add (plot);
					floodFill (i, j, plot);
					plot.FindThreshold ();
				}                
			}
		}
	}



	void CreateBuildings ()
	{
		foreach (Level.Plot plot in plotList) {

			Level.Building initBuilding = new Level.Building (plot);
			initBuilding.id = plot.id;
			plot.buildings.Add (initBuilding);
			initBuilding.cells = plot.cells;
			initBuilding.bBox = plot.bBox;


			if (initBuilding.cells.Count > plot.threshold) { // ALAN THRESHOLD DAN BÜYÜKSE BÖL

				List<Level.Building> newBuildingList;

				do {
					newBuildingList = new List<Level.Building> ();
					foreach (Level.Building building in plot.buildings) {
						Divide (building, plot.threshold, newBuildingList);
					}
					plot.buildings.AddRange (newBuildingList);

				} while(newBuildingList.Count > 0);
			}



			foreach (Level.Building building in plot.buildings) {
				
				if (building.cells.Count < (building.bBox.i_max - building.bBox.i_min + 1) * (building.bBox.j_max - building.bBox.j_min + 1))  // DÜZGÜN DÖRTGEN DEĞiLSE
					Normalize (building);
				
				FindPosition (building);
			}

		}

	}
		



}
