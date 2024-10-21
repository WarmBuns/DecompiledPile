using UnityEngine;

namespace RoR2;

public class AssignRandomMaterial : MonoBehaviour
{
	public Renderer rend;

	public Material[] materials;

	private void Awake()
	{
		rend.material = materials[Random.Range(0, materials.Length)];
	}
}
