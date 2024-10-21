namespace RoR2.PostProcessing;

public abstract class ScreenDamageCalculator
{
	public static int TypeCounter;

	public abstract ScreenDamageData screenDamageData { get; }

	public abstract void CalculateScreenDamage(ScreenDamage screenDamage, HealthComponent healthComponent);

	public abstract void End();
}
