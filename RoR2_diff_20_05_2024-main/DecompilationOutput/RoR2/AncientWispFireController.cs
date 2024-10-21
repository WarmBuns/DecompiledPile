using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(CharacterModel))]
public class AncientWispFireController : MonoBehaviour
{
	public ParticleSystem normalParticles;

	public Light normalLight;

	public ParticleSystem rageParticles;

	public Light rageLight;

	private CharacterModel characterModel;

	private void Awake()
	{
		characterModel = GetComponent<CharacterModel>();
	}

	private void Update()
	{
		bool flag = false;
		CharacterBody body = characterModel.body;
		if ((bool)body)
		{
			flag = body.HasBuff(JunkContent.Buffs.EnrageAncientWisp);
		}
		if ((bool)normalParticles)
		{
			ParticleSystem.EmissionModule emission = normalParticles.emission;
			if (emission.enabled == flag)
			{
				emission.enabled = !flag;
				if (!flag)
				{
					normalParticles.Play();
				}
			}
		}
		if ((bool)rageParticles)
		{
			ParticleSystem.EmissionModule emission2 = rageParticles.emission;
			if (emission2.enabled != flag)
			{
				emission2.enabled = flag;
				if (flag)
				{
					rageParticles.Play();
				}
			}
		}
		if ((bool)normalLight)
		{
			normalLight.enabled = !flag;
		}
		if ((bool)rageLight)
		{
			rageLight.enabled = flag;
		}
	}
}
