using UnityEngine;

public class ScaleLineRenderer : MonoBehaviour
{
	private LineRenderer line;

	public float scaleSize = 1f;

	public Vector3[] positions;

	private void Start()
	{
		line = GetComponent<LineRenderer>();
		SetScale();
	}

	private void Update()
	{
	}

	private void SetScale()
	{
		line.SetPosition(0, positions[0]);
		line.SetPosition(1, positions[1]);
		line.material.SetTextureScale("_MainTex", new Vector2(Vector3.Distance(positions[0], positions[1]) * scaleSize, 1f));
	}
}
