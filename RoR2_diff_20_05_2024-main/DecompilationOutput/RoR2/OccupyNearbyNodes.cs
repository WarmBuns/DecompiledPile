using System.Collections.Generic;
using System.Linq;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

public class OccupyNearbyNodes : MonoBehaviour
{
	public float radius = 5f;

	private static readonly List<OccupyNearbyNodes> instancesList = new List<OccupyNearbyNodes>();

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SceneDirector.onPrePopulateSceneServer += OnSceneDirectorPrePopulateSceneServer;
	}

	private static void OnSceneDirectorPrePopulateSceneServer(SceneDirector sceneDirector)
	{
		DirectorCore instance = DirectorCore.instance;
		NodeGraph groundNodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		NodeGraph.NodeIndex[] array = instancesList.SelectMany((OccupyNearbyNodes v) => groundNodeGraph.FindNodesInRange(v.transform.position, 0f, v.radius, HullMask.None)).Distinct().ToArray();
		foreach (NodeGraph.NodeIndex nodeIndex in array)
		{
			instance.AddOccupiedNode(groundNodeGraph, nodeIndex);
		}
	}
}
