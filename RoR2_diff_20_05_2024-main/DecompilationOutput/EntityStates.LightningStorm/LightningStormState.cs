using System;
using System.Collections.Generic;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.LightningStorm;

public class LightningStormState : BaseState
{
	[SerializeField]
	public GameObject lightningInstancePrefab;

	public static int strikesPerSurvivor = 4;

	public static float minDistanceBetweenStrikes = 3f;

	public static float maxStrikeDistanceFromPlayer = 5f;

	public static float frequencyOfLightningStrikes = 8f;

	public static float timeBetweenIndividualStrikes = 0.2f;

	private List<NodeGraph.NodeIndex> possibleNodesToTarget = new List<NodeGraph.NodeIndex>();

	private List<Vector3> possibleNodePositions = new List<Vector3>();

	private float stormStopwatch;

	private float strikeStopwatch;

	private Queue<Vector3> positionsToStrike = new Queue<Vector3>();

	private GameObject startPostProcessingObject;

	private PostProcessDuration startPostProcessDuration;

	private GameObject endPostProcessingObject;

	private PostProcessDuration endPostProcessDuration;

	private Action ExecuteNextStep;

	public override void OnEnter()
	{
		base.OnEnter();
		LightningStormController component = GetComponent<LightningStormController>();
		if ((bool)component)
		{
			startPostProcessingObject = component.startPostProcessingObject;
			startPostProcessDuration = component.startPostProcessDuration;
			endPostProcessingObject = component.endPostProcessingObject;
			endPostProcessDuration = component.endPostProcessDuration;
		}
		HandleStartStorm();
	}

	public override void OnExit()
	{
		base.OnExit();
		HandleStopStorm();
	}

	private void HandleStartStorm()
	{
		ToggleStormVFX(b: true);
		ExecuteNextStep = GenerateTargetPositions;
		stormStopwatch = -1f;
	}

	private void ToggleStormVFX(bool b)
	{
		if ((bool)startPostProcessingObject)
		{
			startPostProcessingObject.SetActive(b);
			startPostProcessDuration.enabled = b;
		}
		if ((bool)endPostProcessingObject)
		{
			endPostProcessingObject.SetActive(!b);
			endPostProcessDuration.enabled = !b;
		}
	}

	private void GenerateTargetPositions()
	{
		if (stormStopwatch > 0f)
		{
			return;
		}
		positionsToStrike.Clear();
		_ = minDistanceBetweenStrikes;
		_ = minDistanceBetweenStrikes;
		LightningStrikePattern lightningPattern = LightningStormController.lightningPattern;
		if (lightningPattern == null)
		{
			Debug.LogWarning("No Lightning Storm pattern set up in the LightningStormController!");
			return;
		}
		if (!SceneInfo.instance || !SceneInfo.instance.groundNodes)
		{
			Debug.LogWarning("No ground nodes available, disabling lightning storm");
			HandleStopStorm();
			return;
		}
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		possibleNodesToTarget.Clear();
		float num = 0f;
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!(instance == null) && !(instance.master == null) && instance.master.GetBody() != null)
			{
				num += 1f;
			}
		}
		num -= 1f;
		foreach (PlayerCharacterMasterController instance2 in PlayerCharacterMasterController.instances)
		{
			CharacterBody body = instance2.master.GetBody();
			if (body == null || !body.hasAuthority)
			{
				continue;
			}
			Transform coreTransform = body.coreTransform;
			Vector3 velocity = body.characterMotor.velocity;
			float magnitude = new Vector3(velocity.x, 0f, velocity.z).magnitude;
			velocity = velocity.normalized;
			float num2 = magnitude;
			foreach (LightningStormController.LightningStrikePoint lightningStrikePoint in lightningPattern.lightningStrikePoints)
			{
				float maxDistance = LightningStormController.GetMaxDistance(velocity, magnitude, lightningStrikePoint);
				if (maxDistance > num2)
				{
					num2 = maxDistance;
				}
			}
			num2 = Mathf.Max(num2, lightningPattern.maxAcceptableDistanceFromPredictedPoint * 2f);
			possibleNodesToTarget = groundNodes.FindNodesInRange(coreTransform.position, 0f, num2, HullMask.Human);
			possibleNodePositions.Clear();
			int count = possibleNodesToTarget.Count;
			for (int i = 0; i < count; i++)
			{
				if (groundNodes.GetNodePosition(possibleNodesToTarget[i], out var position2))
				{
					possibleNodePositions.Add(position2);
				}
			}
			int count2 = lightningPattern.lightningStrikePoints.Count;
			count2 -= (int)(num * lightningPattern.forEachAdditionalPlayerReduceStrikeTotalBy);
			count2 = Mathf.Max(count2, 1);
			foreach (LightningStormController.LightningStrikePoint lightningStrikePoint2 in lightningPattern.lightningStrikePoints)
			{
				if (positionsToStrike.Count >= count2)
				{
					break;
				}
				Vector3 position3 = LightningStormController.PredictPosition(coreTransform.position, coreTransform.forward, magnitude, lightningStrikePoint2);
				if (TryPickBestNodePosition(position3, lightningPattern.maxAcceptableDistanceFromPredictedPoint, out var bestPosition2))
				{
					positionsToStrike.Enqueue(bestPosition2);
				}
			}
		}
		ExecuteNextStep = FireNextStrike;
		bool TryPickBestNodePosition(Vector3 position, float range, out Vector3 bestPosition)
		{
			int num3 = -1;
			range *= range;
			float num4 = float.MaxValue;
			bestPosition = Vector3.zero;
			int count3 = possibleNodePositions.Count;
			Vector2 vector = new Vector2(position.x, position.z);
			for (int j = 0; j < count3; j++)
			{
				float sqrMagnitude = (new Vector2(possibleNodePositions[j].x, possibleNodePositions[j].z) - vector).sqrMagnitude;
				if (sqrMagnitude <= range && sqrMagnitude < num4)
				{
					num3 = j;
					num4 = sqrMagnitude;
				}
			}
			if (num3 != -1)
			{
				bestPosition = possibleNodePositions[num3];
				possibleNodePositions.RemoveAt(num3);
				return true;
			}
			return false;
		}
	}

	private void FireNextStrike()
	{
		if (positionsToStrike.Count < 1)
		{
			stormStopwatch = LightningStormController.lightningPattern.frequencyOfLightningStrikes;
			ExecuteNextStep = GenerateTargetPositions;
		}
		else if (!(strikeStopwatch > 0f))
		{
			strikeStopwatch = LightningStormController.lightningPattern.timeBetweenIndividualStrikes;
			LightningStormController.FireLightningBolt(positionsToStrike.Dequeue(), lightningInstancePrefab);
		}
	}

	private void WaitForNextStrike()
	{
		if (stormStopwatch <= 0f)
		{
			ExecuteNextStep = GenerateTargetPositions;
		}
	}

	private void HandleStopStorm()
	{
		ToggleStormVFX(b: false);
		ExecuteNextStep = null;
	}

	public override void Update()
	{
		float deltaTime = Time.deltaTime;
		stormStopwatch -= deltaTime;
		strikeStopwatch -= deltaTime;
		ExecuteNextStep?.Invoke();
	}

	private void LightningStrikeAtPosition(Vector3 position)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(lightningInstancePrefab);
		LightningStrikeInstance component = gameObject.GetComponent<LightningStrikeInstance>();
		if (!component)
		{
			Debug.Log("GameObject doesn't have a lightninginstance component!", gameObject);
		}
		else
		{
			component.Initialize(position, null);
		}
	}
}
