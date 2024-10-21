using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SpawnCards/InteractableSpawnCard")]
public class InteractableSpawnCard : SpawnCard
{
	[Tooltip("Whether or not to orient the object to the normal of the ground it spawns on.")]
	public bool orientToFloor;

	[Tooltip("Slightly tweaks the rotation for things like chests and barrels so it looks more natural.")]
	public bool slightlyRandomizeOrientation;

	public bool skipSpawnWhenSacrificeArtifactEnabled;

	[Tooltip("When Sacrifice is enabled, this is multiplied by the card's weight")]
	public float weightScalarWhenSacrificeArtifactEnabled = 1f;

	public bool skipSpawnWhenDevotionArtifactEnabled;

	[Tooltip("Won't spawn more than this many per stage.  If it's negative, there's no cap")]
	public int maxSpawnsPerStage = -1;

	[Range(0f, 1f)]
	[SerializeField]
	[Tooltip("When playing Primatic Trials, this interactable will have a required check to see if it is supposed to spawn. 0 is will not spawn, 1 will 100% spawn and will be default.")]
	public float prismaticTrialSpawnChance = 1f;

	private static readonly float floorOffset = 3f;

	private static readonly float raycastLength = 6f;

	private static readonly Xoroshiro128Plus rng = new Xoroshiro128Plus(0uL);

	protected override void Spawn(Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnResult result)
	{
		ulong nextUlong = directorSpawnRequest.rng.nextUlong;
		rng.ResetSeed(nextUlong);
		if (skipSpawnWhenSacrificeArtifactEnabled && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.sacrificeArtifactDef))
		{
			return;
		}
		GameObject gameObject = Object.Instantiate(prefab, position, rotation);
		Transform transform = gameObject.transform;
		if (orientToFloor)
		{
			Vector3 up = gameObject.transform.up;
			if (Physics.Raycast(new Ray(position + up * floorOffset, -up), out var hitInfo, raycastLength + floorOffset, LayerIndex.world.mask))
			{
				transform.up = hitInfo.normal;
			}
		}
		transform.Rotate(Vector3.up, rng.RangeFloat(0f, 360f), Space.Self);
		if (slightlyRandomizeOrientation)
		{
			transform.Translate(Vector3.down * 0.3f, Space.Self);
			transform.rotation *= Quaternion.Euler(rng.RangeFloat(-30f, 30f), rng.RangeFloat(-30f, 30f), rng.RangeFloat(-30f, 30f));
		}
		NetworkServer.Spawn(gameObject);
		result.spawnedInstance = gameObject;
		result.success = true;
	}
}
