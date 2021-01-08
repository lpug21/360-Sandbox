﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class GridSquish : MonoBehaviour
{

	public MeshFilter _0, _250, _500, _750, _1000, _1250;
	Texture2D tex;
	public RawImage img;

	GameObject femurGroup;

	void Start()
    {
		tex = new Texture2D(320, 180);
		femurGroup = GameObject.Find("FemurGroup");
		
    }

	private void Update() {
		//Reset();
		if (Input.GetKeyDown(KeyCode.Space)) {
			DefineScreenGrid();
		}
		
	}

	List<ScreenGridRow> screenGridRows = new List<ScreenGridRow>();
	int numSlices = 10;

	private void DefineScreenGrid() {

		screenGridRows.Clear();

		float x = tex.width;
		float y = tex.height;

		for (int i = 1; i < x + 1; i++) {
			for (int j = 1; j < y + 1; j++) {
				//Mid points for each grid
				Vector3 midPoint = Camera.main.ViewportToWorldPoint(new Vector3(i / x, j / y, 0));

				//if midpoint x/y is outside of bounds?  
				if(Physics.Raycast(new Ray(midPoint, transform.forward))) {

					ScreenGridRow s = new ScreenGridRow();
					s.MidPoint = midPoint;
					s.Testpoints = new Vector3[numSlices];
					s.pixelIndex = ConvertCoordsToIndex(i, j);

					//define test points?
					for (int q = 0; q < numSlices; q++) {
						s.Testpoints[q] = midPoint + new Vector3(0, 0, q + 1 * (10*(10/numSlices)));
					}
					screenGridRows.Add(s);
				}

			}
		}

		//Debug - define grid planes
		//for (int i = 0; i < 10; i++) {
		//	Vector3 v = Camera.main.transform.position + new Vector3(0, 0, i + 1 * 10);
		//	GameObject g = GameObject.CreatePrimitive(PrimitiveType.Plane);
		//	g.transform.position = v;
		//	g.transform.eulerAngles = new Vector3(-90, 0, 0);
		//}

		TestScreenGridPoints();
	}

	private void TestScreenGridPoints() {

		//Assign each point an associated row										
		Dictionary<Vector3, ScreenGridRow> pointGridLU = new Dictionary<Vector3, ScreenGridRow>(); //can use vector3 to LU associated row
		List<Vector3> gridPointList = new List<Vector3>();
		
		foreach(ScreenGridRow s in screenGridRows) {
			foreach(Vector3 v in s.Testpoints) {
				pointGridLU.Add(v, s);
				gridPointList.Add(v);
			}
		}
		Vector3[] originalGridPoints = gridPointList.ToArray();

		//Construct array of test points for NN.  - each Vector3 has it's own ScreenGridRow
		int i = 0;
		Vector3[] points = new Vector3[pointGridLU.Count];
		foreach(KeyValuePair<Vector3, ScreenGridRow> pair in pointGridLU) {
			points[i] = pair.Key;
			i++;
		}

		//Construct array of originalMesh points
		#region MeshPointArrays
		MeshPointArray m0 = new MeshPointArray();
		m0.Points = _0.mesh.vertices;
		m0.Density = 0;
		m0.gameobj = _0.gameObject;

		MeshPointArray m250 = new MeshPointArray();
		m250.Points = _250.mesh.vertices;
		m250.Density = 250;
		m250.gameobj = _250.gameObject;


		MeshPointArray m500 = new MeshPointArray();
		m500.Points = _500.mesh.vertices;
		m500.Density = 500;
		m500.gameobj = _500.gameObject;

		MeshPointArray m750 = new MeshPointArray();
		m750.Points = _750.mesh.vertices;
		m750.Density = 750;
		m750.gameobj = _750.gameObject;


		MeshPointArray m1000 = new MeshPointArray();
		m1000.Points = _1000.mesh.vertices;
		m1000.Density = 1000;
		m1000.gameobj = _1000.gameObject;


		MeshPointArray m1250 = new MeshPointArray();
		m1250.Points = _1250.mesh.vertices;
		m1250.Density = 1250;
		m1250.gameobj = _1250.gameObject;


		List<MeshPointArray> allMeshList = new List<MeshPointArray>();
		allMeshList.Add(m0);
		allMeshList.Add(m250);
		allMeshList.Add(m500);
		allMeshList.Add(m750);
		allMeshList.Add(m1000);
		allMeshList.Add(m1250);

		#endregion
		MeshPointArray[] allMeshes = allMeshList.ToArray();

		int pointsLength = (m0.Points.Count() + m250.Points.Count() + m500.Points.Count() + m750.Points.Count() + m1000.Points.Count() + m1250.Points.Count());
		Vector3[] allMeshPoints = new Vector3[pointsLength];
		Dictionary<int, int> intDensities = new Dictionary<int, int>();
		//assign the originalmesh point master array, record index of what density it belongs to.  
		int k = 0;
		foreach (MeshPointArray m in allMeshes) {
			foreach(Vector3 p in m.Points) {
				allMeshPoints[k] = m.gameobj.transform.TransformPoint(p);
				intDensities.Add(k, m.Density);
				k++;
			}
		}


		GetNNAndDist(originalGridPoints, allMeshPoints);

		//to get compared MeshPointArray.density, just need neighbor[q]
		//To get the ScreenGridRow - use the Vector3 to lookup the object, increment the val


		//need to cull irrelevant data - beyond a certain dist thresh.
		//Why?
		for (int q = 0; q < neighbors.Length; q++) {
			//if (dists[q] < 10) {  //relevant data

			int _density = intDensities[neighbors[q]];
			//Debug.Log(_density);

			
			pointGridLU[originalGridPoints[q]].val += _density*_density;
			
			


			//}
		}

		//8000 appears to be max val
		///SCREEN GRID ROWS DON'T EXIST IF IT DOESN'T RAYCAST.  THEREFORE...?
		foreach (ScreenGridRow s in screenGridRows) {
			s.outputColour = Color.Lerp(Color.black, Color.white, (s.val / 8000000));
		}

		SetPixelsOfTex();
	}


	void SetPixelsOfTex() {
		Color[] cols = tex.GetPixels();
		for (int i = 0; i < cols.Length; i++) {
			cols[i] = Color.black;
		}
	

		for (int i = 0; i < screenGridRows.Count; i++) {
			cols[screenGridRows[i].pixelIndex] = screenGridRows[i].outputColour;
		}

		tex.SetPixels(cols);
		tex.Apply();

		img.texture = tex;

	}


	int ConvertCoordsToIndex(int x, int y) {
		return y * tex.width + x;
	}



	int[] neighbors;
	float[] dists;
	public void GetNNAndDist(Vector3[] originalPoints, Vector3[] comparedPoints) {
		NearestNeighborInterface.GetNNsandDist(comparedPoints, originalPoints, out neighbors, out dists);
	}



}

class ScreenGridRow {
	public Vector3 MidPoint;
	public Vector3[] Testpoints;
	public Color outputColour;
	public float val = 0f;
	internal int pixelIndex;
}

class MeshPointArray {
	public GameObject gameobj;
	public Vector3[] Points;
	public int Density;
}
