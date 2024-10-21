namespace RoR2;

public interface ICharacterGravityParameterProvider
{
	CharacterGravityParameters gravityParameters { get; set; }

	bool useGravity { get; }

	bool isFlying { get; }
}
