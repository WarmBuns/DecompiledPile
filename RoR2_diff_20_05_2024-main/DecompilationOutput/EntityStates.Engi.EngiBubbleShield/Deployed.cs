using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.EngiBubbleShield;

public class Deployed : EntityState
{
	public static string childLocatorString;

	public static string initialSoundString;

	public static string destroySoundString;

	public static float delayToDeploy;

	public static float lifetime;

	[SerializeField]
	public GameObject destroyEffectPrefab;

	[SerializeField]
	public float destroyEffectRadius;

	private bool hasDeployed;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(initialSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!hasDeployed && base.fixedAge >= delayToDeploy)
		{
			ChildLocator component = GetComponent<ChildLocator>();
			if ((bool)component)
			{
				component.FindChild(childLocatorString).gameObject.SetActive(value: true);
				hasDeployed = true;
			}
		}
		if (base.fixedAge >= lifetime && NetworkServer.active)
		{
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		EffectManager.SpawnEffect(destroyEffectPrefab, new EffectData
		{
			origin = base.transform.position,
			rotation = base.transform.rotation,
			scale = destroyEffectRadius
		}, transmit: false);
		Util.PlaySound(destroySoundString, base.gameObject);
	}
}
