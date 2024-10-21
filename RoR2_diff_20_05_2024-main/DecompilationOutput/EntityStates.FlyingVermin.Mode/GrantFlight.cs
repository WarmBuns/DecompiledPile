using RoR2;

namespace EntityStates.FlyingVermin.Mode;

public class GrantFlight : BaseState
{
	protected ICharacterGravityParameterProvider characterGravityParameterProvider;

	protected ICharacterFlightParameterProvider characterFlightParameterProvider;

	public override void OnEnter()
	{
		base.OnEnter();
		characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
		characterFlightParameterProvider = base.gameObject.GetComponent<ICharacterFlightParameterProvider>();
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
	}

	public override void OnExit()
	{
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount--;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount--;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		base.OnExit();
	}
}
