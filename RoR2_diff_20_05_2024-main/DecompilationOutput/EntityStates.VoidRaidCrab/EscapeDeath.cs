using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class EscapeDeath : GenericCharacterDeath
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public bool addPrintController;

	[SerializeField]
	public float printDuration;

	[SerializeField]
	public float startingPrintHeight;

	[SerializeField]
	public float maxPrintHeight;

	[SerializeField]
	public string gauntletEntranceChildName;

	[SerializeField]
	public string enterSoundString;

	private Vector3 gauntletEntrancePosition;

	private Transform gauntletEntranceTransform;

	private NetworkInstanceId netId;

	protected override bool shouldAutoDestroy => false;

	protected override void PlayDeathAnimation(float crossfadeDuration)
	{
		PlayCrossfade(animationLayerName, animationStateName, animationPlaybackRateParam, duration, crossfadeDuration);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		netId = NetworkInstanceId.Invalid;
		if ((bool)base.characterBody && (bool)base.characterBody.master)
		{
			netId = base.characterBody.master.netId;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		if (addPrintController)
		{
			Transform modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				PrintController printController = modelTransform.gameObject.AddComponent<PrintController>();
				printController.printTime = printDuration;
				printController.enabled = true;
				printController.startingPrintHeight = startingPrintHeight;
				printController.maxPrintHeight = maxPrintHeight;
				printController.startingPrintBias = 0f;
				printController.maxPrintBias = 0f;
				printController.disableWhenFinished = false;
				printController.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
			}
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			gauntletEntranceTransform = modelChildLocator.FindChild(gauntletEntranceChildName);
			RefreshGauntletEntrancePosition();
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active)
		{
			RefreshGauntletEntrancePosition();
			VoidRaidGauntletController.instance.TryOpenGauntlet(gauntletEntrancePosition, netId);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			RefreshGauntletEntrancePosition();
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			EntityState.Destroy(base.gameObject);
		}
	}

	private void RefreshGauntletEntrancePosition()
	{
		if ((bool)gauntletEntranceTransform)
		{
			gauntletEntrancePosition = gauntletEntranceTransform.position;
		}
	}
}
