using UnityEngine;

namespace RoR2;

public class CombineMesh : MonoBehaviour
{
	private void Start()
	{
		Renderer component = GetComponent<Renderer>();
		MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>();
		CombineInstance[] array = new CombineInstance[componentsInChildren.Length];
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			array[i].mesh = componentsInChildren[i].sharedMesh;
			array[i].transform = componentsInChildren[i].transform.localToWorldMatrix;
			componentsInChildren[i].gameObject.SetActive(value: false);
		}
		MeshFilter meshFilter = base.gameObject.AddComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		meshFilter.mesh.CombineMeshes(array, mergeSubMeshes: true, useMatrices: true, hasLightmapData: true);
		component.material = componentsInChildren[0].GetComponent<Renderer>().sharedMaterial;
		base.gameObject.SetActive(value: true);
	}
}
