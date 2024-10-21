using System;
using RoR2.Navigation;
using UnityEngine;

namespace RoR2;

public class MeridianSpawnPointGenerator : MonoBehaviour
{
	[SerializeField]
	private Transform playerSpawnPoint;

	private void OnEnable()
	{
		SceneDirector.onPreGeneratePlayerSpawnPointsServer += OnPreGeneratePlayerSpawnPointsServer;
	}

	private void OnDisable()
	{
		SceneDirector.onPreGeneratePlayerSpawnPointsServer -= OnPreGeneratePlayerSpawnPointsServer;
	}

	private void OnPreGeneratePlayerSpawnPointsServer(SceneDirector sceneDirector, ref Action generationMethod)
	{
		generationMethod = GeneratePlayerSpawnPointsServer;
	}

	private void GeneratePlayerSpawnPointsServer()
	{
		if (SpawnPoint.readOnlyInstancesList.Count > 3 || playerSpawnPoint == null)
		{
			return;
		}
		Vector3 position = playerSpawnPoint.position;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		if (!groundNodes)
		{
			Debug.LogError("MeridianSpawnPointGenerator.GeneratePlayerSpawnPointsServer: No ground nodegraph found to place spawn points.", this);
			return;
		}
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(groundNodes, HullMask.Human);
		nodeGraphSpider.AddNodeForNextStep(groundNodes.FindClosestNode(position, HullClassification.Human));
		for (int i = 0; i < 4; i++)
		{
			nodeGraphSpider.PerformStep();
			if (nodeGraphSpider.collectedSteps.Count > 16)
			{
				break;
			}
		}
		for (int j = 0; j < nodeGraphSpider.collectedSteps.Count; j++)
		{
			NodeGraphSpider.StepInfo stepInfo = nodeGraphSpider.collectedSteps[j];
			groundNodes.GetNodePosition(stepInfo.node, out var position2);
			Quaternion rotation = playerSpawnPoint.rotation;
			SpawnPoint.AddSpawnPoint(position2, rotation);
		}
	}
}
