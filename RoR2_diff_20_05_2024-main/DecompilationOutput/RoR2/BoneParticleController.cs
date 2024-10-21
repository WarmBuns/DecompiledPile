using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class BoneParticleController : MonoBehaviour
{
	public GameObject childParticlePrefab;

	public float spawnFrequency;

	public SkinnedMeshRenderer skinnedMeshRenderer;

	private float stopwatch;

	private List<Transform> bonesList;

	private void Start()
	{
		bonesList = new List<Transform>();
		Transform[] bones = skinnedMeshRenderer.bones;
		foreach (Transform transform in bones)
		{
			if (transform.name.IndexOf("IK", StringComparison.OrdinalIgnoreCase) == -1 && transform.name.IndexOf("Root", StringComparison.OrdinalIgnoreCase) == -1 && transform.name.IndexOf("Base", StringComparison.OrdinalIgnoreCase) == -1)
			{
				Debug.LogFormat("added bone {0}", transform);
				bonesList.Add(transform);
			}
		}
	}

	private void Update()
	{
		if (!skinnedMeshRenderer)
		{
			return;
		}
		stopwatch += Time.deltaTime;
		if (stopwatch > 1f / spawnFrequency)
		{
			stopwatch -= 1f / spawnFrequency;
			int count = bonesList.Count;
			Transform transform = bonesList[UnityEngine.Random.Range(0, count)];
			if ((bool)transform)
			{
				UnityEngine.Object.Instantiate(childParticlePrefab, transform.transform.position, Quaternion.identity, transform);
			}
		}
	}
}
