using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.TitanMonster;

public class FireFist : BaseState
{
	private enum SubState
	{
		Prep,
		FireFist,
		Exit
	}

	private class Predictor
	{
		private enum ExtrapolationType
		{
			None,
			Linear,
			Polar
		}

		private Transform bodyTransform;

		private Transform targetTransform;

		private Vector3 targetPosition0;

		private Vector3 targetPosition1;

		private Vector3 targetPosition2;

		private int collectedPositions;

		public bool hasTargetTransform => targetTransform;

		public bool isPredictionReady => collectedPositions > 2;

		public Predictor(Transform bodyTransform)
		{
			this.bodyTransform = bodyTransform;
		}

		private void PushTargetPosition(Vector3 newTargetPosition)
		{
			targetPosition2 = targetPosition1;
			targetPosition1 = targetPosition0;
			targetPosition0 = newTargetPosition;
			collectedPositions++;
		}

		public void SetTargetTransform(Transform newTargetTransform)
		{
			targetTransform = newTargetTransform;
			targetPosition2 = (targetPosition1 = (targetPosition0 = newTargetTransform.position));
			collectedPositions = 1;
		}

		public void Update()
		{
			if ((bool)targetTransform)
			{
				PushTargetPosition(targetTransform.position);
			}
		}

		public bool GetPredictedTargetPosition(float time, out Vector3 predictedPosition)
		{
			Vector3 vector = targetPosition1 - targetPosition2;
			Vector3 vector2 = targetPosition0 - targetPosition1;
			vector.y = 0f;
			vector2.y = 0f;
			ExtrapolationType extrapolationType;
			if (vector == Vector3.zero || vector2 == Vector3.zero)
			{
				extrapolationType = ExtrapolationType.None;
			}
			else
			{
				Vector3 normalized = vector.normalized;
				Vector3 normalized2 = vector2.normalized;
				extrapolationType = ((Vector3.Dot(normalized, normalized2) > 0.98f) ? ExtrapolationType.Linear : ExtrapolationType.Polar);
			}
			float num = 1f / Time.deltaTime;
			predictedPosition = targetPosition0;
			switch (extrapolationType)
			{
			case ExtrapolationType.Linear:
				predictedPosition = targetPosition0 + vector2 * (time * num);
				break;
			case ExtrapolationType.Polar:
			{
				Vector3 position = bodyTransform.position;
				Vector3 vector3 = Util.Vector3XZToVector2XY(targetPosition2 - position);
				Vector3 vector4 = Util.Vector3XZToVector2XY(targetPosition1 - position);
				Vector3 vector5 = Util.Vector3XZToVector2XY(targetPosition0 - position);
				float magnitude = vector3.magnitude;
				float magnitude2 = vector4.magnitude;
				float magnitude3 = vector5.magnitude;
				float num2 = Vector2.SignedAngle(vector3, vector4) * num;
				float num3 = Vector2.SignedAngle(vector4, vector5) * num;
				float num4 = (magnitude2 - magnitude) * num;
				float num5 = (magnitude3 - magnitude2) * num;
				float num6 = (num2 + num3) * 0.5f;
				float num7 = (num4 + num5) * 0.5f;
				float num8 = magnitude3 + num7 * time;
				if (num8 < 0f)
				{
					num8 = 0f;
				}
				Vector2 vector6 = Util.RotateVector2(vector5, num6 * time);
				vector6 *= num8 * magnitude3;
				predictedPosition = position;
				predictedPosition.x += vector6.x;
				predictedPosition.z += vector6.y;
				break;
			}
			}
			if (Physics.Raycast(new Ray(predictedPosition + Vector3.up * 1f, Vector3.down), out var hitInfo, 200f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				predictedPosition = hitInfo.point;
				return true;
			}
			return false;
		}
	}

	public static float entryDuration = 1f;

	public static float fireDuration = 2f;

	public static float exitDuration = 1f;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public GameObject fistEffectPrefab;

	[SerializeField]
	public GameObject fireEffectPrefab;

	[SerializeField]
	public GameObject fistProjectilePrefab;

	public static float maxDistance = 40f;

	public static float trackingDuration = 0.5f;

	public static float fistDamageCoefficient = 2f;

	public static float fistForce = 2000f;

	public static string chargeFistAttackSoundString;

	private bool hasShownPrediction;

	private bool predictionOk;

	protected Vector3 predictedTargetPosition;

	private AimAnimator aimAnimator;

	private GameObject chargeEffect;

	private Transform fistTransform;

	private float stopwatch;

	private EffectData _effectData;

	protected BullseyeSearch enemyFinder;

	private EffectManagerHelper _emh_chargeEffect;

	private SubState subState;

	private Predictor predictor;

	private GameObject predictorDebug;

