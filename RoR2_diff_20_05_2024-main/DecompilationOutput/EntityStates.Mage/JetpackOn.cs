using UnityEngine;

namespace EntityStates.Mage;

public class JetpackOn : BaseState
{
	public static float hoverVelocity;

	public static float hoverAcceleration;

	private Transform jetOnEffect;

	protected static Vector3 _tempVec3 = Vector3.one;

	public override void Reset()
	{
		base.Reset();
		jetOnEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		jetOnEffect = FindModelChild("JetOn");
		if ((bool)jetOnEffect)
		{
			jetOnEffect.gameObject.SetActive(value: true);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			float y = base.characterMotor.velocity.y;
			y = Mathf.MoveTowards(y, hoverVelocity, hoverAcceleration * GetDeltaTime());
			base.characterMotor.velocity = new Vector3(base.characterMotor.velocity.x, y, base.characterMotor.velocity.z);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)jetOnEffect)
		{
			jetOnEffect.gameObject.SetActive(value: false);
		}
	}
}
