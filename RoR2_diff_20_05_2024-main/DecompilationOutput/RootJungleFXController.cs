using System;
using System.Collections.Generic;
using UnityEngine;

public class RootJungleFXController : MonoBehaviour
{
	public int minSystemsActive = 2;

	public int maxSystemsActive = 4;

	public float minActiveTime = 10f;

	public float maxActiveTime = 30f;

	private int numActive;

	private float timeActive;

	private float effectsTimer;

	private bool timeFX;

	public List<ParticleSystem> FXParticles = new List<ParticleSystem>();

	private void Start()
	{
		TurnOffFX(FXParticles.Count);
	}

	private void FixedUpdate()
	{
		if (timeFX)
		{
			effectsTimer -= Time.fixedDeltaTime;
			if (effectsTimer <= 0f)
			{
				timeFX = false;
				TurnOffFX(numActive);
			}
		}
	}

	public void SetupParticles()
	{
		numActive = UnityEngine.Random.Range(minSystemsActive, maxSystemsActive + 1);
		timeActive = UnityEngine.Random.Range(minActiveTime, maxActiveTime);
		effectsTimer = timeActive;
		FXParticles = Shuffle(FXParticles);
		TurnOnFX(numActive);
		timeFX = true;
	}

	public void TurnOffFX(int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			ParticleSystem.EmissionModule emission = FXParticles[i].emission;
			emission.enabled = false;
		}
		SetupParticles();
	}

	public void TurnOnFX(int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			ParticleSystem.EmissionModule emission = FXParticles[i].emission;
			emission.enabled = true;
		}
	}

	public static List<T> Shuffle<T>(List<T> list)
	{
		System.Random random = new System.Random();
		for (int i = 0; i < list.Count; i++)
		{
			int index = random.Next(0, i);
			T value = list[index];
			list[index] = list[i];
			list[i] = value;
		}
		return list;
	}
}
