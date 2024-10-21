using System;
using System.Collections.Generic;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public class Corpse : MonoBehaviour
{
	private class CorpsesMaxConVar : BaseConVar
	{
		private static CorpsesMaxConVar instance = new CorpsesMaxConVar("corpses_max", ConVarFlags.Archive | ConVarFlags.Engine, "25", "The maximum number of corpses allowed.");

		private CorpsesMaxConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (TextSerialization.TryParseInvariant(newValue, out int result))
			{
				maxCorpses = result;
			}
		}

		public override string GetString()
		{
			return TextSerialization.ToStringInvariant(maxCorpses);
		}
	}

	public enum DisposalMode
	{
		Hard,
		OutOfSight,
		Soft
	}

	private class CorpseDisposalConVar : BaseConVar
	{
		private static CorpseDisposalConVar instance = new CorpseDisposalConVar("corpses_disposal", ConVarFlags.Archive | ConVarFlags.Engine, null, "The corpse disposal mode. Choices are Hard and OutOfSight.");

		private CorpseDisposalConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			try
			{
				DisposalMode disposalMode = (DisposalMode)Enum.Parse(typeof(DisposalMode), newValue, ignoreCase: true);
				if (disposalMode == Corpse.disposalMode)
				{
					return;
				}
				Corpse.disposalMode = disposalMode;
				if (disposalMode == DisposalMode.Hard || disposalMode != DisposalMode.OutOfSight)
				{
					return;
				}
				foreach (Corpse instances in instancesList)
				{
					instances.CollectRenderers();
				}
			}
			catch (ArgumentException)
			{
				Console.ShowHelpText(name);
			}
		}

		public override string GetString()
		{
			return disposalMode.ToString();
		}
	}

	private static readonly List<Corpse> instancesList = new List<Corpse>();

	private Renderer[] renderers;

	public bool dissolve;

	public bool forceCulled;

	public float lifeTime;

	public static float corpseLifeTime = 6f;

	public static float dissolveTime = 1f;

	private CharacterModel dm;

	private static int maxCorpses = 25;

	private static DisposalMode disposalMode = DisposalMode.OutOfSight;

	private static int maxChecksPerUpdate = 3;

	private static int currentCheckIndex = 0;

	private void CollectRenderers()
	{
		if (renderers == null)
		{
			renderers = GetComponentsInChildren<Renderer>();
		}
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		if (disposalMode != 0)
		{
			CollectRenderers();
		}
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	[InitDuringStartup]
	private static void StaticInit()
	{
		RoR2Application.onUpdate += StaticUpdate;
	}

	private static void IncrementCurrentCheckIndex()
	{
		currentCheckIndex++;
		if (currentCheckIndex >= instancesList.Count)
		{
			currentCheckIndex = 0;
		}
	}

	private static bool CheckCorpseOutOfSight(Corpse corpse)
	{
		Renderer[] array = corpse.renderers;
		foreach (Renderer renderer in array)
		{
			if ((bool)renderer && renderer.isVisible)
			{
				return false;
			}
		}
		return true;
	}

	private void GrabDitherController()
	{
		dm = GetComponent<CharacterModel>();
	}

	public void UpdateDissolve()
	{
		lifeTime -= Time.deltaTime;
		float corpseFade = Mathf.Clamp01(lifeTime / dissolveTime);
		if ((bool)dm)
		{
			dm.corpseFade = corpseFade;
		}
		if (lifeTime <= 0f)
		{
			DestroyCorpse(this);
		}
	}

	private static void StaticUpdate()
	{
		if (maxCorpses < 0)
		{
			return;
		}
		int num = instancesList.Count - maxCorpses;
		int num2 = Math.Min(instancesList.Count, maxChecksPerUpdate);
		if (disposalMode == DisposalMode.OutOfSight)
		{
			num2 = Math.Min(num2, num);
		}
		switch (disposalMode)
		{
		case DisposalMode.Hard:
		{
			for (int num3 = num - 1; num3 >= 0; num3--)
			{
				DestroyCorpse(instancesList[num3]);
			}
			break;
		}
		case DisposalMode.OutOfSight:
		{
			for (int k = 0; k < num2; k++)
			{
				IncrementCurrentCheckIndex();
				if (CheckCorpseOutOfSight(instancesList[currentCheckIndex]))
				{
					DestroyCorpse(instancesList[currentCheckIndex]);
				}
			}
			break;
		}
		case DisposalMode.Soft:
		{
			for (int i = 0; i < num2; i++)
			{
				IncrementCurrentCheckIndex();
				Corpse corpse = instancesList[currentCheckIndex];
				if (corpse.forceCulled)
				{
					DestroyCorpse(corpse);
					if (num > 0)
					{
						num--;
					}
				}
				else if (!corpse.dissolve)
				{
					if (num > 0)
					{
						num--;
						corpse.lifeTime = dissolveTime;
					}
					else
					{
						corpse.lifeTime = corpseLifeTime;
					}
					corpse.dissolve = true;
				}
			}
			for (int j = 0; j < instancesList.Count; j++)
			{
				if (instancesList[j].dissolve)
				{
					instancesList[j].UpdateDissolve();
				}
			}
			break;
		}
		}
	}

	private static void DestroyCorpse(Corpse corpse)
	{
		if ((bool)corpse)
		{
			UnityEngine.Object.Destroy(corpse.gameObject);
		}
	}
}
