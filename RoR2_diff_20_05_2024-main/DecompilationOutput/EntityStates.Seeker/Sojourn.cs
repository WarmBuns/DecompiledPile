using RoR2;
using RoR2.PostProcessing;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Seeker;

public class Sojourn : BaseState
{
	[SerializeField]
	public GameObject vehiclePrefab;

	public static float smallHopVelocity;

	public static Color screenDamageColor;

	public static bool combineTintAdditively;

	public static float minimumDamageStrength;

	public static float maximumDamageStrength;

	public static float pulseDuration;

	private SeekerController seekerController;

	public float duration = 0.3f;

	private bool animFinished;

	private ScreenDamageCalculatorSojourn screenDamageCalculator;

	public override void OnEnter()
	{
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(DLC2Content.Buffs.SojournVehicle);
		}
		screenDamageCalculator = new ScreenDamageCalculatorSojourn();
		screenDamageCalculator.minimumDamage = minimumDamageStrength;
		screenDamageCalculator.maximumDamage = maximumDamageStrength;
		screenDamageCalculator.duration = pulseDuration;
		ScreenDamageData screenDamageData = screenDamageCalculator.screenDamageData;
		screenDamageData.IntendedTintColor = screenDamageColor;
		screenDamageData.combineAdditively = combineTintAdditively;
		base.characterBody.healthComponent.screenDamageCalculator = screenDamageCalculator;
		seekerController = base.characterBody.transform.GetComponent<SeekerController>();
		seekerController.doesExitVehicle = false;
		SmallHop(base.characterMotor, smallHopVelocity);
		PlayAnimation("Gesture, Override", "SojournEnterTransform");
		base.fixedAge = 0f;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration && !animFinished)
		{
			if (NetworkServer.active)
			{
				Ray aimRay = GetAimRay();
				GameObject gameObject = Object.Instantiate(vehiclePrefab, aimRay.origin, Quaternion.LookRotation(aimRay.direction));
				gameObject.GetComponent<SojournVehicle>().sojournState = this;
				gameObject.GetComponent<VehicleSeat>().AssignPassenger(base.gameObject);
				NetworkUser networkUser = base.characterBody?.master?.playerCharacterMasterController?.networkUser;
				if ((bool)networkUser)
				{
					NetworkServer.SpawnWithClientAuthority(gameObject, networkUser.gameObject);
				}
				else
				{
					NetworkServer.Spawn(gameObject);
				}
			}
			animFinished = true;
		}
		if (seekerController.doesExitVehicle)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Vehicle;
	}

	public override void OnExit()
	{
		PlayAnimation("Gesture, Override", "SojournExitTransformation");
		if (screenDamageCalculator != null)
		{
			screenDamageCalculator.End();
		}
		base.characterBody.healthComponent.screenDamageCalculator = null;
		if (!NetworkServer.active)
		{
			return;
		}
		base.characterBody.RemoveBuff(DLC2Content.Buffs.SojournVehicle);
		if (base.characterBody.HasBuff(DLC2Content.Buffs.SojournHealing))
		{
			float num = base.characterBody.GetBuffCount(DLC2Content.Buffs.SojournHealing);
			for (int i = 0; (float)i < num; i++)
			{
				base.characterBody.RemoveBuff(DLC2Content.Buffs.SojournHealing);
			}
		}
	}
}
