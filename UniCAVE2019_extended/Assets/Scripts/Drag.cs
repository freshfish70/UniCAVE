using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drag : MonoBehaviour
{
	private Vector3 screenPoint;
	private Vector3 offset;

	private int vertindex;

	private Camera camera;

	void OnMouseDown()
	{
		this.camera = FindObjectOfType<Camera>(); ;
		screenPoint = this.camera.WorldToScreenPoint(transform.position);
		offset = transform.position - this.camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
	}

	void OnMouseDrag()
	{
		Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
		Vector3 curPosition = this.camera.ScreenToWorldPoint(curScreenPoint) + offset;
		transform.position = curPosition;
		// this.meshStudy.PullOneVertex(vertindex, this.transform.localPosition);
	}
	// public void setReference(MeshStudy me, int verticee)
	// {
	// 	this.meshStudy = me;
	// 	this.vertindex = verticee;
	// }

	// // Pulling only one vertex pt, results in broken mesh.
	// public void PullOneVertex(int index, Vector3 newPos)
	// {
	// 	Vector3[] verts = this.warpMeshFilter.sharedMesh.vertices;
	// 	var pos = verts[index];
	// 	verts[index] = new Vector3(newPos.x, newPos.y, pos.z);
	// 	this.warpMesh.vertices = verts;
	// 	this.warpMesh.RecalculateNormals();
	// }
}
