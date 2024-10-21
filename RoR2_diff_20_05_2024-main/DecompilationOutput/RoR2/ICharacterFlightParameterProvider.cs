namespace RoR2;

public interface ICharacterFlightParameterProvider
{
	CharacterFlightParameters flightParameters { get; set; }

	bool isFlying { get; }
}
