using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class Automata : MonoBehaviour
{


	Level.MatrixCell[,] map;
	Level.MatrixCell[,] temp;
	int M, N;
	int p = 45;


	public Automata ()
	{
		map = Level.City;
		M = Level.City.GetLength (0);
		N = Level.City.GetLength (1);
		Debug.Log ("automata created");
	}



	public void RunAutomata ()
	{

		Debug.Log ("automata start " + DateTime.Now);

		temp = new Level.MatrixCell[M, N];
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				temp [i, j] = new Level.MatrixCell ();
			}
		}

		int difference;
        
        
		#region STAGE 1
        
		Randomize (true, 100, p); 

		int previousDifference = 0;
		int currentDifference = M * N;
		int differenceLimit = (M * N) / 1000;
                
		while (Mathf.Abs (previousDifference - currentDifference) > differenceLimit) {
			Grouping ();
			previousDifference = currentDifference;
			currentDifference = Stopper ();
		}        
        
		do {
			difference = Smoothing ();
		} while(difference > differenceLimit);
                
		MatrixFloodFill ();        
                
		do {
			difference = Growing (2);
		} while(difference != 0);
        
		Preparation (-1);

		#endregion

        
		#region STAGE 2
        
		Randomize (false, 100, p);
        
		previousDifference = 0;
		currentDifference = M * N;
		differenceLimit = (M * N) / 100;
        
		while (Mathf.Abs (previousDifference - currentDifference) > differenceLimit) {
			Grouping ();
			previousDifference = currentDifference;
			currentDifference = Stopper ();            
		}        
        
		do {
			difference = Smoothing ();
		} while(difference > differenceLimit);
        
		MatrixFloodFill ();        
        
		do {
			difference = Growing (2);
		} while(difference != 0);
        
		Preparation (-2);
        
		#endregion

        
		#region STAGE 3
        
		Randomize (false, 100, p);
        
		previousDifference = 0;
		currentDifference = M * N;
		differenceLimit = (M * N) / 10;
		
		while (Mathf.Abs (previousDifference - currentDifference) > differenceLimit) {
			Grouping ();
			previousDifference = currentDifference;
			currentDifference = Stopper ();
		}
        
		do {
			difference = Smoothing ();
		} while(difference > (differenceLimit / 10));    // daha fazla çalışsın diye eşiği düşürdük. büyük olursa bir defa çalışıyor (*10 da denedim)
        
		MatrixFloodFill ();        
        
		do {
			difference = Growing (2);
		} while(difference != 0);
        
		Preparation (-3);
        
		#endregion
        
        
		int lastColor = MatrixFloodFill ();
        
		Rasterize (lastColor);

		AdjustRoadTypes ();

		Debug.Log ("automata done " + DateTime.Now);
	}



	void updateMatrix ()
	{
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				map [i, j].value = temp [i, j].value;
			}
		}
	}

	int updateMatrix_WithDifference ()
	{
		int difference = 0;
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				if (map [i, j].value != temp [i, j].value)
					difference++;
				map [i, j].value = temp [i, j].value;
			}
		}
		return difference;
	}




	int neighbourCount (int i, int j)
	{
		int neighbours = 0;
		for (int k = checkM (i - 1); k <= checkM (i + 1); k++) {
			for (int l = checkN (j - 1); l <= checkN (j + 1); l++) {
				if (i == k && j == l)
					continue;
				if (map [k, l].value == 1)
					neighbours++;
			}
		}
		return neighbours;
	}

	int checkM (int v)
	{
		if (v < 0)
			return 0;
		else if (v >= M)
			return M - 1;
		else
			return v;
	}

	int checkN (int v)
	{
		if (v < 0)
			return 0;
		else if (v >= N)
			return N - 1;
		else
			return v;
	}




	int MatrixFloodFill ()
	{        
		int color = 2;        
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {                
				if (map [i, j].value == 1) {                    
					floodFill (i, j, color);
					color++;
				}                
			}
		}
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				temp [i, j].value = map [i, j].value;
			}
		}
		return color;
	}

	void floodFill (int row, int column, int color)
	{        
		if (map [row, column].value == 1) {
			map [row, column].value = color;
			try {
				floodFill (row - 1, column, color);
			} catch (IndexOutOfRangeException exception) {
			}
			try {
				floodFill (row + 1, column, color);
			} catch (IndexOutOfRangeException exception) {
			}
			try {
				floodFill (row, column - 1, color);
			} catch (IndexOutOfRangeException exception) {
			}
			try {
				floodFill (row, column + 1, color);
			} catch (IndexOutOfRangeException exception) {
			}
		}        
	}

    
    
    
	void Randomize (bool init, int limit, int zeroPossibility)
	{
        
		System.Random rand = new System.Random ();
		
		if (init) {
			for (int i = 0; i < M; i++) {
				for (int j = 0; j < N; j++) {
					int p = rand.Next (limit);
					if (p < zeroPossibility)
						map [i, j].value = 0;
					else
						map [i, j].value = 1;
				}
			}
		} else {
			for (int i = 0; i < M; i++) {
				for (int j = 0; j < N; j++) {
					if (map [i, j].value == 1) {                                      
						int p = rand.Next (limit);
						if (p < zeroPossibility)
							temp [i, j].value = 0;
					}
				}
			}
			updateMatrix ();
		}
	}

    
    
	void Grouping ()
	{        
		int neighbours;        
        
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {  
                
				if (map [i, j].value != 0 && map [i, j].value != 1)
					continue;
                
				neighbours = neighbourCount (i, j);

				if (map [i, j].value == 0) {
					if (neighbours >= 4)
						temp [i, j].value = 1;
					else
						temp [i, j].value = 0;                    
				}
				if (map [i, j].value == 1) {
					if (neighbours >= 5)
						temp [i, j].value = 1;
					else
						temp [i, j].value = 0;
				}
			}
		}                
		updateMatrix ();        
	}

    
    
	int Stopper ()
	{
        
		int numberOfChanges_Vertical = 0, numberOfChanges_Horizontal = 0;
        
		for (int i = 0; i < M; i++) {
            
			int currentValue = -1;
            
			for (int j = 0; j < N; j++) {
                
				if (map [i, j].value != 0 && map [i, j].value != 1)
					continue;                
				if (currentValue == -1) {
					currentValue = map [i, j].value;
					continue;
				}                                       // 0 yada 1 olacak                   
				if (currentValue != map [i, j].value)
					numberOfChanges_Vertical++;
				currentValue = map [i, j].value;
			}
		}
        
		for (int j = 0; j < N; j++) {
            
			int currentValue = -1;
            
			for (int i = 0; i < M; i++) {
                
				if (map [i, j].value != 0 && map [i, j].value != 1)
					continue;                
				if (currentValue == -1) {
					currentValue = map [i, j].value;
					continue;
				}                
				if (currentValue != map [i, j].value)
					numberOfChanges_Horizontal++;
				currentValue = map [i, j].value;
			}
		}
        
		if (numberOfChanges_Horizontal < numberOfChanges_Vertical)
			return numberOfChanges_Vertical;
		return numberOfChanges_Horizontal;
	}

    
    
	int Smoothing ()
	{
        
		int neighbours;        
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
                
				neighbours = neighbourCount (i, j);

				if (map [i, j].value == 0 && neighbours > 4)
					temp [i, j].value = 1; 
				if (map [i, j].value == 1 && neighbours < 4)
					temp [i, j].value = 0;                              
			}
		}                
		return updateMatrix_WithDifference ();
	}

    
    
	int Growing (int distance)
	{
        
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {                
                
				if (map [i, j].value == 0) {
                    
					// iki farklı renge rastlandıysa değişmeyecek. boş kalacak
                    
					int color = 0;
					int direction = 1; // clockwise, kuzeyden başlayarak
					while (color <= 0 && direction < 5) {                        
						try {
							if (direction == 1)
								color = map [i - 1, j].value;    // kuzey
							if (direction == 2)
								color = map [i, j + 1].value;    // doğu
							if (direction == 3)
								color = map [i + 1, j].value;    // güney
							if (direction == 4)
								color = map [i, j - 1].value;    // batı
							direction++;
						} catch (IndexOutOfRangeException exception) {
							direction++;
						}                        
					}
                    
					// boş hücrenin ya komşu olduğu ve boyanacağı rengi bulduk,
					// yada herhangi bir renge komşu olmadığını öğrendik
                    
					if (color > 0) {
                        
						bool ok = true;
                        
						// BAŞKA BİR RENGE RASTLARSA O KARE BOYANMAYACAK
                        
						for (int k = -distance; k <= distance; k++) {
							for (int l = -distance; l <= distance; l++) {
                                
								if (i == k && j == l)
									continue;
								try {
									if (map [i + k, j + l].value > 0 && map [i + k, j + l].value != color) {
										ok = false;
										break;
									}
								} catch (IndexOutOfRangeException exception) {
								}
							}
						}                                                
						if (ok)
							temp [i, j].value = color;
					}                    
				}               
			}
		}
		return updateMatrix_WithDifference ();
	}

    
    
	void Preparation (int roadColor)
	{
                
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				if (map [i, j].value == 0)
					map [i, j].value = roadColor;
				if (map [i, j].value > 0)
					map [i, j].value = 1;
			}
		}
        
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				temp [i, j].value = map [i, j].value;
			}
		}
	}



	void Rasterize (int lastColor)
	{  
		System.Random rand = new System.Random ();
		for (int color = 2; color <= lastColor; color++) {
			RasterizeSubArea (color, rand.Next (2) + 2, rand.Next (2) + 2);
		}
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				if (map [i, j].value > 0)
					map [i, j].value = 1;
			}
		}        
	}

    
	bool isMainRoad (int value)
	{
		if (value == -1 || value == -2 || value == -3)
			return true;
		else
			return false;
	}

    
	void RasterizeSubArea (int color, int horizontalDistance, int verticalDistance)
	{
        
		System.Random rand = new System.Random ();
		bool previousAdjacent = false;
        
		for (int i = 0; i < M; i = i + verticalDistance) {            
			previousAdjacent = false;            
			for (int j = 0; j < N; j++) {                                
				if (map [i, j].value == color) {                    
					try {                        
						if (isMainRoad (map [i - 1, j].value) || isMainRoad (map [i + 1, j].value)) {
							if (previousAdjacent) {
								continue;
							}
							previousAdjacent = true;
							map [i, j].value = -4;
						} else {
							map [i, j].value = -4;
							if (previousAdjacent) {
								map [i, j - 1].value = -4;
								previousAdjacent = false;
							}
						}
					} catch (IndexOutOfRangeException e) {
					}                    
				}                
				if (i < M - 1 && rand.Next (30) == 0)
					i++;
			}
		}
        
		for (int j = 0; j < N; j = j + horizontalDistance) {            
			previousAdjacent = false;            
			for (int i = 0; i < M; i++) {
				if (map [i, j].value == color) {
					try {                        
						if (isMainRoad (map [i, j - 1].value) || isMainRoad (map [i, j + 1].value)) {
							if (previousAdjacent) {
								continue;
							}
							previousAdjacent = true;
							map [i, j].value = -4;
						} else {
							map [i, j].value = -4;
							if (previousAdjacent) {
								map [i - 1, j].value = -4;
								previousAdjacent = false;
							}
						}
					} catch (IndexOutOfRangeException e) {
					}                    
				}                
				if (j < N - 1 && rand.Next (30) == 0)
					j++;
			}
		}        
	}



	void AdjustRoadTypes ()
	{        
		for (int i = 0; i < M; i++) {
			for (int j = 0; j < N; j++) {
				if (map [i, j].value == -2 || map [i, j].value == -3)
					map [i, j].value = -1;
				else if (map [i, j].value == -4)
					map [i, j].value = -2;
			}
		}        
	}


}
