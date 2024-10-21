using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

public class PlayerSpawnInhibitor : MonoBehaviour
{
	private static List<PlayerSpawnInhibitor> instancesList = new List<PlayerSpawnInhibitor>();

	private static ReadOnlyCollection<PlayerSpawnInhibitor> _readOnlyInstancesList = new ReadOnlyCollection<PlayerSpawnInhibitor>(instancesList);

	[SerializeField]
	private float radius;

	[SerializeField]
	private Transform center;

	public static ReadOnlyCollection<PlayerSpawnInhibitor> readOnlyInstancesList => _readOnlyInstancesList;

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	public bool IsInhibiting(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex)
	{
		if ((bool)nodeGraph && nodeGraph.GetNodePosition(nodeIndex, out var position))
		{
			return (center.position - position).sqrMagnitude < radius * radius;
		}
		return false;
	}
}