	private static int FireFistStateHash = Animator.StringToHash("FireFist");

	public override void Reset()
	{
		base.Reset();
		hasShownPrediction = false;
		predictionOk = false;
		predictedTargetPosition = Vector3.zero;
		aimAnimator = null;
		chargeEffect = null;
		fistTransform = null;
		stopwatch = 0f;
		subState = SubState.Prep;
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		if (enemyFinder != null)
		{
			enemyFinder.Reset();
		}
		predictor = null;
		predictorDebug = null;
		_emh_chargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		if ((bool)base.modelLocator)
		{
			ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
			aimAnimator = base.modelLocator.modelTransform.GetComponent<AimAnimator>();
			if ((bool)aimAnimator)
			{
				aimAnimator.enabled = true;
			}
			if ((bool)component)
			{
				fistTransform = component.FindChild("RightFist");
				if ((bool)fistTransform)
				{
					if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
					{
						chargeEffect = Object.Instantiate(chargeEffectPrefab, fistTransform);
					}
					else
					{
						_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, fistTransform, inResetLocal: true);
						chargeEffect = _emh_chargeEffect.gameObject;
					}
				}
			}
		}
		subState = SubState.Prep;
		PlayCrossfade("Body", "PrepFist", "PrepFist.playbackRate", entryDuration, 0.1f);
		Util.PlayAttackSpeedSound(chargeFistAttackSoundString, base.gameObject, attackSpeedStat);
		if (NetworkServer.active)
		{
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
			if ((bool)base.teamComponent)
			{
				bullseyeSearch.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
			}
			bullseyeSearch.maxDistanceFilter = maxDistance;
			bullseyeSearch.maxAngleFilter = 90f;
			Ray aimRay = GetAimRay();
			bullseyeSearch.searchOrigin = aimRay.origin;
			bullseyeSearch.searchDirection = aimRay.direction;
			bullseyeSearch.filterByLoS = false;
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
			bullseyeSearch.RefreshCandidates();
			HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
			if ((bool)hurtBox)
			{
				predictor = new Predictor(base.transform);
				predictor.SetTargetTransform(hurtBox.transform);
			}
		}
	}

	protected void DestroyChargeEffect()
	{
		if ((bool)chargeEffect)
		{
			if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
			{
				_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
			}
			else
			{
				EntityState.Destroy(chargeEffect);
			}
			chargeEffect = null;
			_emh_chargeEffect = null;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		DestroyChargeEffect();
		EntityState.Destroy(predictorDebug);
		predictorDebug = null;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		switch (subState)
		{
		case SubState.Prep:
			if (predictor != null)
			{
				predictor.Update();
			}
			if (stopwatch <= trackingDuration)
			{
				if (predictor != null)
				{
					predictionOk = predictor.GetPredictedTargetPosition(entryDuration - trackingDuration, out predictedTargetPosition);
					if (predictionOk && (bool)predictorDebug)
					{
						predictorDebug.transform.position = predictedTargetPosition;
					}
				}
			}
			else if (!hasShownPrediction)
			{
				hasShownPrediction = true;
				PlacePredictedAttack();
			}
			if (stopwatch >= entryDuration)
			{
				predictor = null;
				subState = SubState.FireFist;
				stopwatch = 0f;
				PlayAnimation("Body", FireFistStateHash);
				DestroyChargeEffect();
				if (!EffectManager.ShouldUsePooledEffect(fireEffectPrefab))
				{
					Object.Instantiate(fireEffectPrefab, fistTransform.position, Quaternion.identity, fistTransform);
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(fireEffectPrefab, fistTransform.position, Quaternion.identity, fistTransform);
				}
			}
			break;
		case SubState.FireFist:
			if (stopwatch >= fireDuration)
			{
				subState = SubState.Exit;
				stopwatch = 0f;
				PlayCrossfade("Body", "ExitFist", "ExitFist.playbackRate", exitDuration, 0.3f);
			}
			break;
		case SubState.Exit:
			if (stopwatch >= exitDuration && base.isAuthority)
			{
				outer.SetNextStateToMain();
			}
			break;
		}
	}

	protected virtual void PlacePredictedAttack()
	{
		PlaceSingleDelayBlast(predictedTargetPosition, 0f);
	}

	protected void PlaceSingleDelayBlast(Vector3 position, float delay)
	{
		if (base.isAuthority)
		{
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = fistProjectilePrefab;
			fireProjectileInfo.position = position;
			fireProjectileInfo.rotation = Quaternion.identity;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * fistDamageCoefficient;
			fireProjectileInfo.force = fistForce;
			fireProjectileInfo.crit = base.characterBody.RollCrit();
			fireProjectileInfo.fuseOverride = entryDuration - trackingDuration + delay;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
	}
}
