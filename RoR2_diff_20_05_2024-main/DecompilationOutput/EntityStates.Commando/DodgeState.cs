using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Commando;

public class DodgeState : BaseState
{
	[SerializeField]
	public float duration = 0.9f;

	[SerializeField]
	public float initialSpeedCoefficient;

	[SerializeField]
	public float finalSpeedCoefficient;

	public static string dodgeSoundString;

	public static GameObject jetEffect;

	public static float dodgeFOV;

	public static int primaryReloadStockCount;

	private float rollSpeed;

	private Vector3 forwardDirection;

	private Animator animator;

	private Vector3 previousPosition;

	private static int DodgeForwardStateHash = Animator.StringToHash("DodgeForward");

	private static int DodgeBackwardStateHash = Animator.StringToHash("DodgeBackward");

	private static int DodgeRightStateHash = Animator.StringToHash("DodgeRight");

	private static int DodgeLeftStateHash = Animator.StringToHash("DodgeLeft");

	private static int DodgeParamHash = Animator.StringToHash("Dodge.playbackRate");

	public override void Reset()
	{
		base.Reset();
		rollSpeed = 0f;
		forwardDirection = Vector3.zero;
		animator = null;
		previousPosition = Vector3.zero;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(dodgeSoundString, base.gameObject);
		animator = GetModelAnimator();
		ChildLocator component = animator.GetComponent<ChildLocator>();
		if (base.isAuthority)
		{
			if ((bool)base.inputBank && (bool)base.characterDirection)
			{
				forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
			}
			if ((bool)base.skillLocator.primary)
			{
				base.skillLocator.primary.Reset();
				base.skillLocator.primary.stock = primaryReloadStockCount;
			}
		}
		Vector3 rhs = (base.characterDirection ? base.characterDirection.forward : forwardDirection);
		Vector3 rhs2 = Vector3.Cross(Vector3.up, rhs);
		float num = Vector3.Dot(forwardDirection, rhs);
		float num2 = Vector3.Dot(forwardDirection, rhs2);
		animator.SetFloat("forwardSpeed", num, 0.1f, GetDeltaTime());
		animator.SetFloat("rightSpeed", num2, 0.1f, GetDeltaTime());
		if (Mathf.Abs(num) > Mathf.Abs(num2))
		{
			PlayAnimation("Body", (num > 0f) ? DodgeForwardStateHash : DodgeBackwardStateHash, DodgeParamHash, duration);
		}
		else
		{
			PlayAnimation("Body", (num2 > 0f) ? DodgeRightStateHash : DodgeLeftStateHash, DodgeParamHash, duration);
		}
		if ((bool)jetEffect)
		{
			Transform transform = component.FindChild("LeftJet");
			Transform transform2 = component.FindChild("RightJet");
			bool flag = EffectManager.ShouldUsePooledEffect(jetEffect);
			if ((bool)transform)
			{
				if (!flag)
				{
					Object.Instantiate(jetEffect, transform);
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(jetEffect, transform, inResetLocal: true);
				}
			}
			if ((bool)transform2)
			{
				if (!flag)
				{
					Object.Instantiate(jetEffect, transform2);
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(jetEffect, transform2, inResetLocal: true);
				}
			}
		}
		RecalculateRollSpeed();
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			base.characterMotor.velocity.y = 0f;
			base.characterMotor.velocity = forwardDirection * rollSpeed;
		}
		Vector3 vector = (base.characterMotor ? base.characterMotor.velocity : Vector3.zero);
		previousPosition = base.transform.position - vector;
	}

	private void RecalculateRollSpeed()
	{
		rollSpeed = moveSpeedStat * Mathf.Lerp(initialSpeedCoefficient, finalSpeedCoefficient, base.fixedAge / duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		RecalculateRollSpeed();
		if ((bool)base.cameraTargetParams)
		{
			base.cameraTargetParams.fovOverride = Mathf.Lerp(dodgeFOV, 60f, base.fixedAge / duration);
		}
		Vector3 normalized = (base.transform.position - previousPosition).normalized;
		if ((bool)base.characterMotor && (bool)base.characterDirection && normalized != Vector3.zero)
		{
			Vector3 lhs = normalized * rollSpeed;
			float y = lhs.y;
			lhs.y = 0f;
			float num = Mathf.Max(Vector3.Dot(lhs, forwardDirection), 0f);
			lhs = forwardDirection * num;
			lhs.y += Mathf.Max(y, 0f);
			base.characterMotor.velocity = lhs;
		}
		previousPosition = base.transform.position;
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)base.cameraTargetParams)
		{
			base.cameraTargetParams.fovOverride = -1f;
		}
		base.OnExit();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(forwardDirection);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		forwardDirection = reader.ReadVector3();
	}
}
