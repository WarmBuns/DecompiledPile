using System;
using UnityEngine;

public class Wind : MonoBehaviour
{
	private Renderer rend;

	private MaterialPropertyBlock props;

	public Vector4 windVector;

	public float MainWindAmplitude = 1f;

	public float MainWindSpeed = 3f;

	private float time;

	private void Start()
	{
		rend = GetComponent<Renderer>();
		props = new MaterialPropertyBlock();
		SetWind(props, windVector);
	}

	private void Update()
	{
		time += Time.deltaTime;
		windVector.x = (0.5f + 0.5f * Mathf.Sin(MainWindSpeed * time * (MathF.PI / 180f))) * MainWindAmplitude;
		SetWind(props, windVector);
	}

	private void SetWind(MaterialPropertyBlock block, Vector4 input)
	{
		rend.GetPropertyBlock(block);
		block.Clear();
		block.SetVector("_Wind", input);
		rend.SetPropertyBlock(block);
	}
}
