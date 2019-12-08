using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class RealtimeCalibrator : NetworkBehaviour
{
	public struct CalibrationSelection
	{
		public string machineName;
		public PhysicalDisplayCalibration calibration;
	}

	/// <summary>
	/// Calibration types
	/// </summary>
	private enum CalibrationType
	{
		VERTEX,
		POSITION,
		ROTATION
	}

	private int totalMeshVertices = 0;

	/// <summary>
	/// Holds the current calibration type.
	/// Default is <c>CalibrationType.VERTEX</c>
	/// </summary>
	private CalibrationType calibrationType = CalibrationType.VERTEX;

	/// <summary>
	/// The currently selected display index
	/// </summary>
	public int selectedIndex = 0;

	/// <summary>
	/// The last selected display index
	/// </summary>
	public int lastSelectedIndex = 0;

	/// <summary>
	/// Holds all displays which can be calibrated
	/// </summary>
	public List<CalibrationSelection> allOptions;

	/// <summary>
	/// The selected vertex index
	/// </summary>
	public int vertexIndex = 0;

	/// <summary>
	/// Reference to info display object.
	/// This object is responsible for displaying the current
	/// calibration type.
	/// </summary>
	[SerializeField]
	private InfoDisplay infoDisplay;

	/// <summary>
	/// The instantiated instance of InfoDisplay for the right eye/cam
	/// </summary>
	private InfoDisplay infoDisplayInstance;

	/// <summary>
	/// Verteces selection size. A size of 1 will select only one
	/// vertex. A size of two wil select all vetices around the selected vertex.
	/// </summary>
	private int gridSelectSize = 1;

	/// <summary>
	/// Flag to determine of debug mode is set or not
	/// </summary>
	private bool meshDebugMode = false;

	/// <summary>
	/// Flag to enable/disable vertices display
	/// </summary>
	private bool showVertices = false;

	void Start()
	{
		allOptions = new List<CalibrationSelection>();
		//generate list of options
		List<PhysicalDisplay> displays = gameObject.GetComponent<UCNetwork>().GetAllDisplays();
		foreach (PhysicalDisplay disp in displays)
		{
			PhysicalDisplayCalibration cali = disp.gameObject.GetComponent<PhysicalDisplayCalibration>();
			if (cali != null)
			{
				CalibrationSelection selection;
				selection.machineName = (disp.manager == null) ? disp.machineName : disp.manager.machineName;
				selection.calibration = cali;
				allOptions.Add(selection);
			}
		}
		Debug.Log("RealtimeCalibration: Found " + allOptions.Count + " calibration objects");
		StartCoroutine(InitiateInfoScreen());
	}

	/// <summary>
	/// Instatiate the info screen with a delay.
	/// So we are sure everything has initialized before
	/// setting the info screen
	/// </summary>
	/// <returns></returns>
	private IEnumerator InitiateInfoScreen()
	{
		yield return new WaitForSeconds(3);
		this.CreateInfoDisplay();
		this.InfoDisplayShift(this.selectedIndex);
		this.RpcInfoDisplayShift(this.selectedIndex);

		yield break;
	}

	/// <summary>
	/// Instantiates the info displays.
	/// And set them to disabled at start
	/// </summary>
	private void CreateInfoDisplay()
	{
		if (this.infoDisplay == null) return;
		this.infoDisplayInstance = Instantiate(infoDisplay);
		this.infoDisplayInstance.gameObject.SetActive(false);
	}

	/// <summary>
	/// Moves a vertex by index in a direction (-1, +1 or 0) by a factor delta.
	/// A direction can be Vector2(-1,0); move the display in a negative X direction.
	/// The factor delta sets the "speed/distance" of the movement.
	/// </summary>
	/// <param name="direction">the direction to move in</param>
	/// <param name="delta">movement factor</param>
	/// /// <param name="selectedIndex">the display to move vertex on</param>
	/// <param name="vertexIndex">the vertex to move</param>
	private void LocalShift(Vector2 direction, float delta, int selectedIndex, int vertexIndex)
	{
		PhysicalDisplayCalibration lastCalibration = allOptions[lastSelectedIndex].calibration;
		lastCalibration.HideVisualMarker();
		PhysicalDisplayCalibration calibration = allOptions[selectedIndex].calibration;

		calibration.SetVisualMarkerVertextPoint(vertexIndex);

		Debug.Log("RealtimeCalibration: LocalShift called " + delta + ", " + selectedIndex + ", " + vertexIndex);

		MeshFilter lastWarpedFilter = null;
		foreach (Dewarp dewarp in calibration.GetDisplayWarpsValues())
		{
			MeshFilter meshFilter = dewarp.GetDewarpMeshFilter();
			lastWarpedFilter = meshFilter;
			Vector3[] verts = meshFilter.sharedMesh.vertices;
			Dictionary<int, float> vertsToShift = this.getSurroundingIndices(dewarp.xSize + 1, dewarp.ySize + 1, vertexIndex, this.gridSelectSize);
			foreach (var ind in vertsToShift)
			{
				int key = ind.Key;
				float factor = delta / ind.Value;
				verts[ind.Key] = new Vector3(verts[key].x + (direction.x * factor), verts[key].y + (direction.y * factor), verts[key].z);
			}
			dewarp.UpdateVisualVerticesPosition(verts);
			meshFilter.sharedMesh.vertices = verts;
			meshFilter.sharedMesh.UploadMeshData(false);
			meshFilter.mesh.RecalculateBounds();
			meshFilter.mesh.RecalculateTangents();
		}
		calibration.UpdateMeshPositions(lastWarpedFilter?.sharedMesh.vertices);
	}


	/// <summary>
	/// Move the position of a display in a given direction (-1, +1 or 0) by a factor delta.
	/// A direction can be Vector3(-1,0,0) ; move the display in a negative X direction.
	/// The factor delta sets the "speed/distance" of the movement.
	/// </summary>
	/// <param name="direction">the direction to move in</param>
	/// <param name="delta">movement factor</param>
	/// <param name="selectedIndex">display index of the display to move</param>
	private void LocalPositionShift(Vector3 direction, float delta, int selectedIndex)
	{
		PhysicalDisplayCalibration lastCalibration = allOptions[lastSelectedIndex].calibration;
		lastCalibration.HideVisualMarker();
		PhysicalDisplayCalibration calibration = allOptions[selectedIndex].calibration;
		calibration.SetVisualMarkerVertextPoint(vertexIndex);

		calibration.MoveDisplay(new Vector3(direction.x * delta, direction.y * delta, direction.z * delta));
	}

	/// <summary>
	/// Rotates a display in a around a given axis (-1, +1 or 0) by a factor delta.
	/// A roation can be Vector3(-1,0,0) ; roate the display around X axis in negative direction.
	/// The factor delta sets the "speed" of the rotation.
	/// </summary>
	/// <param name="direction">the axis to rotate around</param>
	/// <param name="delta">rotation factor</param>
	/// <param name="selectedIndex">display index of the display to rotate</param>
	private void LocalRotationShift(Vector3 direction, float delta, int selectedIndex)
	{
		PhysicalDisplayCalibration lastCalibration = allOptions[lastSelectedIndex].calibration;
		lastCalibration.HideVisualMarker();
		PhysicalDisplayCalibration calibration = allOptions[selectedIndex].calibration;
		calibration.SetVisualMarkerVertextPoint(vertexIndex);
		calibration.RotateDisplay(new Vector3(direction.x * delta, direction.y * delta, direction.z * delta));
	}

	/// <summary>
	/// Sets the selection size on local machine
	/// </summary>
	/// <param name="selectSize">size of the select</param>
	private void LocalSetGridSelectSize(int selectSize)
	{
		this.gridSelectSize = selectSize;
		}

	/// <summary>
	/// Shifts the info window to the display on the given index
	/// </summary>
	/// <param name="selectedIndex">index of the display to set display on</param>
	private void InfoDisplayShift(int selectedIndex)
	{
		this.selectedIndex = selectedIndex;
		PhysicalDisplayCalibration currentDisplay = this.allOptions[selectedIndex].calibration;
		if (currentDisplay == null || this.infoDisplayInstance == null) return;
		if (currentDisplay.GetDisplayWarpsValues().Count() > 0)
		{
			this.SetInfoDisplay(infoDisplayInstance.gameObject, currentDisplay.GetDisplayWarpsValues().First().GetDewarpGameObject().transform);
			this.infoDisplayInstance.SetText(this.calibrationType.ToString());
		}
	}

	/// <summary>
	/// Activates the info display, sets it parent to provided transform, 
	/// and resets its local position so it is centered to the parent.
	/// </summary>
	/// <param name="infoDisplay"></param>
	/// <param name="parent"></param>
	private void SetInfoDisplay(GameObject infoDisplay, Transform parent)
	{
		infoDisplay.gameObject.SetActive(true);
		infoDisplay.transform.SetParent(parent);
		infoDisplay.transform.localPosition = new Vector2(0, 0);
	}

	/// <summary>
	/// Finds surrounding verticees positions from a X*Y sized array given the 
	/// index of a vertice. Selection is based on a square, if the selection size is
	/// 2 it will select all indexes which is upto two positions away in a square
	/// from the provided index.
	/// Created by Sander @ https://github.com/sanderhurlen
	/// </summary>
	/// <param name="x">the x size</param>
	/// <param name="y">the y size</param>
	/// <param name="index">the index to get surrounding verticees from</param>
	/// <param name="selectSize">the size of the selection</param>
	/// <returns>Vertice index and position offset from selected idnex</returns>
	public Dictionary<int, float> getSurroundingIndices(int x, int y, int index, int selectSize)
	{
		// Holds the vertices that we want to return and move
		Dictionary<int, float> vertecesToMove = new Dictionary<int, float>();
		// mesh size X and Y
		int sizeX = x;
		int sizeY = y;

		int indexSizeX = x + 1;
		int indexSizeY = y + 1;

		// The row the selected index is on
		int indexRow = (int)Math.Floor((decimal)(index / sizeX));

		// The column the selected index is on
		int indexColumn = (index % sizeX);
		// The start row to select from
		int startRow = indexRow - selectSize >= 0 ? indexRow - selectSize : 0;
		// The row to end selection on
		int endRow = indexRow + selectSize < sizeY ? indexRow + selectSize : sizeY - 1;

		// Column where to start selection
		int startColumn = (indexColumn - selectSize) >= 0 ? (indexColumn - selectSize) : 0;
		// The column where we end selection
		int endColumn = indexColumn + selectSize < sizeX ? indexColumn + selectSize : sizeX - 1;

		// Weighting/distance away factor for vertices
		int[] weightings = new int[sizeX + 1];

		for (int i = 0; i <= sizeX; i++)
		{
			int value = i - (index % sizeX);
			if (value >= 0)
			{
				weightings[i] = (1 + value);
			}
			else
			{
				weightings[i] = 1 + (value * -1);
			}
		}

		int rowDiff;
		for (int position = startRow; position <= indexRow; position++)
		{
			rowDiff = indexRow - position;
			for (int vertex = startColumn; vertex <= endColumn; vertex++)
			{
				if (position != indexRow)
				{
					vertecesToMove.Add(vertex + (sizeX * position), (weightings[vertex] + rowDiff));
				}
				else
				{
					vertecesToMove.Add(vertex + (sizeX * position), weightings[vertex]);
				}
			}
		}

		for (int position = indexRow + 1; position <= endRow; position++)
		{
			rowDiff = position - indexRow;
			for (int vertex = startColumn; vertex <= endColumn; vertex++)
			{
				vertecesToMove.Add(vertex + (sizeX * position), (weightings[vertex] + rowDiff));
			}
		}
		return vertecesToMove;
	}



	/// <summary>
	/// Cycles to the next calibration type, if it reaches the end
	/// start over.
	/// </summary>
	/// <returns>type of calibration :(pos, rot, vertex)</returns>
	private CalibrationType CycleNextCalibrationType()
	{
		this.calibrationType = (from CalibrationType val in Enum.GetValues(typeof(CalibrationType)) where val > this.calibrationType orderby val select val).DefaultIfEmpty().First();
		this.RpcSetCalibrationType(this.calibrationType);
		this.infoDisplayInstance.SetText(this.calibrationType.ToString());
		return this.calibrationType;
	}

	/// <summary>
	/// Sets the selection size on local machine and RPC call
	/// </summary>
	/// <param name="selectSize">size of the select</param>
	public void SetGridSelectSize(int selectSize)
	{
		this.LocalSetGridSelectSize(selectSize);
		this.RcpSetGridSelectSize(selectSize);
	}

	/// <summary>
	/// Triggers local and Rpc vertex shift methods
	/// </summary>
	/// <param name="direction">the direction to move</param>
	/// <param name="delta">the speed/steps of movement</param>
	private void VertexShift(Vector2 direction, float delta)
	{
		LocalShift(direction, delta, selectedIndex, vertexIndex);
		RpcShift(direction, delta, selectedIndex, vertexIndex);
	}

	/// <summary>
	/// Triggers local and Rpc display positions methods
	/// </summary>
	/// <param name="direction">the direction to move</param>
	/// <param name="delta">the speed/steps of movement</param>
	private void PositionShift(Vector3 direction, float delta)
	{
		this.LocalPositionShift(direction, delta, selectedIndex);
		this.RpcMovePosition(direction, delta, selectedIndex);
	}

	/// <summary>
	/// Triggers local and Rpc display rotation methods
	/// </summary>
	/// <param name="direction">the direction of rotation</param>
	/// <param name="delta">the speed/steps of the rotation</param>
	private void RotationShift(Vector3 direction, float delta)
	{
		this.LocalRotationShift(direction, delta, selectedIndex);
		this.RpcRotate(direction, delta, selectedIndex);
	}

	/// <summary>
	/// Triggers local and Rpc info display shift methods
	/// </summary>
	private void DisplayShift()
	{
		this.updateTotalVertices();
		InfoDisplayShift(selectedIndex);
		RpcInfoDisplayShift(selectedIndex);
		this.SetLastIndex(selectedIndex);

	}

	/// <summary>
	/// Triggers Rpc set last display index method and set it on local
	/// <param name="index">the index to set as last index</param>
	private void SetLastIndex(int index)
	{
		this.lastSelectedIndex = index;
		this.RpcSetLastIndex(index);
	}

	/// <summary>
	/// Triggers RCP and Local show verteces
	/// </summary>
	public void ToggleVertice(bool toggle)
	{
		this.LocalToggleVertices(toggle);
		this.RpcToggleVertices(toggle);
	}


	/// <summary>
	/// Shows all verteces on a dewarp mesh for the local instance
	/// </summary>
	private void LocalToggleVertices(bool toggle)
	{
		this.allOptions.ForEach(e =>
		{
			foreach (var item in e.calibration.GetDisplayWarpsValues())
			{
				if (toggle)
				{
					Debug.Log("werjwelkr");
				item.ShowVertices();
			}
				else
				{
					item.HideVertices();
				}
			}
		});
	}

	/// <summary>
	/// Shows all verteces on a dewarp mesh for the local instance
	/// </summary>
	public void ToggleDebug(bool toggle)
	{
		this.LocalToggleDebug(toggle);
		this.RpcToggleDebug(toggle);
	}

	/// <summary>
	/// Shows all verteces on a dewarp mesh for the local instance
	/// </summary>
	private void LocalToggleDebug(bool toggle)
	{
		this.allOptions.ForEach(e =>
		{
			foreach (var item in e.calibration.GetDisplayWarpsValues())
			{
				item.ToogleDebugMode(toggle);
			}
		});
	}

	#region RPCCALLS

	/// <summary>
	/// Client RPC method which triggers local show verteces;
	/// </summary>
	[ClientRpc]
	void RpcToggleDebug(bool toggle)
	{
		this.LocalToggleDebug(toggle);
	}

	/// <summary>
	/// Client RPC method which triggers local show verteces;
	/// </summary>
	[ClientRpc]
	void RpcToggleVertices(bool toggle)
	{
		this.LocalToggleVertices(toggle);
	}

	/// <summary>
	/// Client RPC methods which triggers the local vertex shift movement method.
	/// </summary>
	/// <param name="direction">direction to move in</param>
	/// <param name="delta">movement factor</param>
	/// <param name="selectedIndex">display index</param>
	/// <param name="vertexIndex">vertex index</param>
	[ClientRpc]
	void RpcShift(Vector2 direction, float delta, int selectedIndex, int vertexIndex)
	{
		LocalShift(direction, delta, selectedIndex, vertexIndex);
	}

	/// <summary>
	/// Client RPC methods which triggers the local display movement method.
	/// </summary>
	/// <param name="direction">direction to move in</param>
	/// <param name="delta">movement factor</param>
	/// <param name="selectedIndex">display index</param>
	[ClientRpc]
	void RpcMovePosition(Vector2 direction, float delta, int selectedIndex)
	{
		LocalPositionShift(direction, delta, selectedIndex);
	}
	/// <summary>
	/// Client RPC methods which triggers the local display rotate method.
	/// </summary>
	/// <param name="direction">axis to rotate around and direction</param>
	/// <param name="delta">rotate speed factor</param>
	/// <param name="selectedIndex">display index</param>
	[ClientRpc]
	void RpcRotate(Vector2 direction, float delta, int selectedIndex)
	{
		LocalRotationShift(direction, delta, selectedIndex);
	}

	/// <summary>
	/// Client RPC method which sets the calibration type on each client
	/// </summary>
	/// <param name="calibrationType">calibration type to set</param>
	[ClientRpc]
	void RpcSetCalibrationType(CalibrationType calibrationType)
	{
		this.calibrationType = calibrationType;
		this.infoDisplayInstance.SetText(calibrationType.ToString());
	}

	/// <summary>
	/// Client RPC method which calls <c>InfoDisplayShift</c> and activates
	/// the info display on provided display index
	/// </summary>
	/// <param name="selectedIndex">the selected display index</param>
	[ClientRpc]
	void RpcInfoDisplayShift(int selectedIndex)
	{
		this.InfoDisplayShift(selectedIndex);
	}

	/// <summary>
	/// Sets the selection size on remote machine
	/// </summary>
	/// <param name="selectSize">The size of the select</param>
	[ClientRpc]
	void RcpSetGridSelectSize(int selectSize)
	{
		LocalSetGridSelectSize(selectSize);
	}

	/// <summary>
	/// Client RPC method which sets the last selected display index
	/// </summary>
	/// <param name="index">the index of the last selected display</param>
	[ClientRpc]
	private void RpcSetLastIndex(int index)
	{
		this.lastSelectedIndex = index;
	}

	#endregion

	/// <summary>
	/// Returns the total numbers of vertices for current selected display.
	/// This is Index based so total vertices - 1.
	/// </summary>
	/// <returns>totalnumber of vertices</returns>
	private int getTotalVertices()
	{
		if (this.totalMeshVertices < 1)
		{
			this.updateTotalVertices();
		}
		return this.totalMeshVertices;
	}
	/// <summary>
	/// Updates totalMeshVertices to current selected display
	/// </summary>
	private void updateTotalVertices()
	{
		this.totalMeshVertices = this.allOptions[this.selectedIndex].calibration.GetMeshResolution();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			this.CycleNextCalibrationType();
		}

		if (Input.GetKeyDown(KeyCode.KeypadPlus))
		{
			this.gridSelectSize++;
			this.SetGridSelectSize(this.gridSelectSize);
		}

		if (Input.GetKeyDown(KeyCode.KeypadMinus))
		{
			int currentSelectSize = this.gridSelectSize;
			int newSize = currentSelectSize-- <= 0 ? 0 : currentSelectSize--;
			this.SetGridSelectSize(newSize);
		}

		Vector3 direction = Vector3.zero;
		bool anyPressed = false;
		bool noOptions = allOptions.Count == 0;

		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				int index = this.selectedIndex - 1;
				if (index < 0)
				{
					this.selectedIndex = allOptions.Count - 1;
				}
				else
				{
					this.selectedIndex = Mathf.Abs((selectedIndex - 1) % allOptions.Count);
				}
				if (!noOptions)
				{
					DisplayShift();
					VertexShift(direction, 1f);
				}
			}

		}
		else if (Input.GetKeyDown(KeyCode.Return))
		{
			this.selectedIndex = (selectedIndex + 1) % allOptions.Count;
			if (!noOptions)
			{
				vertexIndex = 0;
				DisplayShift();
				VertexShift(direction, 1f);
			}
		}

		if (Input.GetKeyDown(KeyCode.A))
		{
			int lastIndex = this.vertexIndex - 1;
			this.vertexIndex = (lastIndex < 0) ? this.getTotalVertices() - 1 : lastIndex;
			if (!noOptions)
			{
				this.VertexShift(direction, 1f);
			}

		}

		if (Input.GetKeyDown(KeyCode.D))
		{
			this.vertexIndex++;
			this.vertexIndex = vertexIndex % this.getTotalVertices();
			if (!noOptions)
			{
				this.VertexShift(direction, 1f);
			}
		}

		if (Input.GetKeyDown(KeyCode.V))
		{
			if (!noOptions)
			{
				this.showVertices = !this.showVertices;
				this.ToggleVertice(this.showVertices);
			}
		}

		if (Input.GetKeyDown(KeyCode.Z))
		{
			if (!noOptions)
			{
				this.meshDebugMode = !this.meshDebugMode;
				this.ToggleDebug(this.meshDebugMode);
			}
		}

		if (noOptions) { return; }

		if (Input.GetKey(KeyCode.RightArrow))
		{
			direction.x = 1;
			anyPressed = true;
		}
		else if (Input.GetKey(KeyCode.UpArrow))
		{
			direction.y = 1;
			anyPressed = true;
		}
		else if (Input.GetKey(KeyCode.LeftArrow))
		{
			direction.x = -1;
			anyPressed = true;
		}
		else if (Input.GetKey(KeyCode.DownArrow))
		{
			direction.y = -1;
			anyPressed = true;
		}
		else if (Input.GetKey(KeyCode.Keypad8))
		{
			direction.z = -1;
			anyPressed = true;
		}
		else if (Input.GetKey(KeyCode.Keypad2))
		{
			direction.z = 1;
			anyPressed = true;
		}

		if (anyPressed)
		{
			if (isServer)
			{
				switch (this.calibrationType)
				{
					case CalibrationType.POSITION:
						this.PositionShift(direction, 0.0015f);
						break;
					case CalibrationType.ROTATION:
						this.RotationShift(direction, 0.10f);
						break;
					case CalibrationType.VERTEX:
						this.VertexShift(direction, 0.0015f);
						break;
				}

			}
		}
	}
}