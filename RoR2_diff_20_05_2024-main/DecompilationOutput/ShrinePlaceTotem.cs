using System;
using UnityEngine;

public class ShrinePlaceTotem : MonoBehaviour
{
	public int totemCount = 5;

	public GameObject totem;

	[Tooltip("Distance from which to form totem ring")]
	public float totemRadius = 2f;

	[Tooltip("Height from which to calculate totem placements.")]
	public float height = 10f;

	[Tooltip("Distance to raycast for totems")]
	public float raycastDistance = 20f;

	[Tooltip("Random bending of totems")]
	public float bendAmount = 15f;

	[Tooltip("Allowed difference from straight up (1) to straight down (-1)")]
	public float dotLimit = 0.8f;

	private void Awake()
	{
		int num = 0;
		for (int i = 0; i < totemCount * 2; i++)
		{
			float num2 = ((i >= totemCount) ? ((float)i * 1f * (360f / (float)totemCount)) : (((float)i + 0.5f) * (360f / (float)totemCount)));
			Physics.Raycast(base.transform.position + new Vector3(0f, height, 0f), new Vector3(Mathf.Cos(num2 * (MathF.PI / 180f)) * totemRadius, 0f - height, Mathf.Sin(num2 * (MathF.PI / 180f)) * totemRadius), out var hitInfo, raycastDistance);
			if (hitInfo.collider != null && Vector3.Dot(hitInfo.normal, Vector3.up) > dotLimit)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(totem, hitInfo.point, Quaternion.identity);
				gameObject.transform.parent = base.transform;
				gameObject.transform.rotation = Quaternion.FromToRotation(gameObject.transform.up, hitInfo.normal);
				gameObject.transform.eulerAngles += new Vector3(UnityEngine.Random.Range(0f - bendAmount, bendAmount), UnityEngine.Random.Range(0f - bendAmount, bendAmount), UnityEngine.Random.Range(0f - bendAmount, bendAmount));
				gameObject.transform.position -= new Vector3(0f, UnityEngine.Random.Range(0.1f, 0.2f), 0f);
				num++;
			}
			if (num == totemCount)
			{
				break;
			}
		}
	}

	private void Update()
	{
	}
}
