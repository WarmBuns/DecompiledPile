using UnityEngine;

namespace RoR2;

public class DestroyOnUnseen : MonoBehaviour
{
	public bool cull;

	private Renderer rend;

	private void Start()
	{
		rend = GetComponentInChildren<Renderer>();
	}

	private void Update()
	{
		if (cull && (bool)rend && !rend.isVisible)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
