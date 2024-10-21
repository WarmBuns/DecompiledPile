using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(CharacterMaster))]
public class MasterDropDroplet : MonoBehaviour
{
	private CharacterMaster characterMaster;

	public PickupDropTable[] dropTables;

	public ulong salt;

	[Header("Deprecated")]
	public SerializablePickupIndex[] pickupsToDrop;

	private Xoroshiro128Plus rng;

	private void Start()
	{
		characterMaster = GetComponent<CharacterMaster>();
		rng = new Xoroshiro128Plus(Run.instance.seed ^ salt);
	}

	public void DropItems()
	{
		CharacterBody body = characterMaster.GetBody();
		if (!body)
		{
			return;
		}
		PickupDropTable[] array = dropTables;
		foreach (PickupDropTable pickupDropTable in array)
		{
			if ((bool)pickupDropTable)
			{
				PickupDropletController.CreatePickupDroplet(pickupDropTable.GenerateDrop(rng), body.coreTransform.position, new Vector3(Random.Range(-4f, 4f), 20f, Random.Range(-4f, 4f)));
			}
		}
		SerializablePickupIndex[] array2 = pickupsToDrop;
		for (int i = 0; i < array2.Length; i++)
		{
			PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(array2[i].pickupName), body.coreTransform.position, new Vector3(Random.Range(-4f, 4f), 20f, Random.Range(-4f, 4f)));
		}
	}
}
