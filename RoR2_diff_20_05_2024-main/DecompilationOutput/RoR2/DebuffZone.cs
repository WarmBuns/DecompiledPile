using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class DebuffZone : MonoBehaviour
{
	[Tooltip("The buff type to grant")]
	public BuffDef buffType;

	[Tooltip("The buff duration")]
	public float buffDuration;

	public string buffApplicationSoundString;

	public GameObject buffApplicationEffectPrefab;

	private void Awake()
	{
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkServer.active || !buffType)
		{
			return;
		}
		CharacterBody component = other.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			component.AddTimedBuff(buffType.buffIndex, buffDuration);
			Util.PlaySound(buffApplicationSoundString, component.gameObject);
			if ((bool)buffApplicationEffectPrefab)
			{
				EffectManager.SpawnEffect(buffApplicationEffectPrefab, new EffectData
				{
					origin = component.mainHurtBox.transform.position,
					scale = component.radius
				}, transmit: true);
			}
		}
	}
}
