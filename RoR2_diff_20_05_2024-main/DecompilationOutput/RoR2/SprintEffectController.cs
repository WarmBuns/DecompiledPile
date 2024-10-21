using UnityEngine;

namespace RoR2;

public class SprintEffectController : MonoBehaviour
{
	public ParticleSystem[] loopSystems;

	public GameObject loopRootObject;

	public bool mustBeGrounded;

	public bool mustBeVisible;

	public CharacterBody characterBody;

	public CharacterModel characterModel;

	private void Awake()
	{
		if ((bool)characterBody && Util.IsPrefab(characterBody.gameObject) && !Util.IsPrefab(base.gameObject))
		{
			characterBody = null;
		}
	}

	private void FixedUpdate()
	{
		bool flag = true;
		if ((bool)characterModel)
		{
			flag = characterModel.visibility > VisibilityLevel.Cloaked;
		}
		if (!characterBody)
		{
			return;
		}
		if ((bool)characterBody.characterMotor && characterBody.healthComponent.alive && characterBody.isSprinting && (!mustBeGrounded || characterBody.characterMotor.isGrounded) && (!mustBeVisible || flag))
		{
			if ((bool)loopRootObject)
			{
				loopRootObject.SetActive(value: true);
			}
			for (int i = 0; i < loopSystems.Length; i++)
			{
				ParticleSystem particleSystem = loopSystems[i];
				ParticleSystem.MainModule main = particleSystem.main;
				main.loop = true;
				if (!particleSystem.isPlaying)
				{
					particleSystem.Play();
				}
			}
		}
		else
		{
			if ((bool)loopRootObject)
			{
				loopRootObject.SetActive(value: false);
			}
			for (int j = 0; j < loopSystems.Length; j++)
			{
				ParticleSystem.MainModule main2 = loopSystems[j].main;
				main2.loop = false;
			}
		}
	}
}
