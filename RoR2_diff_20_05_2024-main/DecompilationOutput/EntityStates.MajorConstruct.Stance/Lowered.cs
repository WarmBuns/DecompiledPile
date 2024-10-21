using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MajorConstruct.Stance;

public class Lowered : BaseState
{
	[SerializeField]
	public GameObject attachmentPrefab;

	private NetworkedBodyAttachment attachment;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active && (bool)attachmentPrefab)
		{
			attachment = Object.Instantiate(attachmentPrefab).GetComponent<NetworkedBodyAttachment>();
			attachment.AttachToGameObjectAndSpawn(base.characterBody.gameObject);
		}
	}

	public override void OnExit()
	{
		if ((bool)attachment)
		{
			EntityState.Destroy(attachment.gameObject);
		}
		base.OnExit();
	}
}
