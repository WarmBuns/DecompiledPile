using UnityEngine;

namespace RoR2;

public class NoGravZone : MonoBehaviour
{
	public void OnTriggerEnter(Collider other)
	{
		ICharacterGravityParameterProvider component = other.GetComponent<ICharacterGravityParameterProvider>();
		if (component != null)
		{
			CharacterGravityParameters gravityParameters = component.gravityParameters;
			gravityParameters.environmentalAntiGravityGranterCount++;
			component.gravityParameters = gravityParameters;
		}
		ICharacterFlightParameterProvider component2 = other.GetComponent<ICharacterFlightParameterProvider>();
		if (component2 != null)
		{
			CharacterFlightParameters flightParameters = component2.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			component2.flightParameters = flightParameters;
		}
	}

	public void OnTriggerExit(Collider other)
	{
		ICharacterFlightParameterProvider component = other.GetComponent<ICharacterFlightParameterProvider>();
		if (component != null)
		{
			CharacterFlightParameters flightParameters = component.flightParameters;
			flightParameters.channeledFlightGranterCount--;
			component.flightParameters = flightParameters;
		}
		ICharacterGravityParameterProvider component2 = other.GetComponent<ICharacterGravityParameterProvider>();
		if (component2 != null)
		{
			CharacterGravityParameters gravityParameters = component2.gravityParameters;
			gravityParameters.environmentalAntiGravityGranterCount--;
			component2.gravityParameters = gravityParameters;
		}
	}
}
