using RoR2;
using UnityEngine;

namespace EntityStates.GolemMonster;

public class SpawnState : GenericCharacterSpawnState
{
	private GameObject eyeGameObject;

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			modelTransform.GetComponent<PrintController>().enabled = true;
		}
		eyeGameObject = FindModelChild("Eye")?.gameObject;
		if ((bool)eyeGameObject)
		{
			eyeGameObject.SetActive(value: false);
		}
		if (base.characterBody.HasBuff(DLC2Content.Buffs.EliteAurelionite))
		{
			PrintController component = base.modelLocator.modelTransform.gameObject.GetComponent<PrintController>();
			component.enabled = false;
			component.printTime = duration;
			component.startingPrintHeight = -101.78f;
			component.maxPrintHeight = -93f;
			component.startingPrintBias = 2f;
			component.maxPrintBias = 0.95f;
			component.disableWhenFinished = true;
			component.printCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
			component.enabled = true;
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying && (bool)eyeGameObject)
		{
			eyeGameObject.SetActive(value: true);
		}
	}
}
