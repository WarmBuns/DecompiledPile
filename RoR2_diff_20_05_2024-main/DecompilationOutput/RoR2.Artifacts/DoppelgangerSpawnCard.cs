using RoR2.CharacterAI;
using UnityEngine;

namespace RoR2.Artifacts;

public class DoppelgangerSpawnCard : MasterCopySpawnCard
{
	public static DoppelgangerSpawnCard FromMaster(CharacterMaster srcCharacterMaster)
	{
		if (!srcCharacterMaster || !srcCharacterMaster.GetBody())
		{
			return null;
		}
		DoppelgangerSpawnCard doppelgangerSpawnCard = ScriptableObject.CreateInstance<DoppelgangerSpawnCard>();
		MasterCopySpawnCard.CopyDataFromMaster(doppelgangerSpawnCard, srcCharacterMaster, copyItems: true, copyEquipment: true);
		doppelgangerSpawnCard.GiveItem(RoR2Content.Items.InvadingDoppelganger);
		doppelgangerSpawnCard.onPreSpawnSetup = OnPreSpawnSetup;
		return doppelgangerSpawnCard;
		void OnPreSpawnSetup(CharacterMaster spawnedMaster)
		{
			BaseAI ai = spawnedMaster.GetComponent<BaseAI>();
			CharacterBody srcBody = srcCharacterMaster.GetBody();
			ai.onBodyDiscovered += SetEnemyToOriginator;
			void SetEnemyToOriginator(CharacterBody body)
			{
				ai.currentEnemy.gameObject = srcBody.gameObject;
				ai.onBodyDiscovered -= SetEnemyToOriginator;
			}
		}
	}
}
