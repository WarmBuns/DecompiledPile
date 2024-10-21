using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class DustStreakManager : MonoBehaviour
{
	public GameObject dustStreakPrefab;

	public float timeBetweenStreaksMin;

	public float timeBetweenStreaksMax;

	private float streakTimer;

	public List<Transform> dustStreakLocations = new List<Transform>();

	private int streakNum;

	private bool startDustStreaks;

	private void Start()
	{
		streakTimer = Random.Range(timeBetweenStreaksMin, timeBetweenStreaksMax);
		startDustStreaks = true;
	}

	private void FixedUpdate()
	{
		if (startDustStreaks)
		{
			streakTimer -= Time.deltaTime;
			if (streakTimer <= 0f)
			{
				streakTimer = Random.Range(timeBetweenStreaksMin, timeBetweenStreaksMax);
				streakNum = Random.Range(0, dustStreakLocations.Count);
				EffectManager.SimpleEffect(dustStreakPrefab, dustStreakLocations[streakNum].position, dustStreakPrefab.transform.rotation, transmit: true);
			}
		}
	}
}
