using System;
using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.Headstompers;

public class HeadstompersFall : BaseHeadstompersState
{
	private float stopwatch;

	public static float maxFallDuration = 0f;

	public static float maxFallSpeed = 30f;

	public static float maxDistance = 30f;

	public static float initialFallSpeed = 10f;

	public static float accelerationY = 40f;

	public static float minimumRadius = 5f;

	public static float maximumRadius = 100f;

	public static float minimumDamageCoefficient = 10f;

	public static float maximumDamageCoefficient = 100f;

	public static float seekCone = 20f;

	public static float springboardSpeed = 30f;

	private Transform seekTransform;

	private bool seekLost;

	private CharacterMotor onHitGroundProvider;

	private float initialY;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.isAuthority)
		{
			return;
		}
		if ((bool)body)
		{
			TeamMask allButNeutral = TeamMask.allButNeutral;
			TeamIndex objectTeam = TeamComponent.GetObjectTeam(bodyGameObject);
			if (objectTeam != TeamIndex.None)
			{
				allButNeutral.RemoveTeam(objectTeam);
			}
			BullseyeSearch obj = new BullseyeSearch
			{
				filterByLoS = true,
				maxDistanceFilter = 300f,
				maxAngleFilter = seekCone,
				searchOrigin = body.footPosition,
				searchDirection = Vector3.down,
				sortMode = BullseyeSearch.SortMode.Angle,
				teamMaskFilter = allButNeutral,
				viewer = body
			};
			initialY = body.footPosition.y;
			obj.RefreshCandidates();
			seekTransform = obj.GetResults().FirstOrDefault()?.transform;
		}
		SetOnHitGroundProviderAuthority(bodyMotor);
		if ((bool)bodyMotor)
		{
			bodyMotor.velocity.y = Mathf.Min(bodyMotor.velocity.y, 0f - initialFallSpeed);
		}
	}

	private void SetOnHitGroundProviderAuthority(CharacterMotor newOnHitGroundProvider)
	{
		if ((object)onHitGroundProvider != null)
		{
			onHitGroundProvider.onHitGroundAuthority -= OnMotorHitGroundAuthority;
		}
		onHitGroundProvider = newOnHitGroundProvider;
		if ((object)onHitGroundProvider != null)
		{
			onHitGroundProvider.onHitGroundAuthority += OnMotorHitGroundAuthority;
		}
	}

	public override void OnExit()
	{
		SetOnHitGroundProviderAuthority(null);
		base.OnExit();
	}

	private void OnMotorHitGroundAuthority(ref CharacterMotor.HitGroundInfo hitGroundInfo)
	{
		DoStompExplosionAuthority();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			FixedUpdateAuthority();
		}
	}

	private void FixedUpdateAuthority()
	{
		stopwatch += Time.deltaTime;
		if (base.isGrounded)
		{
			DoStompExplosionAuthority();
		}
		else if (stopwatch >= maxFallDuration)
		{
			outer.SetNextState(new HeadstompersCooldown());
		}
		else
		{
			if (!bodyMotor)
			{
				return;
			}
			Vector3 velocity = bodyMotor.velocity;
			if (velocity.y > 0f - maxFallSpeed)
			{
				velocity.y = Mathf.MoveTowards(velocity.y, 0f - maxFallSpeed, accelerationY * Time.deltaTime);
			}
			if ((bool)seekTransform && !seekLost)
			{
				Vector3 normalized = (seekTransform.position - body.footPosition).normalized;
				if (Vector3.Dot(Vector3.down, normalized) >= Mathf.Cos(seekCone * (MathF.PI / 180f)))
				{
					if (velocity.y < 0f)
					{
						Vector3 vector = normalized * (0f - velocity.y);
						vector.y = 0f;
						Vector3 vector2 = velocity;
						vector2.y = 0f;
						vector2 = vector;
						velocity.x = vector2.x;
						velocity.z = vector2.z;
					}
				}
				else
				{
					seekLost = true;
				}
			}
			bodyMotor.velocity = velocity;
		}
	}

	private void DoStompExplosionAuthority()
	{
		if ((bool)body)
		{
			Inventory inventory = body.inventory;
			if (((!inventory || inventory.GetItemCount(RoR2Content.Items.FallBoots) != 0) ? 1 : 0) > (false ? 1 : 0))
			{
				bodyMotor.velocity = Vector3.zero;
				float num = Mathf.Max(0f, initialY - body.footPosition.y);
				if (num > 0f)
				{
					float t = Mathf.InverseLerp(0f, maxDistance, num);
					float num2 = Mathf.Lerp(minimumDamageCoefficient, maximumDamageCoefficient, t);
					float num3 = Mathf.Lerp(minimumRadius, maximumRadius, t);
					BlastAttack obj = new BlastAttack
					{
						attacker = body.gameObject,
						inflictor = body.gameObject
					};
					obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
					obj.position = body.footPosition;
					obj.procCoefficient = 1f;
					obj.radius = num3;
					obj.baseForce = 200f * num2;
					obj.bonusForce = Vector3.up * 2000f;
					obj.baseDamage = body.damage * num2;
					obj.falloffModel = BlastAttack.FalloffModel.SweetSpot;
					obj.crit = Util.CheckRoll(body.crit, body.master);
					obj.damageColorIndex = DamageColorIndex.Item;
					obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
					obj.Fire();
					EffectData effectData = new EffectData();
					effectData.origin = body.footPosition;
					effectData.scale = num3;
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/BootShockwave"), effectData, transmit: true);
				}
			}
		}
		SetOnHitGroundProviderAuthority(null);
		outer.SetNextState(new HeadstompersCooldown());
	}
}
