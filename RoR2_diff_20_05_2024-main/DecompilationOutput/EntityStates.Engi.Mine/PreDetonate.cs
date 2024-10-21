using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class PreDetonate : BaseMineState
{
	public static float duration;

	public static string pathToPrepForExplosionChildEffect;

	public static float detachForce;

	protected override bool shouldStick => false;

	protected override bool shouldRevertToWaitForStickOnSurfaceLost => false;

	public override void OnEnter()
	{
		base.OnEnter();
		base.transform.Find(pathToPrepForExplosionChildEffect).gameObject.SetActive(value: true);
		base.rigidbody.AddForce(base.transform.forward * detachForce);
		base.rigidbody.AddTorque(Random.onUnitSphere * 200f);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && duration <= base.fixedAge)
		{
			outer.SetNextState(new Detonate());
		}
	}
}
