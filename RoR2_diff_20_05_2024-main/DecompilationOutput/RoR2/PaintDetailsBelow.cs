using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class PaintDetailsBelow : MonoBehaviour
{
	[Tooltip("Influence radius in world coordinates")]
	public float influenceOuter = 2f;

	public float influenceInner = 1f;

	[Tooltip("Which detail layer")]
	public int layer;

	[Tooltip("Density, from 0-1")]
	public float density = 0.5f;

	public float densityPower = 1f;

	private static List<PaintDetailsBelow> painterList;

	static PaintDetailsBelow()
	{
		painterList = new List<PaintDetailsBelow>();
	}

	public void OnEnable()
	{
		painterList.Add(this);
	}

	public void OnDisable()
	{
		painterList.Remove(this);
	}

	public static List<PaintDetailsBelow> GetPainters()
	{
		return painterList;
	}
}
