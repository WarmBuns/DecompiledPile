using System.Collections;
using System.Collections.Generic;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

public class DevotedLemurianController : MonoBehaviour
{
	private CharacterMaster _lemurianMaster;

	private DevotionInventoryController _devotionInventoryController;

	private ItemIndex _devotionItem;

	private int _devotedEvolutionLevel;

	private float _leashDistSq = 160000f;

	private const float minTeleportDistance = 10f;

	private const float maxTeleportDistance = 40f;

	public int DevotedEvolutionLevel
	{
		get
		{
			return _devotedEvolutionLevel;
		}
		set
		{
			_devotedEvolutionLevel = value;
		}
	}

	public ItemIndex DevotionItem => _devotionItem;

	public CharacterBody LemurianBody => _lemurianMaster.GetBody();

	public Inventory LemurianInventory => _lemurianMaster.inventory;

	private void Start()
	{
		StartCoroutine(TryTeleport());
	}

	private bool CheckIfNeedTeleport()
	{
		if (!LemurianBody.hasEffectiveAuthority)
		{
			return false;
		}
		CharacterMaster characterMaster = (_lemurianMaster ? _lemurianMaster.minionOwnership.ownerMaster : null);
		CharacterBody characterBody = (characterMaster ? characterMaster.GetBody() : null);
		if (!characterBody)
		{
			return false;
		}
		Vector3 corePosition = characterBody.corePosition;
		Vector3 corePosition2 = LemurianBody.corePosition;
		if ((((bool)LemurianBody.characterMotor && LemurianBody.characterMotor.walkSpeed > 0f) || LemurianBody.moveSpeed > 0f) && (corePosition2 - corePosition).sqrMagnitude > _leashDistSq)
		{
			return true;
		}
		return false;
	}

	private IEnumerator TryTeleport()
	{
		Debug.Log("try teleport");
		while ((bool)_lemurianMaster)
		{
			while (!LemurianBody || !CheckIfNeedTeleport())
			{
				yield return new WaitForSeconds(1f);
			}
			CharacterMaster characterMaster = (_lemurianMaster ? _lemurianMaster.minionOwnership.ownerMaster : null);
			CharacterBody ownerBody = characterMaster.GetBody();
			NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
			List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRangeWithFlagConditions(ownerBody.transform.position, 3f, 20f, HullMask.None, NodeFlags.None, NodeFlags.NoCharacterSpawn, preventOverhead: false);
			while (list.Count == 0)
			{
				Debug.Log("no valid node to teleport");
				yield return new WaitForSeconds(1f);
				list = nodeGraph.FindNodesInRangeWithFlagConditions(ownerBody.transform.position, 3f, 20f, HullMask.None, NodeFlags.None, NodeFlags.NoCharacterSpawn, preventOverhead: false);
			}
			while (list.Count > 0)
			{
				Debug.Log("teleporting");
				int index = Random.Range(0, list.Count);
				NodeGraph.NodeIndex nodeIndex = list[index];
				if (nodeGraph.GetNodePosition(nodeIndex, out var position))
				{
					TeleportHelper.TeleportBody(LemurianBody, position);
					GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(LemurianBody.gameObject);
					if ((bool)teleportEffectPrefab)
					{
						EffectManager.SimpleEffect(teleportEffectPrefab, position, Quaternion.identity, transmit: true);
					}
					break;
				}
			}
			yield return new WaitForSeconds(1f);
		}
		Debug.Log("finish try teleport");
	}

	public void InitializeDevotedLemurian(ItemIndex itemIndex, DevotionInventoryController devotionInventoryController)
	{
		_devotionItem = itemIndex;
		_devotionInventoryController = devotionInventoryController;
		_lemurianMaster = GetComponent<CharacterMaster>();
	}

	public void OnDevotedBodyDead()
	{
		if (_devotionInventoryController.HasItem(RoR2Content.Items.ExtraLife))
		{
			_devotionInventoryController.RemoveItem(RoR2Content.Items.ExtraLife.itemIndex);
		}
		else
		{
			_devotionInventoryController.RemoveItem(_devotionItem, _devotedEvolutionLevel + 1);
			_devotionInventoryController.DropScrapOnDeath(_devotionItem, LemurianBody);
		}
		_devotionInventoryController.UpdateAllMinions();
	}
}
