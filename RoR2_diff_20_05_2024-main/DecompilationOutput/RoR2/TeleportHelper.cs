using RoR2.Navigation;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public static class TeleportHelper
{
	private class TeleportMessage : MessageBase
	{
		public GameObject gameObject;

		public Vector3 newPosition;

		public Vector3 delta;

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(gameObject);
			writer.Write(newPosition);
			writer.Write(delta);
		}

		public override void Deserialize(NetworkReader reader)
		{
			gameObject = reader.ReadGameObject();
			newPosition = reader.ReadVector3();
			delta = reader.ReadVector3();
		}
	}

	private static readonly TeleportMessage messageBuffer = new TeleportMessage();

	public static void TeleportGameObject(GameObject gameObject, Vector3 newPosition)
	{
		bool hasEffectiveAuthority = Util.HasEffectiveAuthority(gameObject);
		TeleportGameObject(gameObject, newPosition, newPosition - gameObject.transform.position, hasEffectiveAuthority);
	}

	private static void TeleportGameObject(GameObject gameObject, Vector3 newPosition, Vector3 delta, bool hasEffectiveAuthority)
	{
		OnTeleport(gameObject, newPosition, delta);
		if (NetworkServer.active || hasEffectiveAuthority)
		{
			TeleportMessage msg = new TeleportMessage
			{
				gameObject = gameObject,
				newPosition = newPosition,
				delta = delta
			};
			QosChannelIndex defaultReliable = QosChannelIndex.defaultReliable;
			if (NetworkServer.active)
			{
				NetworkServer.SendByChannelToAll(68, msg, defaultReliable.intVal);
			}
			else
			{
				NetworkManager.singleton.client.connection.SendByChannel(68, msg, defaultReliable.intVal);
			}
		}
	}

	private static void OnTeleport(GameObject gameObject, Vector3 newPosition, Vector3 delta)
	{
		CharacterMotor component = gameObject.GetComponent<CharacterMotor>();
		if ((bool)component)
		{
			component.Motor.SetPosition(newPosition);
			component.velocity = new Vector3(0f, Mathf.Min(component.velocity.y, 0f), 0f);
		}
		else
		{
			gameObject.transform.position = newPosition;
		}
		ITeleportHandler[] componentsInChildren = gameObject.GetComponentsInChildren<ITeleportHandler>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].OnTeleport(newPosition - delta, newPosition);
		}
	}

	public static void TeleportBody(CharacterBody body, Vector3 targetFootPosition)
	{
		if ((bool)body)
		{
			Vector3 vector = new Vector3(0f, 0.1f, 0f);
			Vector3 vector2 = body.footPosition - body.transform.position;
			TeleportGameObject(body.gameObject, targetFootPosition - vector2 + vector);
			if ((bool)body.characterMotor)
			{
				body.characterMotor.disableAirControlUntilCollision = false;
			}
		}
	}

	[NetworkMessageHandler(client = true, server = true, msgType = 68)]
	private static void HandleTeleport(NetworkMessage netMsg)
	{
		if (Util.ConnectionIsLocal(netMsg.conn))
		{
			return;
		}
		netMsg.ReadMessage(messageBuffer);
		if ((bool)messageBuffer.gameObject)
		{
			bool flag = Util.HasEffectiveAuthority(messageBuffer.gameObject);
			if (!flag)
			{
				TeleportGameObject(messageBuffer.gameObject, messageBuffer.newPosition, messageBuffer.delta, flag);
			}
		}
	}

	public static Vector3? FindSafeTeleportDestination(Vector3 characterFootPosition, CharacterBody characterBodyOrPrefabComponent, Xoroshiro128Plus rng)
	{
		Vector3? result = null;
		SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
		spawnCard.hullSize = characterBodyOrPrefabComponent.hullClassification;
		spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
		spawnCard.prefab = LegacyResourcesAPI.Load<GameObject>("SpawnCards/HelperPrefab");
		GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
			position = characterFootPosition
		}, rng));
		if (!gameObject)
		{
			gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.RandomNormalized,
				position = characterFootPosition
			}, rng));
		}
		if ((bool)gameObject)
		{
			result = gameObject.transform.position;
			Object.Destroy(gameObject);
		}
		Object.Destroy(spawnCard);
		return result;
	}
}
