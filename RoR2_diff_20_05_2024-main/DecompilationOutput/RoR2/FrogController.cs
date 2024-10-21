using UnityEngine;

namespace RoR2;

public class FrogController : MonoBehaviour
{
	[SerializeField]
	private int maxPets;

	[SerializeField]
	private PortalSpawner portalSpawner;

	[SerializeField]
	private PurchaseInteraction purchaseInteraction;

	[SerializeField]
	private string petChatToken;

	private int petCount;

	public void Pet(Interactor interactor)
	{
		petCount++;
		if (!string.IsNullOrEmpty(petChatToken))
		{
			Chat.SendBroadcastChat(new SubjectChatMessage
			{
				subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
				baseToken = petChatToken
			});
		}
		if (petCount >= maxPets && (bool)portalSpawner)
		{
			portalSpawner.AttemptSpawnPortalServer();
		}
	}
}
