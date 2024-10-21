using UnityEngine;

namespace RoR2;

public class SnailAnimator : MonoBehaviour
{
	public ParticleSystem healEffectSystem;

	private bool lastOutOfDanger;

	private Animator animator;

	private CharacterModel characterModel;

	private static int spawnParamHash = Animator.StringToHash("spawn");

	private static int hideParamHash = Animator.StringToHash("hide");

	private void Start()
	{
		animator = GetComponent<Animator>();
		characterModel = GetComponentInParent<CharacterModel>();
	}

	private void FixedUpdate()
	{
		if (!characterModel)
		{
			return;
		}
		CharacterBody body = characterModel.body;
		if ((bool)body)
		{
			bool outOfDanger = body.outOfDanger;
			if (outOfDanger && !lastOutOfDanger)
			{
				animator.SetBool(spawnParamHash, value: true);
				animator.SetBool(hideParamHash, value: false);
				Util.PlaySound("Play_item_proc_slug_emerge", characterModel.gameObject);
				ParticleSystem.MainModule main = healEffectSystem.main;
				main.loop = true;
				healEffectSystem.Play();
			}
			else if (!outOfDanger && lastOutOfDanger)
			{
				animator.SetBool(hideParamHash, value: true);
				animator.SetBool(spawnParamHash, value: false);
				Util.PlaySound("Play_item_proc_slug_hide", characterModel.gameObject);
				ParticleSystem.MainModule main2 = healEffectSystem.main;
				main2.loop = false;
			}
			lastOutOfDanger = outOfDanger;
		}
	}
}
