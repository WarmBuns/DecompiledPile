using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

public class DirectorCore : MonoBehaviour
{
	private struct NodeReference : IEquatable<NodeReference>
	{
		public readonly NodeGraph nodeGraph;

		public readonly NodeGraph.NodeIndex nodeIndex;

		public NodeReference(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex)
		{
			this.nodeGraph = nodeGraph;
			this.nodeIndex = nodeIndex;
		}

		public bool Equals(NodeReference other)
		{
			if (object.Equals(nodeGraph, other.nodeGraph))
			{
				return nodeIndex.Equals(other.nodeIndex);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is NodeReference other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((nodeGraph != null) ? nodeGraph.GetHashCode() : 0) * 397) ^ nodeIndex.GetHashCode();
		}
	}

	public enum MonsterSpawnDistance
	{
		Standard,
		Close,
		Far
	}

	public static List<GameObject> spawnedObjects = new List<GameObject>();

	private NodeReference[] occupiedNodes = Array.Empty<NodeReference>();

	public static DirectorCore instance { get; private set; }

	public GameObject[] GetObjectsOfTeam(TeamIndex _teamIndex)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < spawnedObjects.Count; i++)
		{
			CharacterMaster component = spawnedObjects[i].GetComponent<CharacterMaster>();
			if ((bool)component && component.teamIndex == _teamIndex)
			{
				spawnedObjects.Add(spawnedObjects[i]);
			}
		}
		return list.ToArray();
	}

	private void OnEnable()
	{
		if (!instance)
		{
			instance = this;
			return;
		}
		Debug.LogErrorFormat(this, "Duplicate instance of singleton class {0}. Only one should exist at a time.", GetType().Name);
	}

	private void OnDisable()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	public void AddOccupiedNode(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex)
	{
		Array.Resize(ref occupiedNodes, occupiedNodes.Length + 1);
		occupiedNodes[occupiedNodes.Length - 1] = new NodeReference(nodeGraph, nodeIndex);
	}

	private bool CheckPositionFree(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex, SpawnCard spawnCard)
	{
		if (Array.IndexOf(value: new NodeReference(nodeGraph, nodeIndex), array: occupiedNodes) != -1)
		{
			return false;
		}
		float num = HullDef.Find(spawnCard.hullSize).radius * 0.7f;
		nodeGraph.GetNodePosition(nodeIndex, out var position);
		if (spawnCard.nodeGraphType == MapNodeGroup.GraphType.Ground)
		{
			position += Vector3.up * (num + 0.25f);
		}
		return !HGPhysics.DoesOverlapSphere(position, num, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault | (int)LayerIndex.CommonMasks.fakeActorLayers);
	}

	public GameObject TrySpawnObject([NotNull] DirectorSpawnRequest directorSpawnRequest)
	{
		SpawnCard spawnCard = directorSpawnRequest.spawnCard;
		DirectorPlacementRule placementRule = directorSpawnRequest.placementRule;
		Xoroshiro128Plus rng = directorSpawnRequest.rng;
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(spawnCard.nodeGraphType);
		if (nodeGraph == null)
		{
			Debug.LogError($"Unable to find nodegraph for {SceneInfo.instance.sceneDef.cachedName} of type {spawnCard.nodeGraphType}.");
			return null;
		}
		GameObject result = null;
		switch (placementRule.placementMode)
		{
		case DirectorPlacementRule.PlacementMode.Direct:
		{
			Quaternion quaternion = Quaternion.Euler(0f, rng.nextNormalizedFloat * 360f, 0f);
			result = spawnCard.DoSpawn(placementRule.spawnOnTarget ? placementRule.spawnOnTarget.position : directorSpawnRequest.placementRule.position, placementRule.spawnOnTarget ? placementRule.spawnOnTarget.rotation : quaternion, directorSpawnRequest).spawnedInstance;
			break;
		}
		case DirectorPlacementRule.PlacementMode.Approximate:
		{
			List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRangeWithFlagConditions(placementRule.targetPosition, placementRule.minDistance, placementRule.maxDistance, (HullMask)(1 << (int)spawnCard.hullSize), spawnCard.requiredFlags, spawnCard.forbiddenFlags, placementRule.preventOverhead);
			if (list.Count == 0)
			{
			}
			while (list.Count > 0)
			{
				int index = rng.RangeInt(0, list.Count);
				NodeGraph.NodeIndex nodeIndex4 = list[index];
				if (nodeGraph.GetNodePosition(nodeIndex4, out var position4) && CheckPositionFree(nodeGraph, nodeIndex4, spawnCard))
				{
					Quaternion rotation4 = GetRotationFacingTargetPositionFromPoint(position4);
					result = spawnCard.DoSpawn(position4, rotation4, directorSpawnRequest).spawnedInstance;
					if (spawnCard.occupyPosition)
					{
						AddOccupiedNode(nodeGraph, nodeIndex4);
					}
					break;
				}
				list.RemoveAt(index);
			}
			break;
		}
		case DirectorPlacementRule.PlacementMode.NearestNode:
		{
			NodeGraph.NodeIndex nodeIndex3 = nodeGraph.FindClosestNodeWithFlagConditions(placementRule.targetPosition, spawnCard.hullSize, spawnCard.requiredFlags, spawnCard.forbiddenFlags, placementRule.preventOverhead);
			if (nodeGraph.GetNodePosition(nodeIndex3, out var position3))
			{
				Quaternion rotation3 = GetRotationFacingTargetPositionFromPoint(position3);
				result = spawnCard.DoSpawn(position3, rotation3, directorSpawnRequest).spawnedInstance;
				if (spawnCard.occupyPosition)
				{
					AddOccupiedNode(nodeGraph, nodeIndex3);
				}
			}
			break;
		}
		case DirectorPlacementRule.PlacementMode.ApproximateSimple:
		{
			NodeGraph.NodeIndex nodeIndex2 = nodeGraph.FindClosestNodeWithFlagConditions(placementRule.targetPosition, spawnCard.hullSize, spawnCard.requiredFlags, spawnCard.forbiddenFlags, placementRule.preventOverhead);
			if (nodeGraph.GetNodePosition(nodeIndex2, out var position2) && CheckPositionFree(nodeGraph, nodeIndex2, spawnCard))
			{
				Quaternion rotation2 = GetRotationFacingTargetPositionFromPoint(position2);
				result = spawnCard.DoSpawn(position2, rotation2, directorSpawnRequest).spawnedInstance;
				if (spawnCard.occupyPosition)
				{
					AddOccupiedNode(nodeGraph, nodeIndex2);
				}
			}
			break;
		}
		case DirectorPlacementRule.PlacementMode.Random:
		{
			List<NodeGraph.NodeIndex> activeNodesForHullMaskWithFlagConditions = nodeGraph.GetActiveNodesForHullMaskWithFlagConditions((HullMask)(1 << (int)spawnCard.hullSize), spawnCard.requiredFlags, spawnCard.forbiddenFlags);
			if (activeNodesForHullMaskWithFlagConditions.Count == 0)
			{
			}
			while (activeNodesForHullMaskWithFlagConditions.Count > 0)
			{
				int index2 = rng.RangeInt(0, activeNodesForHullMaskWithFlagConditions.Count);
				NodeGraph.NodeIndex nodeIndex5 = activeNodesForHullMaskWithFlagConditions[index2];
				if (nodeGraph.GetNodePosition(nodeIndex5, out var position5) && CheckPositionFree(nodeGraph, nodeIndex5, spawnCard))
				{
					Quaternion rotation5 = Quaternion.Euler(0f, rng.nextNormalizedFloat * 360f, 0f);
					result = spawnCard.DoSpawn(position5, rotation5, directorSpawnRequest).spawnedInstance;
					if (spawnCard.occupyPosition)
					{
						AddOccupiedNode(nodeGraph, nodeIndex5);
					}
					break;
				}
				activeNodesForHullMaskWithFlagConditions.RemoveAt(index2);
			}
			break;
		}
		case DirectorPlacementRule.PlacementMode.RandomNormalized:
		{
			if (SceneInfo.instance.approximateMapBoundMesh == null)
			{
				break;
			}
			Bounds bounds = SceneInfo.instance.approximateMapBoundMesh.bounds;
			Vector3 position = new Vector3(rng.RangeFloat(bounds.min.x, bounds.max.x), rng.RangeFloat(bounds.min.y, bounds.max.y), rng.RangeFloat(bounds.min.z, bounds.max.z));
			NodeGraph.NodeIndex nodeIndex = nodeGraph.FindClosestNodeWithFlagConditions(position, spawnCard.hullSize, spawnCard.requiredFlags, spawnCard.forbiddenFlags, placementRule.preventOverhead);
			if (nodeGraph.GetNodePosition(nodeIndex, out position) && CheckPositionFree(nodeGraph, nodeIndex, spawnCard))
			{
				Quaternion rotation = GetRotationFacingTargetPositionFromPoint(position);
				result = spawnCard.DoSpawn(position, rotation, directorSpawnRequest).spawnedInstance;
				if (spawnCard.occupyPosition)
				{
					AddOccupiedNode(nodeGraph, nodeIndex);
				}
			}
			break;
		}
		}
		return result;
		Quaternion GetRotationFacingTargetPositionFromPoint(Vector3 point)
		{
			point.y = placementRule.targetPosition.y;
			return Util.QuaternionSafeLookRotation(placementRule.targetPosition - point);
		}
	}

	public static void GetMonsterSpawnDistance(MonsterSpawnDistance input, out float minimumDistance, out float maximumDistance)
	{
		minimumDistance = 0f;
		maximumDistance = 0f;
		switch (input)
		{
		case MonsterSpawnDistance.Close:
			minimumDistance = 8f;
			maximumDistance = 20f;
			break;
		case MonsterSpawnDistance.Standard:
			minimumDistance = 25f;
			maximumDistance = 40f;
			break;
		case MonsterSpawnDistance.Far:
			minimumDistance = 70f;
			maximumDistance = 120f;
			break;
		}
	}
}
