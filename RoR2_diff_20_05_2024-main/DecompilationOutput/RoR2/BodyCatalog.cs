using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Modding;
using RoR2.UI;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public static class BodyCatalog
{
	public static class SpecialCases
	{
		private static BodyIndex chief = BodyIndex.None;

		private static BodyIndex railgunner = BodyIndex.None;

		private static BodyIndex heretic = BodyIndex.None;

		public static BodyIndex Chief()
		{
			if (chief == BodyIndex.None)
			{
				chief = FindBodyIndex("ChefBody");
			}
			return chief;
		}

		public static BodyIndex RailGunner()
		{
			if (railgunner == BodyIndex.None)
			{
				railgunner = FindBodyIndex("RailgunnerBody");
			}
			return railgunner;
		}

		public static BodyIndex HereticBody()
		{
			if (heretic == BodyIndex.None)
			{
				heretic = FindBodyIndex("HereticBody");
			}
			return heretic;
		}
	}

	public static ResourceAvailability availability = default(ResourceAvailability);

	private static string[] bodyNames;

	private static GameObject[] bodyPrefabs;

	private static CharacterBody[] bodyPrefabBodyComponents;

	private static Component[][] bodyComponents;

	private static GenericSkill[][] skillSlots;

	private static SkinDef[][] skins;

	private static readonly Dictionary<string, BodyIndex> nameToIndexMap = new Dictionary<string, BodyIndex>();

	private static Texture2D defaultPortraitIcon;

	public static int bodyCount { get; private set; }

	public static IEnumerable<GameObject> allBodyPrefabs => bodyPrefabs;

	public static IEnumerable<CharacterBody> allBodyPrefabBodyBodyComponents => bodyPrefabBodyComponents;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<GameObject>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.BodyCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.bodyPrefabs);
		}
		remove
		{
		}
	}

	public static GameObject GetBodyPrefab(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(bodyPrefabs, (int)bodyIndex);
	}

	[CanBeNull]
	public static CharacterBody GetBodyPrefabBodyComponent(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(bodyPrefabBodyComponents, (int)bodyIndex);
	}

	public static string GetBodyName(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(bodyNames, (int)bodyIndex);
	}

	public static BodyIndex FindBodyIndex([NotNull] string bodyName)
	{
		if (nameToIndexMap.TryGetValue(bodyName, out var value))
		{
			return value;
		}
		return BodyIndex.None;
	}

	public static BodyIndex FindBodyIndexCaseInsensitive([NotNull] string bodyName)
	{
		for (BodyIndex bodyIndex = (BodyIndex)0; (int)bodyIndex < bodyPrefabs.Length; bodyIndex++)
		{
			if (string.Compare(bodyPrefabs[(int)bodyIndex].name, bodyName, StringComparison.OrdinalIgnoreCase) == 0)
			{
				return bodyIndex;
			}
		}
		return BodyIndex.None;
	}

	public static BodyIndex FindBodyIndex(GameObject bodyObject)
	{
		return FindBodyIndex(bodyObject ? bodyObject.GetComponent<CharacterBody>() : null);
	}

	public static BodyIndex FindBodyIndex(CharacterBody characterBody)
	{
		return characterBody?.bodyIndex ?? BodyIndex.None;
	}

	[CanBeNull]
	public static GameObject FindBodyPrefab([NotNull] string bodyName)
	{
		BodyIndex bodyIndex = FindBodyIndex(bodyName);
		if (bodyIndex != BodyIndex.None)
		{
			return GetBodyPrefab(bodyIndex);
		}
		return null;
	}

	[CanBeNull]
	public static GameObject FindBodyPrefab(CharacterBody characterBody)
	{
		return GetBodyPrefab(FindBodyIndex(characterBody));
	}

	[CanBeNull]
	public static GameObject FindBodyPrefab(GameObject characterBodyObject)
	{
		return GetBodyPrefab(FindBodyIndex(characterBodyObject));
	}

	[CanBeNull]
	public static GenericSkill[] GetBodyPrefabSkillSlots(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(skillSlots, (int)bodyIndex);
	}

	public static SkinDef[] GetBodySkins(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(skins, (int)bodyIndex, Array.Empty<SkinDef>());
	}

	[SystemInitializer(new Type[] { })]
	public static IEnumerator Init()
	{
		AsyncOperationHandle<Texture2D> mysteryIconHandler = LegacyResourcesAPI.LoadAsync<Texture2D>("Textures/MiscIcons/texMysteryIcon");
		while (!mysteryIconHandler.IsDone)
		{
			yield return null;
		}
		defaultPortraitIcon = mysteryIconHandler.Result;
		SetBodyPrefabs(ContentManager.bodyPrefabs);
		availability.MakeAvailable();
	}

	private static void SetBodyPrefabs([NotNull] GameObject[] newBodyPrefabs)
	{
		bodyPrefabs = ArrayUtils.Clone(newBodyPrefabs);
		Array.Sort(bodyPrefabs, (GameObject a, GameObject b) => string.CompareOrdinal(a.name, b.name));
		bodyPrefabBodyComponents = new CharacterBody[bodyPrefabs.Length];
		bodyNames = new string[bodyPrefabs.Length];
		bodyComponents = new Component[bodyPrefabs.Length][];
		skillSlots = new GenericSkill[bodyPrefabs.Length][];
		skins = new SkinDef[bodyPrefabs.Length][];
		nameToIndexMap.Clear();
		for (BodyIndex bodyIndex = (BodyIndex)0; (int)bodyIndex < bodyPrefabs.Length; bodyIndex++)
		{
			GameObject gameObject = bodyPrefabs[(int)bodyIndex];
			string name = gameObject.name;
			bodyNames[(int)bodyIndex] = name;
			bodyComponents[(int)bodyIndex] = gameObject.GetComponents<Component>();
			skillSlots[(int)bodyIndex] = gameObject.GetComponents<GenericSkill>();
			nameToIndexMap.Add(name, bodyIndex);
			nameToIndexMap.Add(name + "(Clone)", bodyIndex);
			CharacterBody characterBody = (bodyPrefabBodyComponents[(int)bodyIndex] = gameObject.GetComponent<CharacterBody>());
			characterBody.bodyIndex = bodyIndex;
			BodyIndex _temporaryBodyIndex = bodyIndex;
			AsyncOperationHandle<Texture2D> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Texture2D>("Textures/BodyIcons/" + name);
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Texture2D> x)
			{
				FinishAsyncLoads(_temporaryBodyIndex, x.Result);
			};
			skins[(int)bodyIndex] = gameObject.GetComponent<ModelLocator>()?.modelTransform?.GetComponent<ModelSkinController>()?.skins ?? Array.Empty<SkinDef>();
			if (string.IsNullOrEmpty(characterBody.baseNameToken))
			{
				characterBody.baseNameToken = "UNIDENTIFIED";
			}
		}
		bodyCount = bodyPrefabs.Length;
	}

	private static void FinishAsyncLoads(BodyIndex _bodyIndex, Texture2D tex)
	{
		CharacterBody characterBody = bodyPrefabBodyComponents[(int)_bodyIndex];
		if ((bool)tex)
		{
			characterBody.portraitIcon = tex;
		}
		else if (characterBody.portraitIcon == null)
		{
			characterBody.portraitIcon = defaultPortraitIcon;
		}
	}

	private static IEnumerator GeneratePortraits(bool forceRegeneration)
	{
		Debug.LogError("Starting portrait generation.");
		Debug.Break();
		ModelPanel modelPanel = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/IconGenerator")).GetComponentInChildren<ModelPanel>();
		yield return new WaitForEndOfFrame();
		int i = 0;
		while (i < bodyPrefabs.Length)
		{
			CharacterBody characterBody = bodyPrefabBodyComponents[i];
			if ((bool)characterBody && (forceRegeneration || IconIsNotSuitable(characterBody.portraitIcon)))
			{
				float num = 1f;
				try
				{
					Debug.LogFormat("Generating portrait for {0}", bodyPrefabs[i].name);
					modelPanel.modelPrefab = bodyPrefabs[i].GetComponent<ModelLocator>()?.modelTransform.gameObject;
					modelPanel.SetAnglesForCharacterThumbnail(setZoom: true);
					PrintController printController = modelPanel.modelPrefab?.GetComponentInChildren<PrintController>();
					if ((object)printController != null)
					{
						num = Mathf.Max(num, printController.printTime + 1f);
					}
					TemporaryOverlay temporaryOverlay = modelPanel.modelPrefab?.GetComponentInChildren<TemporaryOverlay>();
					if ((object)temporaryOverlay != null)
					{
						num = Mathf.Max(num, temporaryOverlay.durationInstance + 1f);
					}
				}
				catch (Exception)
				{
				}
				RoR2Application.onLateUpdate += UpdateCamera;
				yield return new WaitForSeconds(num);
				modelPanel.SetAnglesForCharacterThumbnail(setZoom: true);
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
				try
				{
					Texture2D texture2D = new Texture2D(modelPanel.renderTexture.width, modelPanel.renderTexture.height, TextureFormat.ARGB32, mipChain: false, linear: false);
					RenderTexture active = RenderTexture.active;
					RenderTexture.active = modelPanel.renderTexture;
					texture2D.ReadPixels(new Rect(0f, 0f, modelPanel.renderTexture.width, modelPanel.renderTexture.height), 0, 0, recalculateMipMaps: false);
					RenderTexture.active = active;
					byte[] array = texture2D.EncodeToPNG();
					using FileStream fileStream = new FileStream("Assets/RoR2/GeneratedPortraits/" + bodyPrefabs[i].name + ".png", FileMode.Create, FileAccess.Write);
					fileStream.Write(array, 0, array.Length);
				}
				catch (Exception)
				{
				}
				RoR2Application.onLateUpdate -= UpdateCamera;
				yield return new WaitForEndOfFrame();
			}
			int num2 = i + 1;
			i = num2;
		}
		UnityEngine.Object.Destroy(modelPanel.transform.root.gameObject);
		static bool IconIsNotSuitable(Texture texture)
		{
			if (!texture)
			{
				return true;
			}
			string name = texture.name;
			if (name == "texMysteryIcon" || name == "texNullIcon")
			{
				return true;
			}
			return false;
		}
		void UpdateCamera()
		{
			modelPanel.SetAnglesForCharacterThumbnail(setZoom: true);
		}
	}

	[ConCommand(commandName = "body_generate_portraits", flags = ConVarFlags.None, helpText = "Generates portraits for all bodies that are currently using the default.")]
	private static void CCBodyGeneratePortraits(ConCommandArgs args)
	{
		RoR2Application.instance.StartCoroutine(GeneratePortraits(args.TryGetArgBool(0) == true));
	}

	[ConCommand(commandName = "body_list", flags = ConVarFlags.None, helpText = "Prints a list of all character bodies in the game.")]
	private static void CCBodyList(ConCommandArgs args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < bodyComponents.Length; i++)
		{
			stringBuilder.Append("[").Append(i).Append("]=")
				.Append(bodyNames[i])
				.AppendLine();
		}
	}

	[ConCommand(commandName = "body_reload_all", flags = ConVarFlags.Cheat, helpText = "Reloads all bodies and repopulates the BodyCatalog.")]
	private static void CCBodyReloadAll(ConCommandArgs args)
	{
		SetBodyPrefabs(Resources.LoadAll<GameObject>("Prefabs/CharacterBodies/"));
	}
}
