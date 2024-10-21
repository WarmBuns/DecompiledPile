using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class DeathState : GenericCharacterDeath
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
	public GameObject initialEffectPrefab;

	[SerializeField]
	public string initialEffectMuzzle;

	[SerializeField]
	public GameObject explosionEffectPrefab;

	[SerializeField]
	public string explosionEffectMuzzle;

	[SerializeField]
	public Vector3 ragdollForce;

	[SerializeField]
	public float explosionForce;

	[SerializeField]
	public bool addPrintController;

	[SerializeField]
	public float printDuration;

	[SerializeField]
	public float startingPrintBias;

	[SerializeField]
	public float maxPrintBias;

	private Transform modelTransform;

	protected override bool shouldAutoDestroy => false;

	protected override void PlayDeathAnimation(float crossfadeDuration)
	{
		PlayCrossfade(animationLayerName, animationStateName, animationPlaybackRateParam, duration, crossfadeDuration);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)VoidRaidGauntletController.instance)
		{
			VoidRaidGauntletController.instance.SetCurrentDonutCombatDirectorEnabled(isEnabled: false);
		}
		modelTransform = GetModelTransform();
		Transform transform = FindModelChild("StandableSurface");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: false);
		}
		if ((bool)explosionEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(initialEffectPrefab, base.gameObject, initialEffectMuzzle, transmit: false);
		}
		if (addPrintController)
		{
			PrintController printController = modelTransform.gameObject.AddComponent<PrintController>();
			printController.printTime = printDuration;
			printController.enabled = true;
			printController.startingPrintHeight = 99f;
			printController.maxPrintHeight = 99f;
			printController.startingPrintBias = startingPrintBias;
			printController.maxPrintBias = maxPrintBias;
			printController.disableWhenFinished = false;
			printController.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			if ((bool)explosionEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(explosionEffectPrefab, base.gameObject, explosionEffectMuzzle, transmit: true);
			}
			DestroyBodyAsapServer();
		}
	}

	public override void OnExit()
	{
		if ((bool)modelTransform)
		{
			RagdollController component = modelTransform.GetComponent<RagdollController>();
			Rigidbody component2 = GetComponent<Rigidbody>();
			if ((bool)component && (bool)component2)
			{
				component.BeginRagdoll(ragdollForce);
			}
			ExplodeRigidbodiesOnStart component3 = modelTransform.GetComponent<ExplodeRigidbodiesOnStart>();
			if ((bool)component3)
			{
				component3.force = explosionForce;
				component3.enabled = true;
			}
		}
	}
}
