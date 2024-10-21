using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using HG;
using JetBrains.Annotations;

namespace RoR2;

public static class NetworkModCompatibilityHelper
{
	[NotNull]
	public delegate byte[] ModListHasherDelegate([NotNull] IEnumerable<string> modList);

	public static readonly string steamworksGameserverGameDataValuePrefix;

	public static readonly string steamworksGameserverRulesBaseName;

	private static string[] _networkModList;

	private static ModListHasherDelegate _modListHasher;

	public static string networkModHash { get; private set; }

	public static string steamworksGameserverGameDataValue { get; private set; }

	public static string steamworksGameserverGameRulesValue { get; private set; }

	public static IEnumerable<string> networkModList
	{
		get
		{
			return _networkModList;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			List<string> list = CollectionPool<string, List<string>>.RentCollection();
			foreach (string item in value)
			{
				if (item == null)
				{
					throw new ArgumentException("Argument cannot contain null entries.", "value");
				}
				list.Add(item);
			}
			_networkModList = list.ToArray();
			CollectionPool<string, List<string>>.ReturnCollection(list);
			Rebuild();
		}
	}

	public static ModListHasherDelegate modListHasher
	{
		get
		{
			return _modListHasher;
		}
		set
		{
			_modListHasher = value ?? throw new ArgumentNullException("value");
			Rebuild();
		}
	}

	public static event Action onUpdated;

	public static byte[] DefaultModListHasher([NotNull] IEnumerable<string> networkModList)
	{
		HashAlgorithm hashAlgorithm = MD5.Create();
		byte[] result = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(string.Join(",", networkModList)));
		hashAlgorithm.Dispose();
		return result;
	}

	private static void Rebuild()
	{
		byte[] array = modListHasher(networkModList);
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.AppendByteHexValue(array[i]);
		}
		networkModHash = stringBuilder.ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		steamworksGameserverGameDataValue = steamworksGameserverGameDataValuePrefix + networkModHash;
		steamworksGameserverGameRulesValue = string.Join(",", _networkModList);
		NetworkModCompatibilityHelper.onUpdated?.Invoke();
	}

	static NetworkModCompatibilityHelper()
	{
		networkModHash = string.Empty;
		steamworksGameserverGameDataValuePrefix = "modHash=";
		steamworksGameserverGameDataValue = string.Empty;
		steamworksGameserverRulesBaseName = "mods";
		steamworksGameserverGameRulesValue = string.Empty;
		_networkModList = Array.Empty<string>();
		_modListHasher = DefaultModListHasher;
		Rebuild();
	}
}
