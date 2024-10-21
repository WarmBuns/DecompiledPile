using UnityEngine;

namespace RoR2;

public class PaintTerrain : MonoBehaviour
{
	public float splatHeightReference = 60f;

	public float splatRaycastLength = 200f;

	public float splatSlopePower = 1f;

	public float heightPower = 1f;

	public Vector3 snowfallDirection = Vector3.up;

	public Texture2D grassNoiseMap;

	private Terrain terrain;

	private TerrainData data;

	private float[,,] alphamaps;

	private int[,] detailmapGrass;

	private void Start()
	{
		snowfallDirection = snowfallDirection.normalized;
		terrain = GetComponent<Terrain>();
		data = terrain.terrainData;
		alphamaps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
		detailmapGrass = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, 0);
		UpdateAlphaMaps();
		UpdateDetailMaps();
	}

	private void UpdateAlphaMaps()
	{
		for (int i = 0; i < data.alphamapHeight; i++)
		{
			for (int j = 0; j < data.alphamapWidth; j++)
			{
				float z = (float)j / (float)data.alphamapWidth * data.size.x;
				float x = (float)i / (float)data.alphamapHeight * data.size.z;
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				float num5 = Mathf.Pow(Vector3.Dot(Vector3.up, data.GetInterpolatedNormal((float)i / (float)data.alphamapHeight, (float)j / (float)data.alphamapWidth)), splatSlopePower);
				num = num5;
				num3 = 1f - num5;
				float interpolatedHeight = data.GetInterpolatedHeight((float)i / (float)data.alphamapHeight, (float)j / (float)data.alphamapWidth);
				if (Physics.Raycast(new Vector3(x, interpolatedHeight, z), snowfallDirection, out var hitInfo, splatRaycastLength, LayerIndex.world.mask))
				{
					float num6 = num;
					float num7 = Mathf.Clamp01(Mathf.Pow(hitInfo.distance / splatHeightReference, heightPower));
					num = num7 * num6;
					num2 = (1f - num7) * num6;
				}
				alphamaps[j, i, 0] = num;
				alphamaps[j, i, 1] = num2;
				alphamaps[j, i, 2] = num3;
				alphamaps[j, i, 3] = num4;
			}
		}
		data.SetAlphamaps(0, 0, alphamaps);
	}

	private void UpdateDetailMaps()
	{
		for (int i = 0; i < data.detailHeight; i++)
		{
			for (int j = 0; j < data.detailWidth; j++)
			{
				int num = 0;
				detailmapGrass[j, i] = num;
			}
		}
		data.SetDetailLayer(0, 0, 0, detailmapGrass);
	}
}
