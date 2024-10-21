using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GrandParent;

public class ChannelSun : ChannelSunBase
{
	public static string animLayerName;

	public static string animStateName;

	public static GameObject sunPrefab;

	public static float sunPrefabDiameter = 10f;

	public static float sunPlacementMinDistance = 100f;

	public static float sunPlacementIdealAltitudeBonus = 200f;

	private GameObject sunInstance;

	public Vector3? sunSpawnPosition;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animLayerName, animStateName);
		if (NetworkServer.active)
		{
			sunSpawnPosition = sunSpawnPosition ?? FindSunSpawnPosition(base.transform.position);
			if (sunSpawnPosition.HasValue)
			{
				sunInstance = CreateSun(sunSpawnPosition.Value);
			}
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active && (bool)sunInstance)
		{
			sunInstance.GetComponent<GenericOwnership>().ownerObject = null;
			sunInstance = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && !IsKeyDownAuthority())
		{
			outer.SetNextState(new ChannelSunEnd());
		}
	}

	private GameObject CreateSun(Vector3 sunSpawnPosition)
	{
		GameObject obj = Object.Instantiate(sunPrefab, sunSpawnPosition, Quaternion.identity);
		obj.GetComponent<GenericOwnership>().ownerObject = base.gameObject;
		NetworkServer.Spawn(obj);
		return obj;
	}

	private static Vector3? FindSunNodePosition(Vector3 searchOrigin)
	{
		NodeGraph airNodes = SceneInfo.instance.airNodes;
		NodeGraph.NodeIndex nodeIndex = airNodes.FindClosestNodeWithFlagConditions(searchOrigin, HullClassification.Golem, NodeFlags.None, NodeFlags.None, preventOverhead: false);
		if (nodeIndex == NodeGraph.NodeIndex.invalid)
		{
			return null;
		}
		float num = sunPlacementMinDistance;
		float num2 = num * num;
		_ = NodeGraph.NodeIndex.invalid;
		float num3 = 0f;
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(airNodes, HullMask.Golem);
		nodeGraphSpider.AddNodeForNextStep(nodeIndex);
		int num4 = 0;
		int i = 0;
		while (nodeGraphSpider.PerformStep())
		{
			num4++;
			for (; i < nodeGraphSpider.collectedSteps.Count; i++)
			{
				NodeGraphSpider.StepInfo stepInfo = nodeGraphSpider.collectedSteps[i];
				airNodes.GetNodePosition(stepInfo.node, out var position);
				float sqrMagnitude = (position - searchOrigin).sqrMagnitude;
				if (sqrMagnitude > num3)
				{
					num3 = sqrMagnitude;
					_ = stepInfo.node;
					if (num3 >= num2)
					{
						return position;
					}
				}
			}
		}
		return null;
	}

	public static Vector3? FindSunSpawnPosition(Vector3 searchOrigin)
	{
		Vector3? vector = FindSunNodePosition(searchOrigin);
		if (vector.HasValue)
		{
			Vector3 value = vector.Value;
			float num = sunPlacementIdealAltitudeBonus;
			float num2 = sunPrefabDiameter * 0.5f;
			if (Physics.Raycast(value, Vector3.up, out var hitInfo, sunPlacementIdealAltitudeBonus + num2, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				num = Mathf.Clamp(hitInfo.distance - num2, 0f, num);
			}
			value.y += num;
			return value;
		}
		return null;
	}
}
