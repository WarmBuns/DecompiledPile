using RoR2;
using UnityEngine;

namespace EntityStates.Halcyonite;

public class ChargeTriLaser : BaseState
{
	public static float baseDuration = 3f;

	public static float laserMaxWidth = 0.2f;

	public static GameObject effectPrefab;

	public static GameObject laserPrefab;

	public static string attackSoundString;

	private float duration;

	private uint chargePlayID;

	private GameObject chargeEffect;

	private GameObject laserEffect;

	private LineRenderer laserLineComponent;

	private Vector3 laserDirection;

	private Vector3 visualEndPosition;

	private float flashTimer;

	private bool laserOn;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("FullBody Override", "TriLaserEnter", "TriLaser.playbackRate", duration, 0.1f);
		Transform modelTransform = GetModelTransform();
		chargePlayID = Util.PlayAttackSpeedSound("Play_halcyonite_skill2_chargeup", base.gameObject, attackSpeedStat);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleLaser");
				if ((bool)transform)
				{
					if ((bool)effectPrefab)
					{
						chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
						chargeEffect.transform.parent = transform;
						ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
						if ((bool)component2)
						{
							component2.newDuration = duration;
						}
					}
					if ((bool)laserPrefab)
					{
						laserEffect = Object.Instantiate(laserPrefab, transform.position, transform.rotation);
						laserEffect.transform.parent = transform;
						laserLineComponent = laserEffect.GetComponent<LineRenderer>();
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
		flashTimer = 0f;
		laserOn = true;
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(chargePlayID);
		base.OnExit();
		if ((bool)chargeEffect)
		{
			EntityState.Destroy(chargeEffect);
		}
		if ((bool)laserEffect)
		{
			EntityState.Destroy(laserEffect);
		}
		PlayAnimation("FullBody Override", "BufferEmpty");
	}

	public override void Update()
	{
		base.Update();
		if (!laserEffect || !laserLineComponent)
		{
			return;
		}
		float num = 1000f;
		Ray aimRay = GetAimRay();
		Vector3 position = laserEffect.transform.parent.position;
		Vector3 point = aimRay.GetPoint(num);
		laserDirection = point - position;
		if (Physics.Raycast(aimRay, out var hitInfo, num, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask))
		{
			point = hitInfo.point;
		}
		laserLineComponent.SetPosition(0, position);
		laserLineComponent.SetPosition(1, point);
		float num2;
		if (duration - base.age > 0.5f)
		{
			num2 = base.age / duration;
		}
		else
		{
			flashTimer -= Time.deltaTime;
			if (flashTimer <= 0f)
			{
				laserOn = !laserOn;
				flashTimer = 1f / 30f;
			}
			num2 = (laserOn ? 1f : 0f);
		}
		num2 *= laserMaxWidth;
		laserLineComponent.startWidth = num2;
		laserLineComponent.endWidth = num2;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			TriLaser triLaser = new TriLaser();
			triLaser.laserDirection = laserDirection;
			outer.SetNextState(triLaser);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
