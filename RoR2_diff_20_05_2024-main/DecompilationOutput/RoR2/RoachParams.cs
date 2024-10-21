using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/RoachParams")]
public class RoachParams : ScriptableObject
{
	public float reorientTimerMin = 0.2f;

	public float reorientTimerMax = 0.5f;

	public float turnSpeed = 72f;

	public float acceleration = 400f;

	public float maxSpeed = 100f;

	public float backupDuration = 0.1f;

	public float wiggle = 720f;

	public float stepSize = 0.1f;

	public float minSimulationDuration = 3f;

	public float maxSimulationDuration = 7f;

	public float chanceToFinishOnBump = 0.5f;

	public float keyframeInterval = 1f / 15f;

	public float minReactionTime;

	public float maxReactionTime = 0.2f;

	public GameObject roachPrefab;
}
