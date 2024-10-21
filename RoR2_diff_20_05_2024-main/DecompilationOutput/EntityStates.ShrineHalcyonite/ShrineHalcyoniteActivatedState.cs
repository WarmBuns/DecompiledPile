using RoR2;
using UnityEngine;

namespace EntityStates.ShrineHalcyonite;

public class ShrineHalcyoniteActivatedState : ShrineHalcyoniteBaseState
{
	public float shrineAnimationDuration = 1f;

	public float shrineDelayAge;

	public Vector3 shrineEndLocation = new Vector3(0f, 0f, 0f);

	private float shrineMovementTimer;

	private GameObject modelBase;

	public override void OnEnter()
	{
		base.OnEnter();
		Transform transform = parentShrineReference.modelChildLocator.FindChild("ModelBase");
		if ((bool)transform)
		{
			modelBase = transform.gameObject;
		}
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HalcyonResurfaceVFX"), new EffectData
		{
			origin = base.gameObject.transform.position
		}, transmit: true);
		Util.PlaySound("Play_obj_shrineHalcyonite_activate", base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		shrineMovementTimer -= Time.deltaTime;
		if (!(shrineMovementTimer <= 0f))
		{
			return;
		}
		if (modelBase.transform.localPosition != shrineEndLocation)
		{
			shrineMovementTimer += 0.05f;
			modelBase.transform.localPosition += new Vector3(0f, 0.1f, 0f);
			if (modelBase.transform.localPosition == shrineEndLocation)
			{
				Util.PlaySound("Play_obj_shrineHalcyonite_activate_finish", base.gameObject);
			}
		}
		else
		{
			shrineDelayAge += Time.deltaTime;
			if (shrineDelayAge >= shrineAnimationDuration)
			{
				outer.SetNextState(new ShrineHalcyoniteNoQuality());
			}
		}
	}
}
