using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MegaConstruct;

public class RaiseShield : FlyState
{
	[SerializeField]
	public GameObject attachmentPrefab;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationEnterStateName;

	[SerializeField]
	public float duration;

	private NetworkedBodyAttachment attachment;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active && (bool)attachmentPrefab)
		{
			attachment = Object.Instantiate(attachmentPrefab).GetComponent<NetworkedBodyAttachment>();
			attachment.AttachToGameObjectAndSpawn(base.characterBody.gameObject);
		}
		MasterSpawnSlotController component = GetComponent<MasterSpawnSlotController>();
		if (NetworkServer.active && (bool)component)
		{
			component.SpawnAllOpen(base.gameObject, Run.instance.stageRng);
		}
		PlayAnimation(animationLayerName, animationEnterStateName);
	}

	protected override bool CanExecuteSkill(GenericSkill skillSlot)
	{
		return false;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new ExitShield());
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active && (bool)attachment)
		{
			EntityState.Destroy(attachment.gameObject);
		}
		base.OnExit();
	}
}
