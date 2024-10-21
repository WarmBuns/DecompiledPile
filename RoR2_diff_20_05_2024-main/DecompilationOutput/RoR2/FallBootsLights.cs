using EntityStates.Headstompers;
using UnityEngine;

namespace RoR2;

public class FallBootsLights : MonoBehaviour
{
	public GameObject readyEffect;

	public GameObject triggerEffect;

	public GameObject chargingEffect;

	private GameObject readyEffectInstance;

	private GameObject triggerEffectInstance;

	private GameObject chargingEffectInstance;

	private bool isReady;

	private bool isTriggered;

	private bool isCharging;

	private CharacterModel characterModel;

	private EntityStateMachine sourceStateMachine;

	private void Start()
	{
		characterModel = GetComponentInParent<CharacterModel>();
		FindSourceStateMachine();
	}

	private void FindSourceStateMachine()
	{
		if ((bool)characterModel && (bool)characterModel.body)
		{
			sourceStateMachine = BaseHeadstompersState.FindForBody(characterModel.body)?.outer;
		}
	}

	private void Update()
	{
		if (!sourceStateMachine)
		{
			FindSourceStateMachine();
		}
		bool flag = (bool)sourceStateMachine && !(sourceStateMachine.state is HeadstompersCooldown);
		if (flag != isReady)
		{
			if (flag)
			{
				readyEffectInstance = Object.Instantiate(readyEffect, base.transform.position, base.transform.rotation, base.transform);
				Util.PlaySound("Play_item_proc_fallboots_activate", base.gameObject);
			}
			else if ((bool)readyEffectInstance)
			{
				Object.Destroy(readyEffectInstance);
			}
			isReady = flag;
		}
		bool flag2 = (bool)sourceStateMachine && sourceStateMachine.state is HeadstompersFall;
		if (flag2 != isTriggered)
		{
			if (flag2)
			{
				triggerEffectInstance = Object.Instantiate(triggerEffect, base.transform.position, base.transform.rotation, base.transform);
				Util.PlaySound("Play_item_proc_fallboots_activate", base.gameObject);
			}
			else if ((bool)triggerEffectInstance)
			{
				Object.Destroy(triggerEffectInstance);
			}
			isTriggered = flag2;
		}
		bool flag3 = (bool)sourceStateMachine && sourceStateMachine.state is HeadstompersCharge;
		if (flag3 != isCharging)
		{
			if (flag3)
			{
				chargingEffectInstance = Object.Instantiate(chargingEffect, base.transform.position, base.transform.rotation, base.transform);
			}
			else if ((bool)chargingEffectInstance)
			{
				Object.Destroy(chargingEffectInstance);
			}
			isCharging = flag3;
		}
	}
}
