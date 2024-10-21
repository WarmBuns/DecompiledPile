using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using Rewired;
using RoR2.Networking;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public static class Util
{
	private struct EasyTargetCandidate
	{
		public Transform transform;

		public float score;

		public float distance;
	}

	public const string attackSpeedRtpcName = "attackSpeed";

	private static readonly string strBackslash = "\\";

	private static readonly string strBackslashBackslash = strBackslash + strBackslash;

	private static readonly string strQuote = "\"";

	private static readonly string strBackslashQuote = strBackslash + strQuote;

	private static readonly Regex backlashSearch = new Regex(strBackslashBackslash);

	private static readonly Regex quoteSearch = new Regex(strQuote);

	public static readonly DateTime dateZero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

	public static bool useCachedNetWriter = false;

	public static bool breakInVerify = true;

	public static bool logVerify = true;

	public static bool cacheBulletHitHurtBox = true;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	private static readonly Stack<string> sharedStringStack = new Stack<string>();

	private static readonly string cloneString = "(Clone)";

	public static void CleanseBody(CharacterBody characterBody, bool removeDebuffs, bool removeBuffs, bool removeCooldownBuffs, bool removeDots, bool removeStun, bool removeNearbyProjectiles)
	{
		if (removeDebuffs || removeBuffs || removeCooldownBuffs)
		{
			BuffIndex buffIndex = (BuffIndex)0;
			for (BuffIndex buffCount = (BuffIndex)BuffCatalog.buffCount; buffIndex < buffCount; buffIndex++)
			{
				BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
				if ((buffDef.isDebuff && removeDebuffs) || (buffDef.isCooldown && removeCooldownBuffs) || (!buffDef.isDebuff && !buffDef.isCooldown && removeBuffs))
				{
					characterBody.ClearTimedBuffs(buffIndex);
				}
			}
		}
		if (removeDots)
		{
			DotController.RemoveAllDots(characterBody.gameObject);
		}
		if (removeStun)
		{
			SetStateOnHurt component = characterBody.GetComponent<SetStateOnHurt>();
			if ((bool)component)
			{
				component.Cleanse();
			}
		}
		if (!removeNearbyProjectiles)
		{
			return;
		}
		float num = 6f * 6f;
		TeamIndex teamIndex = characterBody.teamComponent.teamIndex;
		List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
		List<ProjectileController> list = new List<ProjectileController>();
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			ProjectileController projectileController = instancesList[i];
			if (!projectileController.cannotBeDeleted && projectileController.teamFilter.teamIndex != teamIndex && (projectileController.transform.position - characterBody.corePosition).sqrMagnitude < num)
			{
				list.Add(projectileController);
			}
		}
		int j = 0;
		for (int count2 = list.Count; j < count2; j++)
		{
			ProjectileController projectileController2 = list[j];
			if ((bool)projectileController2)
			{
				UnityEngine.Object.Destroy(projectileController2.gameObject);
			}
		}
	}

	public static WeightedSelection<DirectorCard> CreateReasonableDirectorCardSpawnList(float availableCredit, int maximumNumberToSpawnBeforeSkipping, int minimumToSpawn)
	{
		WeightedSelection<DirectorCard> monsterSelection = ClassicStageInfo.instance.monsterSelection;
		WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>();
		for (int i = 0; i < monsterSelection.Count; i++)
		{
			DirectorCard value = monsterSelection.choices[i].value;
			float combatDirectorHighestEliteCostMultiplier = CombatDirector.CalcHighestEliteCostMultiplier(value.spawnCard.eliteRules);
			if (DirectorCardIsReasonableChoice(availableCredit, maximumNumberToSpawnBeforeSkipping, minimumToSpawn, value, combatDirectorHighestEliteCostMultiplier))
			{
				weightedSelection.AddChoice(value, monsterSelection.choices[i].weight);
			}
		}
		return weightedSelection;
	}

	public static bool DirectorCardIsReasonableChoice(float availableCredit, int maximumNumberToSpawnBeforeSkipping, int minimumToSpawn, DirectorCard card, float combatDirectorHighestEliteCostMultiplier)
	{
		float num = (float)(card.cost * maximumNumberToSpawnBeforeSkipping) * ((card.spawnCard as CharacterSpawnCard).noElites ? 1f : combatDirectorHighestEliteCostMultiplier);
		if (card.IsAvailable() && (float)card.cost * (float)minimumToSpawn <= availableCredit)
		{
			return num / 2f > availableCredit;
		}
		return false;
	}

	public static CharacterBody HurtBoxColliderToBody(Collider collider)
	{
		HurtBox component = collider.GetComponent<HurtBox>();
		if ((bool)component && (bool)component.healthComponent)
		{
			return component.healthComponent.body;
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ConvertAmplificationPercentageIntoReductionPercentage(float amplificationPercentage)
	{
		return (1f - 100f / (100f + amplificationPercentage)) * 100f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ConvertAmplificationPercentageIntoReductionNormalized(float amplificationNormal)
	{
		return (1f - 1f / (1f + amplificationNormal)) * 1f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float NonlinearLerp(float min, float max, float amplifactionNormalixed)
	{
		return Mathf.Lerp(min, max, ConvertAmplificationPercentageIntoReductionNormalized(amplifactionNormalixed));
	}

	public static Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
	{
		Vector3 rhs = vPoint - vA;
		Vector3 normalized = (vB - vA).normalized;
		float num = Vector3.Distance(vA, vB);
		float num2 = Vector3.Dot(normalized, rhs);
		if (num2 <= 0f)
		{
			return vA;
		}
		if (num2 >= num)
		{
			return vB;
		}
		Vector3 vector = normalized * num2;
		return vA + vector;
	}

	public static CharacterBody TryToCreateGhost(CharacterBody targetBody, CharacterBody ownerBody, int duration)
	{
		if (!NetworkServer.active)
		{
			UnityEngine.Debug.LogWarning("[Server] function 'RoR2.CharacterBody RoR2.Util::TryToCreateGhost(RoR2.CharacterBody, RoR2.CharacterBody, int)' called on client");
			return null;
		}
		if (!targetBody)
		{
			return null;
		}
		GameObject bodyPrefab = BodyCatalog.FindBodyPrefab(targetBody);
		if (!bodyPrefab)
		{
			return null;
		}
		CharacterMaster characterMaster = MasterCatalog.allAiMasters.FirstOrDefault((CharacterMaster master) => master.bodyPrefab == bodyPrefab);
		if (!characterMaster)
		{
			return null;
		}
		MasterSummon obj = new MasterSummon
		{
			masterPrefab = characterMaster.gameObject,
			ignoreTeamMemberLimit = false,
			position = targetBody.footPosition
		};
		CharacterDirection component = targetBody.GetComponent<CharacterDirection>();
		obj.rotation = (component ? Quaternion.Euler(0f, component.yaw, 0f) : targetBody.transform.rotation);
		obj.summonerBodyObject = (ownerBody ? ownerBody.gameObject : null);
		obj.inventoryToCopy = targetBody.inventory;
		obj.useAmbientLevel = null;
		obj.preSpawnSetupCallback = (Action<CharacterMaster>)Delegate.Combine(obj.preSpawnSetupCallback, new Action<CharacterMaster>(PreSpawnSetup));
		CharacterMaster characterMaster2 = obj.Perform();
		if (!characterMaster2)
		{
			return null;
		}
		CharacterBody body = characterMaster2.GetBody();
		if ((bool)body)
		{
			EntityStateMachine[] components = body.GetComponents<EntityStateMachine>();
			foreach (EntityStateMachine obj2 in components)
			{
				obj2.initialStateType = obj2.mainStateType;
			}
		}
		return body;
		void PreSpawnSetup(CharacterMaster newMaster)
		{
			newMaster.inventory.GiveItem(RoR2Content.Items.Ghost);
			newMaster.inventory.GiveItem(RoR2Content.Items.HealthDecay, duration);
			newMaster.inventory.GiveItem(RoR2Content.Items.BoostDamage, 150);
		}
	}

	public static float OnHitProcDamage(float damageThatProccedIt, float damageStat, float damageCoefficient)
	{
		float b = damageThatProccedIt * damageCoefficient;
		return Mathf.Max(1f, b);
	}

	public static float OnKillProcDamage(float baseDamage, float damageCoefficient)
	{
		return baseDamage * damageCoefficient;
	}

	public static Quaternion QuaternionSafeLookRotation(Vector3 forward)
	{
		Quaternion result = Quaternion.identity;
		if (forward.sqrMagnitude > Mathf.Epsilon && forward != Vector3.zero)
		{
			result = Quaternion.LookRotation(forward);
		}
		return result;
	}

	public static Quaternion QuaternionSafeLookRotation(Vector3 forward, Vector3 upwards)
	{
		Quaternion result = Quaternion.identity;
		if (forward.sqrMagnitude > Mathf.Epsilon)
		{
			result = Quaternion.LookRotation(forward, upwards);
		}
		return result;
	}

	public static bool HasParameterOfType(Animator animator, string name, AnimatorControllerParameterType type)
	{
		AnimatorControllerParameter[] parameters = animator.parameters;
		foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
		{
			if (animatorControllerParameter.type == type && animatorControllerParameter.name == name)
			{
				return true;
			}
		}
		return false;
	}

	public static uint PlaySound(string soundString, GameObject gameObject)
	{
		if (string.IsNullOrEmpty(soundString))
		{
			return 0u;
		}
		if (gameObject == null)
		{
			return AkSoundEngine.PostEvent(soundString, ulong.MaxValue);
		}
		return AkSoundEngine.PostEvent(soundString, gameObject);
	}

	public static uint PlaySound(string soundString, GameObject gameObject, string RTPCstring, float RTPCvalue)
	{
		uint num = PlaySound(soundString, gameObject);
		if (num != 0)
		{
			AkSoundEngine.SetRTPCValueByPlayingID(RTPCstring, RTPCvalue, num);
		}
		return num;
	}

	public static uint PlayAttackSpeedSound(string soundString, GameObject gameObject, float attackSpeedStat)
	{
		uint num = PlaySound(soundString, gameObject);
		if (num != 0)
		{
			float in_value = CalculateAttackSpeedRtpcValue(attackSpeedStat);
			AkSoundEngine.SetRTPCValueByPlayingID("attackSpeed", in_value, num);
		}
		return num;
	}

	public static float CalculateAttackSpeedRtpcValue(float attackSpeedStat)
	{
		float num = Mathf.Log(attackSpeedStat, 2f);
		return 1200f * num / 96f + 50f;
	}

	public static void RotateAwayFromWalls(float raycastLength, int raycastCount, Vector3 raycastOrigin, Transform referenceTransform)
	{
		float num = 360f / (float)raycastCount;
		float angle = 0f;
		float num2 = 0f;
		for (int i = 0; i < raycastCount; i++)
		{
			Vector3 direction = Quaternion.Euler(0f, num * (float)i, 0f) * Vector3.forward;
			float num3 = raycastLength;
			if (Physics.Raycast(raycastOrigin, direction, out var hitInfo, raycastLength, LayerIndex.world.mask))
			{
				num3 = hitInfo.distance;
			}
			if (hitInfo.distance > num2)
			{
				angle = num * (float)i;
				num2 = num3;
			}
		}
		referenceTransform.Rotate(Vector3.up, angle, Space.Self);
	}

	public static string GetActionDisplayString(ActionElementMap actionElementMap)
	{
		if (actionElementMap == null)
		{
			return "";
		}
		return actionElementMap.elementIdentifierName switch
		{
			"Left Mouse Button" => "M1", 
			"Right Mouse Button" => "M2", 
			"Left Shift" => "Shift", 
			_ => actionElementMap.elementIdentifierName, 
		};
	}

	public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
	{
		return Mathf.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2)) * 57.29578f;
	}

	public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
	{
		return outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin);
	}

	public static bool HasAnimationParameter(string paramName, Animator animator)
	{
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == paramName)
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasAnimationParameter(int paramHash, AnimatorControllerParameter[] parameters)
	{
		int i = 0;
		for (int num = parameters.Length; i < num; i++)
		{
			if (parameters[i].nameHash == paramHash)
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasAnimationParameter(int paramHash, Animator animator)
	{
		return HasAnimationParameter(paramHash, animator.parameters);
	}

	public static bool CheckRoll(float percentChance, float luck = 0f, CharacterMaster effectOriginMaster = null)
	{
		if (percentChance <= 0f)
		{
			return false;
		}
		int num = Mathf.CeilToInt(Mathf.Abs(luck));
		float num2 = UnityEngine.Random.Range(0f, 100f);
		float num3 = num2;
		for (int i = 0; i < num; i++)
		{
			float b = UnityEngine.Random.Range(0f, 100f);
			num2 = ((luck > 0f) ? Mathf.Min(num2, b) : Mathf.Max(num2, b));
		}
		if (num2 <= percentChance)
		{
			if (num3 > percentChance && (bool)effectOriginMaster)
			{
				GameObject bodyObject = effectOriginMaster.GetBodyObject();
				if ((bool)bodyObject)
				{
					CharacterBody component = bodyObject.GetComponent<CharacterBody>();
					if ((bool)component)
					{
						component.wasLucky = true;
					}
				}
			}
			return true;
		}
		return false;
	}

	public static bool CheckRoll(float percentChance, CharacterMaster master)
	{
		return CheckRoll(percentChance, master ? master.luck : 0f, master);
	}

	public static float EstimateSurfaceDistance(Collider a, Collider b)
	{
		Vector3 center = a.bounds.center;
		Vector3 center2 = b.bounds.center;
		RaycastHit hitInfo;
		Vector3 a2 = ((!b.Raycast(new Ray(center, center2 - center), out hitInfo, float.PositiveInfinity)) ? b.ClosestPointOnBounds(center) : hitInfo.point);
		Vector3 b2 = ((!a.Raycast(new Ray(center2, center - center2), out hitInfo, float.PositiveInfinity)) ? a.ClosestPointOnBounds(center2) : hitInfo.point);
		return Vector3.Distance(a2, b2);
	}

	public static bool HasEffectiveAuthority(GameObject gameObject)
	{
		if ((bool)gameObject)
		{
			return HasEffectiveAuthority(gameObject.GetComponent<NetworkIdentity>());
		}
		return false;
	}

	public static bool HasEffectiveAuthority(NetworkIdentity networkIdentity)
	{
		if ((bool)networkIdentity)
		{
			if (!networkIdentity.hasAuthority)
			{
				if (NetworkServer.active)
				{
					return networkIdentity.clientAuthorityOwner == null;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static float CalculateSphereVolume(float radius)
	{
		return 4.1887903f * radius * radius * radius;
	}

	public static float CalculateCylinderVolume(float radius, float height)
	{
		return MathF.PI * radius * radius * height;
	}

	public static float CalculateColliderVolume(Collider collider)
	{
		Vector3 lossyScale = collider.transform.lossyScale;
		float num = lossyScale.x * lossyScale.y * lossyScale.z;
		float num2 = 0f;
		if (collider is BoxCollider)
		{
			Vector3 size = ((BoxCollider)collider).size;
			num2 = size.x * size.y * size.z;
		}
		else if (collider is SphereCollider)
		{
			num2 = CalculateSphereVolume(((SphereCollider)collider).radius);
		}
		else if (collider is CapsuleCollider)
		{
			CapsuleCollider obj = (CapsuleCollider)collider;
			float radius = obj.radius;
			float num3 = CalculateSphereVolume(radius);
			float num4 = Mathf.Max(obj.height - num3, 0f);
			float num5 = MathF.PI * radius * radius * num4;
			num2 = num3 + num5;
		}
		else if (collider is CharacterController)
		{
			CharacterController obj2 = (CharacterController)collider;
			float radius2 = obj2.radius;
			float num6 = CalculateSphereVolume(radius2);
			float num7 = Mathf.Max(obj2.height - num6, 0f);
			float num8 = MathF.PI * radius2 * radius2 * num7;
			num2 = num6 + num8;
		}
		return num2 * num;
	}

	public static Vector3 RandomColliderVolumePoint(Collider collider)
	{
		Transform transform = collider.transform;
		Vector3 position = Vector3.zero;
		if (collider is BoxCollider)
		{
			BoxCollider obj = (BoxCollider)collider;
			Vector3 size = obj.size;
			Vector3 center = obj.center;
			position = new Vector3(center.x + UnityEngine.Random.Range(size.x * -0.5f, size.x * 0.5f), center.y + UnityEngine.Random.Range(size.y * -0.5f, size.y * 0.5f), center.z + UnityEngine.Random.Range(size.z * -0.5f, size.z * 0.5f));
		}
		else if (collider is SphereCollider)
		{
			SphereCollider sphereCollider = (SphereCollider)collider;
			position = sphereCollider.center + UnityEngine.Random.insideUnitSphere * sphereCollider.radius;
		}
		else if (collider is CapsuleCollider)
		{
			CapsuleCollider capsuleCollider = (CapsuleCollider)collider;
			float radius = capsuleCollider.radius;
			float num = Mathf.Max(capsuleCollider.height - radius, 0f);
			float num2 = CalculateSphereVolume(radius);
			float num3 = CalculateCylinderVolume(radius, num);
			float maxInclusive = num2 + num3;
			if (UnityEngine.Random.Range(0f, maxInclusive) <= num2)
			{
				position = UnityEngine.Random.insideUnitSphere * radius;
				float num4 = ((float)UnityEngine.Random.Range(0, 2) * 2f - 1f) * num * 0.5f;
				switch (capsuleCollider.direction)
				{
				case 0:
					position.x += num4;
					break;
				case 1:
					position.y += num4;
					break;
				case 2:
					position.z += num4;
					break;
				}
			}
			else
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * radius;
				float num5 = UnityEngine.Random.Range(num * -0.5f, num * 0.5f);
				switch (capsuleCollider.direction)
				{
				case 0:
					position = new Vector3(num5, vector.x, vector.y);
					break;
				case 1:
					position = new Vector3(vector.x, num5, vector.y);
					break;
				case 2:
					position = new Vector3(vector.x, vector.y, num5);
					break;
				}
			}
			position += capsuleCollider.center;
		}
		else if (collider is CharacterController)
		{
			CharacterController characterController = (CharacterController)collider;
			float radius2 = characterController.radius;
			float num6 = Mathf.Max(characterController.height - radius2, 0f);
			float num7 = CalculateSphereVolume(radius2);
			float num8 = CalculateCylinderVolume(radius2, num6);
			float maxInclusive2 = num7 + num8;
			if (UnityEngine.Random.Range(0f, maxInclusive2) <= num7)
			{
				position = UnityEngine.Random.insideUnitSphere * radius2;
				float num9 = ((float)UnityEngine.Random.Range(0, 2) * 2f - 1f) * num6 * 0.5f;
				position.y += num9;
			}
			else
			{
				Vector2 vector2 = UnityEngine.Random.insideUnitCircle * radius2;
				float y = UnityEngine.Random.Range(num6 * -0.5f, num6 * 0.5f);
				position = new Vector3(vector2.x, y, vector2.y);
			}
			position += characterController.center;
		}
		return transform.TransformPoint(position);
	}

	public static CharacterBody GetFriendlyEasyTarget(CharacterBody casterBody, Ray aimRay, float maxDistance, float maxDeviation = 20f)
	{
		TeamIndex teamIndex = TeamIndex.Neutral;
		TeamComponent component = casterBody.GetComponent<TeamComponent>();
		if ((bool)component)
		{
			teamIndex = component.teamIndex;
		}
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
		Vector3 origin = aimRay.origin;
		Vector3 direction = aimRay.direction;
		List<EasyTargetCandidate> candidatesList = new List<EasyTargetCandidate>(teamMembers.Count);
		List<int> list = new List<int>(teamMembers.Count);
		float num = Mathf.Cos(maxDeviation * (MathF.PI / 180f));
		for (int i = 0; i < teamMembers.Count; i++)
		{
			Transform transform = teamMembers[i].transform;
			Vector3 vector = transform.position - origin;
			float magnitude = vector.magnitude;
			float num2 = Vector3.Dot(vector * (1f / magnitude), direction);
			CharacterBody component2 = transform.GetComponent<CharacterBody>();
			if (num2 >= num && component2 != casterBody)
			{
				float num3 = 1f / magnitude;
				float score = num2 + num3;
				candidatesList.Add(new EasyTargetCandidate
				{
					transform = transform,
					score = score,
					distance = magnitude
				});
				list.Add(list.Count);
			}
		}
		list.Sort(delegate(int a, int b)
		{
			float score2 = candidatesList[a].score;
			float score3 = candidatesList[b].score;
			return (score2 != score3) ? ((!(score2 > score3)) ? 1 : (-1)) : 0;
		});
		for (int j = 0; j < list.Count; j++)
		{
			int index = list[j];
			CharacterBody component3 = candidatesList[index].transform.GetComponent<CharacterBody>();
			if ((bool)component3 && component3 != casterBody)
			{
				return component3;
			}
		}
		return null;
	}

	public static CharacterBody GetEnemyEasyTarget(CharacterBody casterBody, Ray aimRay, float maxDistance, float maxDeviation = 20f)
	{
		TeamIndex teamIndex = TeamIndex.Neutral;
		TeamComponent component = casterBody.GetComponent<TeamComponent>();
		if ((bool)component)
		{
			teamIndex = component.teamIndex;
		}
		List<TeamComponent> list = new List<TeamComponent>();
		for (TeamIndex teamIndex2 = TeamIndex.Neutral; teamIndex2 < TeamIndex.Count; teamIndex2++)
		{
			if (teamIndex2 != teamIndex)
			{
				list.AddRange(TeamComponent.GetTeamMembers(teamIndex2));
			}
		}
		Vector3 origin = aimRay.origin;
		Vector3 direction = aimRay.direction;
		List<EasyTargetCandidate> candidatesList = new List<EasyTargetCandidate>(list.Count);
		List<int> list2 = new List<int>(list.Count);
		float num = Mathf.Cos(maxDeviation * (MathF.PI / 180f));
		for (int i = 0; i < list.Count; i++)
		{
			Transform transform = list[i].transform;
			Vector3 vector = transform.position - origin;
			float magnitude = vector.magnitude;
			float num2 = Vector3.Dot(vector * (1f / magnitude), direction);
			CharacterBody component2 = transform.GetComponent<CharacterBody>();
			if (num2 >= num && component2 != casterBody && magnitude < maxDistance)
			{
				float num3 = 1f / magnitude;
				float score = num2 + num3;
				candidatesList.Add(new EasyTargetCandidate
				{
					transform = transform,
					score = score,
					distance = magnitude
				});
				list2.Add(list2.Count);
			}
		}
		list2.Sort(delegate(int a, int b)
		{
			float score2 = candidatesList[a].score;
			float score3 = candidatesList[b].score;
			return (score2 != score3) ? ((!(score2 > score3)) ? 1 : (-1)) : 0;
		});
		for (int j = 0; j < list2.Count; j++)
		{
			int index = list2[j];
			CharacterBody component3 = candidatesList[index].transform.GetComponent<CharacterBody>();
			if ((bool)component3 && component3 != casterBody)
			{
				return component3;
			}
		}
		return null;
	}

	public static float GetBodyPrefabFootOffset(GameObject prefab)
	{
		CapsuleCollider component = prefab.GetComponent<CapsuleCollider>();
		if ((bool)component)
		{
			return component.height * 0.5f - component.center.y;
		}
		return 0f;
	}

	public static void ShuffleList<T>(List<T> list)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = UnityEngine.Random.Range(0, num + 1);
			T value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
	}

	public static void ShuffleList<T>(List<T> list, Xoroshiro128Plus rng)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = rng.RangeInt(0, num + 1);
			T value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
	}

	public static void ShuffleArray<T>(T[] array)
	{
		for (int num = array.Length - 1; num > 0; num--)
		{
			int num2 = UnityEngine.Random.Range(0, num + 1);
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
	}

	public static void ShuffleArray<T>(T[] array, Xoroshiro128Plus rng)
	{
		for (int num = array.Length - 1; num > 0; num--)
		{
			int num2 = rng.RangeInt(0, num + 1);
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
	}

	public static Transform FindNearest(Vector3 position, List<Transform> transformsList, float range = float.PositiveInfinity)
	{
		Transform result = null;
		float num = range * range;
		for (int i = 0; i < transformsList.Count; i++)
		{
			float sqrMagnitude = (transformsList[i].position - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = transformsList[i];
			}
		}
		return result;
	}

	public static Vector3 ApplySpread(Vector3 aimDirection, float minSpread, float maxSpread, float spreadYawScale, float spreadPitchScale, float bonusYaw = 0f, float bonusPitch = 0f)
	{
		Vector3 up = Vector3.up;
		Vector3 axis = Vector3.Cross(up, aimDirection);
		float x = UnityEngine.Random.Range(minSpread, maxSpread);
		float z = UnityEngine.Random.Range(0f, 360f);
		Vector3 vector = Quaternion.Euler(0f, 0f, z) * (Quaternion.Euler(x, 0f, 0f) * Vector3.forward);
		float y = vector.y;
		vector.y = 0f;
		float angle = (Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f + bonusYaw) * spreadYawScale;
		float angle2 = (Mathf.Atan2(y, vector.magnitude) * 57.29578f + bonusPitch) * spreadPitchScale;
		return Quaternion.AngleAxis(angle, up) * (Quaternion.AngleAxis(angle2, axis) * aimDirection);
	}

	public static string GenerateColoredString(string str, Color32 color)
	{
		return string.Format(CultureInfo.InvariantCulture, "<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", color.r, color.g, color.b, str);
	}

	public static bool GuessRenderBounds(GameObject gameObject, out Bounds bounds)
	{
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
		if (componentsInChildren.Length != 0)
		{
			bounds = componentsInChildren[0].bounds;
			for (int i = 1; i < componentsInChildren.Length; i++)
			{
				bounds.Encapsulate(componentsInChildren[i].bounds);
			}
			return true;
		}
		bounds = new Bounds(gameObject.transform.position, Vector3.zero);
		return false;
	}

	public static bool GuessRenderBoundsMeshOnly(GameObject gameObject, out Bounds bounds)
	{
		Renderer[] array = (from renderer in gameObject.GetComponentsInChildren<Renderer>()
			where renderer is MeshRenderer || renderer is SkinnedMeshRenderer
			select renderer).ToArray();
		if (array.Length != 0)
		{
			bounds = array[0].bounds;
			for (int i = 1; i < array.Length; i++)
			{
				bounds.Encapsulate(array[i].bounds);
			}
			return true;
		}
		bounds = new Bounds(gameObject.transform.position, Vector3.zero);
		return false;
	}

	public static GameObject FindNetworkObject(NetworkInstanceId networkInstanceId)
	{
		if (NetworkServer.active)
		{
			return NetworkServer.FindLocalObject(networkInstanceId);
		}
		return ClientScene.FindLocalObject(networkInstanceId);
	}

	public static string GetGameObjectHierarchyName(GameObject gameObject)
	{
		int num = 0;
		Transform transform = gameObject.transform;
		while ((bool)transform)
		{
			num++;
			transform = transform.parent;
		}
		string[] array = new string[num];
		Transform transform2 = gameObject.transform;
		while ((bool)transform2)
		{
			array[--num] = transform2.gameObject.name;
			transform2 = transform2.parent;
		}
		return string.Join("/", array);
	}

	public static string GetBestBodyName(GameObject bodyObject)
	{
		CharacterBody characterBody = null;
		string text = "???";
		if ((bool)bodyObject)
		{
			characterBody = bodyObject.GetComponent<CharacterBody>();
			if ((bool)characterBody)
			{
				text = characterBody.GetUserName();
			}
			else
			{
				IDisplayNameProvider component = bodyObject.GetComponent<IDisplayNameProvider>();
				if (component != null)
				{
					text = component.GetDisplayName();
				}
			}
		}
		string text2 = text;
		if ((bool)characterBody)
		{
			if (characterBody.isElite)
			{
				BuffIndex[] eliteBuffIndices = BuffCatalog.eliteBuffIndices;
				foreach (BuffIndex buffIndex in eliteBuffIndices)
				{
					if (characterBody.HasBuff(buffIndex))
					{
						text2 = Language.GetStringFormatted(BuffCatalog.GetBuffDef(buffIndex).eliteDef.modifierToken, text2);
					}
				}
			}
			if ((bool)characterBody.inventory)
			{
				if (characterBody.inventory.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0)
				{
					text2 = Language.GetStringFormatted("BODY_MODIFIER_DOPPELGANGER", text2);
				}
				if (characterBody.inventory.GetItemCount(DLC1Content.Items.GummyCloneIdentifier) > 0)
				{
					text2 = Language.GetStringFormatted("BODY_MODIFIER_GUMMYCLONE", text2);
				}
			}
		}
		return text2;
	}

	public static string GetBestBodyNameColored(GameObject bodyObject)
	{
		if ((bool)bodyObject)
		{
			CharacterBody component = bodyObject.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				CharacterMaster master = component.master;
				if ((bool)master)
				{
					PlayerCharacterMasterController component2 = master.GetComponent<PlayerCharacterMasterController>();
					if ((bool)component2)
					{
						GameObject networkUserObject = component2.networkUserObject;
						if ((bool)networkUserObject)
						{
							NetworkUser component3 = networkUserObject.GetComponent<NetworkUser>();
							if ((bool)component3)
							{
								return GenerateColoredString(component3.userName, component3.userColor);
							}
						}
					}
				}
			}
			IDisplayNameProvider component4 = bodyObject.GetComponent<IDisplayNameProvider>();
			if (component4 != null)
			{
				return component4.GetDisplayName();
			}
		}
		return "???";
	}

	public static string GetBestMasterName(CharacterMaster characterMaster)
	{
		if ((bool)characterMaster)
		{
			string text = characterMaster.playerCharacterMasterController?.networkUser?.userName;
			if (text == null)
			{
				string text2 = characterMaster.bodyPrefab?.GetComponent<CharacterBody>().baseNameToken;
				if (text2 != null)
				{
					text = Language.GetString(text2);
				}
			}
			return text;
		}
		return "Null Master";
	}

	public static NetworkUser LookUpBodyNetworkUser(GameObject bodyObject)
	{
		if ((bool)bodyObject)
		{
			return LookUpBodyNetworkUser(bodyObject.GetComponent<CharacterBody>());
		}
		return null;
	}

	public static NetworkUser LookUpBodyNetworkUser(CharacterBody characterBody)
	{
		if ((bool)characterBody)
		{
			CharacterMaster master = characterBody.master;
			if ((bool)master)
			{
				PlayerCharacterMasterController component = master.GetComponent<PlayerCharacterMasterController>();
				if ((bool)component)
				{
					GameObject networkUserObject = component.networkUserObject;
					if ((bool)networkUserObject)
					{
						NetworkUser component2 = networkUserObject.GetComponent<NetworkUser>();
						if ((bool)component2)
						{
							return component2;
						}
					}
				}
			}
		}
		return null;
	}

	private static bool HandleCharacterPhysicsCastResults(GameObject bodyObject, Ray ray, int numHits, RaycastHit[] hits, out RaycastHit hitInfo)
	{
		int num = -1;
		float num2 = float.PositiveInfinity;
		for (int i = 0; i < numHits; i++)
		{
			if (bodyObject == hits[i].collider.gameObject)
			{
				continue;
			}
			float distance = hits[i].distance;
			if (!(distance < num2))
			{
				continue;
			}
			HurtBox component = hits[i].collider.GetComponent<HurtBox>();
			if ((bool)component)
			{
				HealthComponent healthComponent = component.healthComponent;
				if ((bool)healthComponent && healthComponent.gameObject == bodyObject)
				{
					continue;
				}
			}
			if (distance == 0f)
			{
				hitInfo = hits[i];
				hitInfo.point = ray.origin;
				return true;
			}
			num = i;
			num2 = distance;
		}
		if (num == -1)
		{
			hitInfo = default(RaycastHit);
			return false;
		}
		hitInfo = hits[num];
		return true;
	}

	public static bool CharacterRaycast(GameObject bodyObject, Ray ray, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
	{
		int num = 0;
		num = HGPhysics.RaycastAll(out var hits, ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
		bool result = HandleCharacterPhysicsCastResults(bodyObject, ray, num, hits, out hitInfo);
		HGPhysics.ReturnResults(hits);
		return result;
	}

	public static bool CharacterSpherecast(GameObject bodyObject, Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
	{
		RaycastHit[] hits;
		int numHits = HGPhysics.SphereCastAll(out hits, ray.origin, radius, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
		bool result = HandleCharacterPhysicsCastResults(bodyObject, ray, numHits, hits, out hitInfo);
		HGPhysics.ReturnResults(hits);
		return result;
	}

	public static bool ConnectionIsLocal([NotNull] NetworkConnection conn)
	{
		if (conn is SteamNetworkConnection)
		{
			return false;
		}
		if (conn is EOSNetworkConnection)
		{
			return false;
		}
		return conn.GetType() != typeof(NetworkConnection);
	}

	public static string EscapeRichTextForTextMeshPro(string rtString)
	{
		string text = rtString;
		text = text.Replace("<", "</noparse><noparse><</noparse><noparse>");
		return "<noparse>" + text + "</noparse>";
	}

	public static string EscapeQuotes(string str)
	{
		str = backlashSearch.Replace(str, strBackslashBackslash);
		str = quoteSearch.Replace(str, strBackslashQuote);
		return str;
	}

	public static string RGBToHex(Color32 rgb)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0:X2}{1:X2}{2:X2}", rgb.r, rgb.g, rgb.b);
	}

	public static Vector2 Vector3XZToVector2XY(Vector3 vector3)
	{
		return new Vector2(vector3.x, vector3.z);
	}

	public static Vector2 Vector3XZToVector2XY(ref Vector3 vector3)
	{
		return new Vector2(vector3.x, vector3.z);
	}

	public static void Vector3XZToVector2XY(Vector3 vector3, out Vector2 vector2)
	{
		vector2.x = vector3.x;
		vector2.y = vector3.z;
	}

	public static void Vector3XZToVector2XY(ref Vector3 vector3, out Vector2 vector2)
	{
		vector2.x = vector3.x;
		vector2.y = vector3.z;
	}

	public static Vector2 RotateVector2(Vector2 vector2, float degrees)
	{
		float num = Mathf.Sin(degrees * (MathF.PI / 180f));
		float num2 = Mathf.Cos(degrees * (MathF.PI / 180f));
		return new Vector2(num2 * vector2.x - num * vector2.y, num * vector2.x + num2 * vector2.y);
	}

	public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
	{
		float target2 = Quaternion.Angle(current, target);
		target2 = Mathf.SmoothDamp(0f, target2, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
		return Quaternion.RotateTowards(current, target, target2);
	}

	public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref float currentVelocity, float smoothTime)
	{
		float target2 = Quaternion.Angle(current, target);
		target2 = Mathf.SmoothDamp(0f, target2, ref currentVelocity, smoothTime);
		return Quaternion.RotateTowards(current, target, target2);
	}

	public static HurtBox FindBodyMainHurtBox(CharacterBody characterBody)
	{
		return characterBody.mainHurtBox;
	}

	public static HurtBox FindBodyMainHurtBox(GameObject bodyObject)
	{
		CharacterBody component = bodyObject.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			return FindBodyMainHurtBox(component);
		}
		return null;
	}

	public static Vector3 GetCorePosition(CharacterBody characterBody)
	{
		return characterBody.corePosition;
	}

	public static Vector3 GetCorePosition(GameObject bodyObject)
	{
		CharacterBody component = bodyObject.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			return GetCorePosition(component);
		}
		return bodyObject.transform.position;
	}

	public static Transform GetCoreTransform(GameObject bodyObject)
	{
		CharacterBody component = bodyObject.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			return component.coreTransform;
		}
		return bodyObject.transform;
	}

	public static float SphereRadiusToVolume(float radius)
	{
		return 4.1887903f * (radius * radius * radius);
	}

	public static float SphereVolumeToRadius(float volume)
	{
		return Mathf.Pow(3f * volume / (MathF.PI * 4f), 1f / 3f);
	}

	public static void CopyList<T>(List<T> src, List<T> dest)
	{
		dest.Clear();
		foreach (T item in src)
		{
			dest.Add(item);
		}
	}

	public static float ScanCharacterAnimationClipForMomentOfRootMotionStop(GameObject characterPrefab, string clipName, string rootBoneNameInChildLocator)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(characterPrefab);
		Transform modelTransform = gameObject.GetComponent<ModelLocator>().modelTransform;
		Transform transform = modelTransform.GetComponent<ChildLocator>().FindChild(rootBoneNameInChildLocator);
		AnimationClip animationClip = modelTransform.GetComponent<Animator>().runtimeAnimatorController.animationClips.FirstOrDefault((AnimationClip c) => c.name == clipName);
		float result = 1f;
		animationClip.SampleAnimation(gameObject, 0f);
		Vector3 vector = transform.position;
		for (float num = 0.1f; num < 1f; num += 0.1f)
		{
			animationClip.SampleAnimation(gameObject, num);
			Vector3 position = transform.position;
			if ((position - vector).magnitude == 0f)
			{
				result = num;
				break;
			}
			vector = position;
		}
		UnityEngine.Object.Destroy(gameObject);
		return result;
	}

	public static void DebugCross(Vector3 position, float radius, UnityEngine.Color color, float duration)
	{
		UnityEngine.Debug.DrawLine(position - Vector3.right * radius, position + Vector3.right * radius, color, duration);
		UnityEngine.Debug.DrawLine(position - Vector3.up * radius, position + Vector3.up * radius, color, duration);
		UnityEngine.Debug.DrawLine(position - Vector3.forward * radius, position + Vector3.forward * radius, color, duration);
	}

	public static void DebugBounds(Bounds bounds, UnityEngine.Color color, float duration)
	{
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		Vector3 start = new Vector3(min.x, min.y, min.z);
		Vector3 vector = new Vector3(min.x, min.y, max.z);
		Vector3 vector2 = new Vector3(min.x, max.y, min.z);
		Vector3 end = new Vector3(min.x, max.y, max.z);
		Vector3 vector3 = new Vector3(max.x, min.y, min.z);
		Vector3 vector4 = new Vector3(max.x, min.y, max.z);
		Vector3 end2 = new Vector3(max.x, max.y, min.z);
		Vector3 start2 = new Vector3(max.x, max.y, max.z);
		UnityEngine.Debug.DrawLine(start, vector, color, duration);
		UnityEngine.Debug.DrawLine(start, vector3, color, duration);
		UnityEngine.Debug.DrawLine(start, vector2, color, duration);
		UnityEngine.Debug.DrawLine(vector2, end, color, duration);
		UnityEngine.Debug.DrawLine(vector2, end2, color, duration);
		UnityEngine.Debug.DrawLine(start2, end, color, duration);
		UnityEngine.Debug.DrawLine(start2, end2, color, duration);
		UnityEngine.Debug.DrawLine(start2, vector4, color, duration);
		UnityEngine.Debug.DrawLine(vector4, vector3, color, duration);
		UnityEngine.Debug.DrawLine(vector4, vector, color, duration);
		UnityEngine.Debug.DrawLine(vector, end, color, duration);
		UnityEngine.Debug.DrawLine(vector3, end2, color, duration);
	}

	public static bool PositionIsValid(Vector3 value)
	{
		float f = value.x + value.y + value.z;
		if (!float.IsInfinity(f))
		{
			return !float.IsNaN(f);
		}
		return false;
	}

	public static void Swap<T>(ref T a, ref T b)
	{
		T val = a;
		a = b;
		b = val;
	}

	public static DateTime UnixTimeStampToDateTimeUtc(uint unixTimeStamp)
	{
		DateTime dateTime = dateZero;
		return dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
	}

	public static double GetCurrentUnixEpochTimeInSeconds()
	{
		return Client.Instance.Utils.GetServerRealTime();
	}

	public static bool IsValid(UnityEngine.Object o)
	{
		return o;
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyComponent<T>(T a, T b) where T : Component
	{
		if (a != b)
		{
			if (logVerify)
			{
				UnityEngine.Debug.LogErrorFormat("Components are not the same!  {0} != {1}", (a != null) ? a.ToString() : "null", (b != null) ? b.ToString() : "null");
			}
			if (breakInVerify)
			{
				UnityEngine.Debug.DebugBreak();
			}
		}
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyNullComponent(Component a)
	{
		if (a != null)
		{
			if (logVerify)
			{
				UnityEngine.Debug.LogErrorFormat("Components is not null!  {0} != null", a.ToString());
			}
			if (breakInVerify)
			{
				UnityEngine.Debug.DebugBreak();
			}
		}
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyNonNullComponent<T>(T a, string desc) where T : Component
	{
		if (a == null)
		{
			if (logVerify)
			{
				UnityEngine.Debug.LogErrorFormat("Component is null!  {0}: {1}", typeof(T).ToString(), string.IsNullOrEmpty(desc) ? "" : desc);
			}
			if (breakInVerify)
			{
				UnityEngine.Debug.DebugBreak();
			}
		}
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyComponent<T>(T a, GameObject b) where T : Component
	{
		_ = b != null;
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyComponent<T, X>(T a, X b) where T : Component where X : Component
	{
		_ = b != null;
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyObject(GameObject a, GameObject b)
	{
		if (a != b)
		{
			if (logVerify)
			{
				UnityEngine.Debug.LogErrorFormat("Objects are not the same!  {0} != {1}", a.ToString(), b.ToString());
			}
			if (breakInVerify)
			{
				UnityEngine.Debug.DebugBreak();
			}
		}
	}

	[Conditional("VERIFY_COMPONENTS")]
	public static void VerifyTeam(TeamIndex a, Component b)
	{
		if (a != TeamComponent.GetObjectTeam(b.gameObject))
		{
			if (logVerify)
			{
				UnityEngine.Debug.LogErrorFormat("Objects are not the same!  {0} != {1}", a.ToString(), b.ToString());
			}
			if (breakInVerify)
			{
				UnityEngine.Debug.DebugBreak();
			}
		}
	}

	public static string BuildPrefabTransformPath(Transform root, Transform transform, bool appendCloneSuffix = false, bool includeRoot = false)
	{
		if (!root)
		{
			throw new ArgumentException("Value provided as 'root' is an invalid object.", "root");
		}
		if (!transform)
		{
			throw new ArgumentException("Value provided as 'transform' is an invalid object.", "transform");
		}
		try
		{
			Transform transform2 = transform;
			while ((object)transform2 != root)
			{
				if (!transform2)
				{
					string arg = TakeResult();
					throw new ArgumentException($"\"{arg}\" is not a child of \"{root}\".");
				}
				if (sharedStringStack.Count > 0)
				{
					sharedStringStack.Push("/");
				}
				sharedStringStack.Push(transform2.gameObject.name);
				transform2 = transform2.parent;
			}
			if (includeRoot)
			{
				if (sharedStringStack.Count > 0)
				{
					sharedStringStack.Push("/");
				}
				sharedStringStack.Push(root.gameObject.name);
			}
			return TakeResult();
		}
		finally
		{
			sharedStringBuilder.Clear();
			sharedStringStack.Clear();
		}
		string TakeResult()
		{
			while (sharedStringStack.Count > 0)
			{
				sharedStringBuilder.Append(sharedStringStack.Pop());
			}
			if (appendCloneSuffix)
			{
				sharedStringBuilder.Append(cloneString);
			}
			return sharedStringBuilder.Take();
		}
	}

	public static int GetItemCountForTeam(TeamIndex teamIndex, ItemIndex itemIndex, bool requiresAlive, bool requiresConnected = true)
	{
		int num = 0;
		ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
		int i = 0;
		for (int count = readOnlyInstancesList.Count; i < count; i++)
		{
			CharacterMaster characterMaster = readOnlyInstancesList[i];
			if (characterMaster.teamIndex == teamIndex && (!requiresAlive || characterMaster.hasBody) && (!requiresConnected || !characterMaster.playerCharacterMasterController || characterMaster.playerCharacterMasterController.isConnected))
			{
				num += characterMaster.inventory.GetItemCount(itemIndex);
			}
		}
		return num;
	}

	public static int GetItemCountGlobal(ItemIndex itemIndex, bool requiresAlive, bool requiresConnected = true)
	{
		int num = 0;
		ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
		int i = 0;
		for (int count = readOnlyInstancesList.Count; i < count; i++)
		{
			CharacterMaster characterMaster = readOnlyInstancesList[i];
			if ((!requiresAlive || characterMaster.hasBody) && (!requiresConnected || !characterMaster.playerCharacterMasterController || characterMaster.playerCharacterMasterController.isConnected))
			{
				num += characterMaster.inventory.GetItemCount(itemIndex);
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void NullifyIfInvalid<T>(ref T objRef) where T : UnityEngine.Object
	{
		if ((object)objRef != null && !objRef)
		{
			objRef = null;
		}
	}

	public static bool IsPrefab(GameObject gameObject)
	{
		if ((bool)gameObject)
		{
			return !gameObject.scene.IsValid();
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint IntToUintPlusOne(int value)
	{
		return (uint)(value + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int UintToIntMinusOne(uint value)
	{
		return (int)(value - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong LongToUlongPlusOne(long value)
	{
		return (ulong)(value + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long UlongToLongMinusOne(ulong value)
	{
		return (long)(value - 1);
	}

	public static float GetExpAdjustedDropChancePercent(float baseChancePercent, GameObject characterBodyObject)
	{
		int num = 0;
		DeathRewards component = characterBodyObject.GetComponent<DeathRewards>();
		if ((bool)component)
		{
			num = component.spawnValue;
		}
		return baseChancePercent * Mathf.Log((float)num + 1f, 2f);
	}

	public static bool IsDontDestroyOnLoad(GameObject go)
	{
		return go.scene.name == "DontDestroyOnLoad";
	}
}
