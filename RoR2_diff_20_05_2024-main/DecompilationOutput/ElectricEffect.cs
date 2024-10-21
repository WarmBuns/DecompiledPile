using System.Collections.Generic;
using UnityEngine;

public class ElectricEffect : MonoBehaviour
{
	public LineRenderer lightningLineRenderer;

	public List<Texture> lightningTextures = new List<Texture>();

	private Material lineRendererMat;

	private int counter;

	private int textureNum;

	public int interval = 3;

	public float lineWidth = 1f;

	private void Start()
	{
		lineRendererMat = lightningLineRenderer.material;
		lightningLineRenderer.startWidth = lineWidth;
		lightningLineRenderer.endWidth = lineWidth;
	}

	private void Update()
	{
		if (!(Time.timeScale > 0f))
		{
			return;
		}
		if (counter >= interval)
		{
			if (textureNum == lightningTextures.Count)
			{
				textureNum = 0;
			}
			lineRendererMat.SetTexture("_MainTex", lightningTextures[textureNum]);
			textureNum++;
			counter = 0;
		}
		counter++;
	}
}
