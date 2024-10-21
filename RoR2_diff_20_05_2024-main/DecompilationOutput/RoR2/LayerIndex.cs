using UnityEngine;

namespace RoR2;

public struct LayerIndex
{
	public static class CommonMasks
	{
		public static readonly LayerMask bullet = (int)world.mask | (int)entityPrecise.mask;

		public static readonly LayerMask laser = (int)world.mask | (int)characterBodiesOrDefault | (int)entityPrecise.mask;

		public static readonly LayerMask interactable = (int)defaultLayer.mask | (int)world.mask | (int)pickups.mask;

		public static readonly LayerMask characterBodies = (int)playerBody.mask | (int)enemyBody.mask;

		public static readonly LayerMask characterBodiesOrDefault = (int)defaultLayer.mask | (int)characterBodies;

		public static readonly LayerMask fakeActorLayers = (int)fakeActor.mask | (int)playerFakeActor.mask;
	}

	public int intVal;

	private static uint assignedLayerMask;

	public static readonly LayerIndex invalidLayer;

	public static readonly LayerIndex defaultLayer;

	public static readonly LayerIndex transparentFX;

	public static readonly LayerIndex ignoreRaycast;

	public static readonly LayerIndex water;

	public static readonly LayerIndex ui;

	public static readonly LayerIndex fakeActor;

	public static readonly LayerIndex playerFakeActor;

	public static readonly LayerIndex noCollision;

	public static readonly LayerIndex pickups;

	public static readonly LayerIndex world;

	public static readonly LayerIndex entityPrecise;

	public static readonly LayerIndex debris;

	public static readonly LayerIndex projectile;

	public static readonly LayerIndex manualRender;

	public static readonly LayerIndex collideWithCharacterHullOnly;

	public static readonly LayerIndex ragdoll;

	public static readonly LayerIndex noDraw;

	public static readonly LayerIndex prefabBrush;

	public static readonly LayerIndex postProcess;

	public static readonly LayerIndex uiWorldSpace;

	public static readonly LayerIndex projectileWorldOnly;

	public static readonly string modderMessage;

	public static readonly LayerIndex playerBody;

	public static readonly LayerIndex enemyBody;

	public static readonly LayerIndex triggerZone;

	public static readonly LayerIndex enemyBarrier;

	private static readonly LayerMask[] collisionMasks;

	public LayerMask mask => (intVal >= 0) ? (1 << intVal) : intVal;

	public LayerMask collisionMask => collisionMasks[intVal];

	static LayerIndex()
	{
		assignedLayerMask = 0u;
		invalidLayer = new LayerIndex
		{
			intVal = -1
		};
		defaultLayer = GetLayerIndex("Default");
		transparentFX = GetLayerIndex("TransparentFX");
		ignoreRaycast = GetLayerIndex("Ignore Raycast");
		water = GetLayerIndex("Water");
		ui = GetLayerIndex("UI");
		fakeActor = GetLayerIndex("FakeActor");
		playerFakeActor = GetLayerIndex("PlayerFakeActor");
		noCollision = GetLayerIndex("NoCollision");
		pickups = GetLayerIndex("Pickups");
		world = GetLayerIndex("World");
		entityPrecise = GetLayerIndex("EntityPrecise");
		debris = GetLayerIndex("Debris");
		projectile = GetLayerIndex("Projectile");
		manualRender = GetLayerIndex("ManualRender");
		collideWithCharacterHullOnly = GetLayerIndex("CollideWithCharacterHullOnly");
		ragdoll = GetLayerIndex("Ragdoll");
		noDraw = GetLayerIndex("NoDraw");
		prefabBrush = GetLayerIndex("PrefabBrush");
		postProcess = GetLayerIndex("PostProcess");
		uiWorldSpace = GetLayerIndex("UI, WorldSpace");
		projectileWorldOnly = GetLayerIndex("ProjectileWorldOnly");
		modderMessage = "Layers below are used only by console versions.";
		playerBody = GetLayerIndex("PlayerBody");
		enemyBody = GetLayerIndex("EnemyBody");
		triggerZone = GetLayerIndex("TriggerZone");
		enemyBarrier = GetLayerIndex("EnemyBarrier");
		collisionMasks = CalcCollisionMasks();
		for (int i = 0; i < 32; i++)
		{
			string text = LayerMask.LayerToName(i);
			if (text != "" && (assignedLayerMask & (uint)(1 << i)) == 0)
			{
				Debug.LogWarningFormat("Layer \"{0}\" is defined in this project's \"Tags and Layers\" settings but is not defined in LayerIndex!", text);
			}
		}
	}

	private static LayerIndex GetLayerIndex(string layerName)
	{
		LayerIndex layerIndex = default(LayerIndex);
		layerIndex.intVal = LayerMask.NameToLayer(layerName);
		LayerIndex result = layerIndex;
		if (result.intVal == invalidLayer.intVal)
		{
			Debug.LogErrorFormat("Layer \"{0}\" is not defined in this project's \"Tags and Layers\" settings.", layerName);
		}
		else
		{
			assignedLayerMask |= (uint)(1 << result.intVal);
		}
		return result;
	}

	private static LayerMask[] CalcCollisionMasks()
	{
		LayerMask[] array = new LayerMask[32];
		for (int i = 0; i < 32; i++)
		{
			LayerMask layerMask = default(LayerMask);
			for (int j = 0; j < 32; j++)
			{
				if (!Physics.GetIgnoreLayerCollision(i, j))
				{
					layerMask = (int)layerMask | (1 << j);
				}
			}
			array[i] = layerMask;
		}
		return array;
	}

	public static int GetAppropriateLayerForTeam(TeamIndex teamIndex)
	{
		switch (teamIndex)
		{
		case TeamIndex.Player:
			return playerBody.intVal;
		case TeamIndex.Monster:
		case TeamIndex.Lunar:
			return enemyBody.intVal;
		default:
			return defaultLayer.intVal;
		}
	}

	public static LayerIndex GetAppropriateFakeLayerForTeam(TeamIndex teamIndex)
	{
		if (teamIndex == TeamIndex.Player)
		{
			return playerFakeActor;
		}
		return fakeActor;
	}
}
