using UnityEngine;

public class Visualizer : MonoBehaviour
{
	public float yscale;

	public GameObject particleObject;

	public float yvalue;

	private Vector3 initialPos;

	private void Start()
	{
		initialPos = particleObject.transform.localPosition;
	}

	private void Update()
	{
		particleObject.transform.localPosition = initialPos + new Vector3(0f, yvalue / yscale, 0f);
	}
}
