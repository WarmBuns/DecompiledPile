using System;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(EffectComponent))]
public class CoinBehavior : MonoBehaviour
{
	[Serializable]
	public struct CoinTier
	{
		public ParticleSystem particleSystem;

		public int valuePerCoin;
	}

	public int originalCoinCount;

	public CoinTier[] coinTiers;

	private void Start()
	{
		originalCoinCount = (int)GetComponent<EffectComponent>().effectData.genericFloat;
		int num = originalCoinCount;
		for (int i = 0; i < coinTiers.Length; i++)
		{
			CoinTier coinTier = coinTiers[i];
			int num2 = 0;
			while (num >= coinTier.valuePerCoin)
			{
				num -= coinTier.valuePerCoin;
				num2++;
			}
			if (num2 > 0)
			{
				ParticleSystem.EmissionModule emission = coinTier.particleSystem.emission;
				emission.enabled = true;
				emission.SetBursts(new ParticleSystem.Burst[1]
				{
					new ParticleSystem.Burst(0f, num2)
				});
				coinTier.particleSystem.gameObject.SetActive(value: true);
			}
		}
	}
}
