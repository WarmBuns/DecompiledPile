using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class TeleportOutController : NetworkBehaviour
{
	[NonSerialized]
	[SyncVar]
	public GameObject target;

	public ParticleSystem bodyGlowParticles;

	public static string tpOutSoundString = "Play_UI_teleport_off_map";

	private float fixedAge;

	private const float warmupDuration = 2f;

	public float delayBeforePlayingSFX = 1f;

	private bool hasPlayedSFX;

	private NetworkInstanceId ___targetNetId;

	public GameObject Networktarget
	{
		get
		{
			return target;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref target, 1u, ref ___targetNetId);
		}
	}

	public static void AddTPOutEffect(CharacterModel characterModel, float beginAlpha, float endAlpha, float duration)
	{
		if ((bool)characterModel)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(characterModel.gameObject);
			temporaryOverlayInstance.duration = duration;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, beginAlpha, 1f, endAlpha);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matTPInOut");
			temporaryOverlayInstance.AddToCharacterModel(characterModel);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if ((bool)target)
		{
			ModelLocator component = target.GetComponent<ModelLocator>();
			if ((bool)component)
			{
				Transform modelTransform = component.modelTransform;
				if ((bool)modelTransform)
				{
					CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
					if ((bool)component2)
					{
						AddTPOutEffect(component2, 0f, 1f, 2f);
						if (component2.baseRendererInfos.Length != 0)
						{
							Renderer renderer = component2.baseRendererInfos[component2.baseRendererInfos.Length - 1].renderer;
							if ((bool)renderer)
							{
								ParticleSystem.ShapeModule shape = bodyGlowParticles.shape;
								if (renderer is MeshRenderer)
								{
									shape.shapeType = ParticleSystemShapeType.MeshRenderer;
									shape.meshRenderer = renderer as MeshRenderer;
								}
								else if (renderer is SkinnedMeshRenderer)
								{
									shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
									shape.skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
								}
							}
						}
					}
				}
			}
		}
		bodyGlowParticles.Play();
	}

	public void FixedUpdate()
	{
		fixedAge += Time.fixedDeltaTime;
		if (fixedAge >= delayBeforePlayingSFX && !hasPlayedSFX)
		{
			hasPlayedSFX = true;
			Util.PlaySound(tpOutSoundString, target);
		}
		if (NetworkServer.active && fixedAge >= 2f && (bool)target)
		{
			GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(target);
			if ((bool)teleportEffectPrefab)
			{
				EffectManager.SpawnEffect(teleportEffectPrefab, new EffectData
				{
					origin = target.transform.position
				}, transmit: true);
			}
			UnityEngine.Object.Destroy(target);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(target);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(target);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			___targetNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			target = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___targetNetId.IsEmpty())
		{
			Networktarget = ClientScene.FindLocalObject(___targetNetId);
		}
	}
}
