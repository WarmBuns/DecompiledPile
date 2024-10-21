using System.Runtime.InteropServices;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PortalSpawner : NetworkBehaviour
{
	[SerializeField]
	private InteractableSpawnCard portalSpawnCard;

	[Range(0f, 1f)]
	[SerializeField]
	private float spawnChance;

	[SerializeField]
	[Tooltip("The portal is spawned relative to this transform.  If null, it uses this object's transform for reference.")]
	private Transform spawnReferenceLocation;

	[SerializeField]
	private float minSpawnDistance;

	[SerializeField]
	[Tooltip("The maximum spawn distance for the portal relative to the spawnReferenceLocation.  If 0, it will be spawned at exactly the referenced location.")]
	private float maxSpawnDistance;

	[SerializeField]
	private string spawnPreviewMessageToken;

	[SerializeField]
	private string spawnMessageToken;

	[SerializeField]
	private ChildLocator modelChildLocator;

	[SerializeField]
	private string previewChildName;

	[SerializeField]
	private ExpansionDef requiredExpansion;

	[SerializeField]
	private int minStagesCleared;

	[SerializeField]
	private string bannedEventFlag;

	[SerializeField]
	private string requiredEventFlag;

	[SerializeField]
	private string[] validStages;

	[SerializeField]
	private string[] invalidStages;

	[SerializeField]
	private int[] validStageTiers;

	private Xoroshiro128Plus rng;

	private GameObject previewChild;

	[SyncVar(hook = "OnWillSpawnUpdated")]
	private bool willSpawn;

	public bool NetworkwillSpawn
	{
		get
		{
			return willSpawn;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnWillSpawnUpdated(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref willSpawn, 1u);
		}
	}

	private void Start()
	{
		GetPreviewChild();
		if (!NetworkServer.active)
		{
			return;
		}
		rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
		bool flag = !requiredExpansion || Run.instance.IsExpansionEnabled(requiredExpansion);
		bool flag2 = Run.instance.stageClearCount >= minStagesCleared;
		bool flag3 = string.IsNullOrEmpty(bannedEventFlag) || !Run.instance.GetEventFlag(bannedEventFlag);
		if ((string.IsNullOrEmpty(requiredEventFlag) || Run.instance.GetEventFlag(requiredEventFlag)) && flag3 && rng.nextNormalizedFloat <= spawnChance && flag && flag2 && isValidStage())
		{
			NetworkwillSpawn = true;
			if (!string.IsNullOrEmpty(spawnPreviewMessageToken))
			{
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = spawnPreviewMessageToken
				});
			}
			if ((bool)previewChild)
			{
				previewChild.SetActive(value: true);
			}
		}
	}

	private void GetPreviewChild()
	{
		if ((bool)modelChildLocator)
		{
			Transform transform = modelChildLocator.FindChild(previewChildName);
			if ((bool)transform)
			{
				previewChild = transform.gameObject;
			}
		}
	}

	private bool isValidStage()
	{
		bool result = false;
		bool flag = false;
		if (validStages.Length != 0)
		{
			string baseSceneNameOverride = Stage.instance.sceneDef.baseSceneNameOverride;
			for (int i = 0; i < validStages.Length; i++)
			{
				if (validStages[i] == baseSceneNameOverride)
				{
					flag = true;
				}
			}
		}
		else if (validStages.Length == 0)
		{
			flag = true;
		}
		bool flag2 = false;
		if (invalidStages.Length != 0)
		{
			string baseSceneNameOverride2 = Stage.instance.sceneDef.baseSceneNameOverride;
			for (int j = 0; j < invalidStages.Length; j++)
			{
				if (invalidStages[j] == baseSceneNameOverride2)
				{
					flag2 = true;
				}
			}
		}
		bool flag3 = false;
		if (validStageTiers.Length != 0)
		{
			int stageOrder = Stage.instance.sceneDef.stageOrder;
			for (int k = 0; k < validStageTiers.Length; k++)
			{
				if (validStageTiers[k] == stageOrder)
				{
					flag3 = true;
				}
			}
		}
		else if (validStageTiers.Length == 0)
		{
			flag3 = true;
		}
		if (flag && !flag2 && flag3)
		{
			result = true;
		}
		_ = flag && flag2;
		return result;
	}

	[Server]
	public bool AttemptSpawnPortalServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.PortalSpawner::AttemptSpawnPortalServer()' called on client");
			return false;
		}
		if (willSpawn)
		{
			if ((bool)previewChild)
			{
				previewChild.SetActive(value: false);
			}
			NetworkwillSpawn = false;
			DirectorPlacementRule.PlacementMode placementMode = DirectorPlacementRule.PlacementMode.Approximate;
			if (maxSpawnDistance <= 0f)
			{
				placementMode = DirectorPlacementRule.PlacementMode.Direct;
			}
			Transform transform = spawnReferenceLocation;
			if (!transform)
			{
				transform = base.transform;
			}
			GameObject obj = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(portalSpawnCard, new DirectorPlacementRule
			{
				minDistance = minSpawnDistance,
				maxDistance = maxSpawnDistance,
				placementMode = placementMode,
				position = base.transform.position,
				spawnOnTarget = transform
			}, rng));
			if ((bool)obj && !string.IsNullOrEmpty(spawnMessageToken))
			{
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = spawnMessageToken
				});
			}
			return obj;
		}
		return false;
	}

	private void OnWillSpawnUpdated(bool newValue)
	{
		NetworkwillSpawn = newValue;
		if (previewChild == null)
		{
			GetPreviewChild();
		}
		if ((bool)previewChild)
		{
			previewChild.SetActive(newValue);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(willSpawn);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(willSpawn);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			willSpawn = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnWillSpawnUpdated(reader.ReadBoolean());
		}
	}

	public override void PreStartClient()
	{
	}
}
