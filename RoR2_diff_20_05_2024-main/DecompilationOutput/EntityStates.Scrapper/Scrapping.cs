using RoR2;
using UnityEngine;

namespace EntityStates.Scrapper;

public class Scrapping : ScrapperBaseState
{
	public static string enterSoundString;

	public static string exitSoundString;

	public static float duration;

	private static int ScrappingStateHash = Animator.StringToHash("Scrapping");

	private static int ScrappingParamHash = Animator.StringToHash("Scrapping.playbackRate");

	protected override bool enableInteraction => false;

	public override void OnEnter()
	{
		base.OnEnter();
		FindModelChild("ScrappingEffect").gameObject.SetActive(value: true);
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Base", ScrappingStateHash, ScrappingParamHash, duration);
	}

	public override void OnExit()
	{
		FindModelChild("ScrappingEffect").gameObject.SetActive(value: false);
		Util.PlaySound(exitSoundString, base.gameObject);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new ScrappingToIdle());
		}
	}
}
