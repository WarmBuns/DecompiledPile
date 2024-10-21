using UnityEngine;

public class RenderInFrontOfParticles : MonoBehaviour
{
	public int renderOrder;

	private Renderer rend;

	private void Start()
	{
		rend = GetComponent<Renderer>();
		rend.material.renderQueue = renderOrder;
	}

	private void Update()
	{
	}
}
