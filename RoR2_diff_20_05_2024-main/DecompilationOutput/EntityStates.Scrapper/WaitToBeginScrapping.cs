namespace EntityStates.Scrapper;

public class WaitToBeginScrapping : ScrapperBaseState
{
	public static float duration;

	protected override bool enableInteraction => false;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new Scrapping());
		}
	}
}
