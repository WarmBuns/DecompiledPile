using System;
using System.Collections;
using System.Collections.Generic;
using HG;
using JetBrains.Annotations;

namespace RoR2.ContentManagement;

public abstract class NamedAssetCollection
{
	[CanBeNull]
	public abstract bool Find([NotNull] string assetName, out object result);
}
public class NamedAssetCollection<TAsset> : NamedAssetCollection, IEquatable<NamedAssetCollection<TAsset>>, IEnumerable<TAsset>, IEnumerable
{
	private struct AssetInfo : IComparable<AssetInfo>, IEquatable<AssetInfo>
	{
		public TAsset asset;

		public string assetName;

		public int CompareTo(AssetInfo other)
		{
			return string.Compare(assetName, other.assetName, StringComparison.OrdinalIgnoreCase);
		}

		public bool Equals(AssetInfo other)
		{
			if (object.Equals(asset, other.asset))
			{
				return string.Equals(assetName, other.assetName, StringComparison.Ordinal);
			}
			return false;
		}
	}

	public struct AssetEnumerator : IEnumerator<TAsset>, IEnumerator, IDisposable
	{
		private int i;

		private NamedAssetCollection<TAsset> src;

		public TAsset Current => src.assetInfos[i].asset;

		object IEnumerator.Current => Current;

		public AssetEnumerator(NamedAssetCollection<TAsset> src)
		{
			this.src = src;
			i = -1;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			i++;
			return i < src.Length;
		}

		public void Reset()
		{
			i = -1;
		}
	}

	private Dictionary<TAsset, string> assetToName = new Dictionary<TAsset, string>();

	private Dictionary<string, TAsset> nameToAsset = new Dictionary<string, TAsset>();

	private AssetInfo[] assetInfos = Array.Empty<AssetInfo>();

	private Func<TAsset, string> nameProvider;

	public int Length => assetInfos.Length;

	public int Count => Length;

	public bool IsReadOnly => false;

	public TAsset this[int i] => assetInfos[i].asset;

	public NamedAssetCollection([NotNull] Func<TAsset, string> nameProvider)
	{
		this.nameProvider = nameProvider;
	}

	public void Add([NotNull] TAsset[] newAssets)
	{
		string[] array = new string[newAssets.Length];
		for (int i = 0; i < newAssets.Length; i++)
		{
			array[i] = nameProvider(newAssets[i]);
		}
		for (int j = 0; j < newAssets.Length; j++)
		{
			TAsset val = newAssets[j];
			string text = array[j];
			if (assetToName.ContainsKey(val))
			{
				throw new ArgumentException($"Asset {val} is already registered!");
			}
			if (nameToAsset.ContainsKey(text))
			{
				throw new ArgumentException("Asset name " + text + " is already registered!");
			}
		}
		int num = assetInfos.Length;
		int newSize = num + newAssets.Length;
		Array.Resize(ref assetInfos, newSize);
		for (int k = 0; k < newAssets.Length; k++)
		{
			TAsset val2 = newAssets[k];
			string text2 = array[k];
			assetInfos[num + k] = new AssetInfo
			{
				asset = val2,
				assetName = text2
			};
			nameToAsset[text2] = val2;
			assetToName[val2] = text2;
		}
		Array.Sort(assetInfos);
	}

	[CanBeNull]
	public TAsset Find([NotNull] string assetName)
	{
		if (!nameToAsset.TryGetValue(assetName, out var value))
		{
			return default(TAsset);
		}
		return value;
	}

	[CanBeNull]
	public override bool Find([NotNull] string assetName, out object result)
	{
		TAsset value;
		bool result2 = nameToAsset.TryGetValue(assetName, out value);
		result = value;
		return result2;
	}

	[CanBeNull]
	public string GetAssetName([NotNull] TAsset asset)
	{
		if (!assetToName.TryGetValue(asset, out var value))
		{
			return null;
		}
		return value;
	}

	public bool Contains([NotNull] TAsset asset)
	{
		return assetToName.ContainsKey(asset);
	}

	public void Clear()
	{
		assetToName.Clear();
		nameToAsset.Clear();
		assetInfos = Array.Empty<AssetInfo>();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is NamedAssetCollection<TAsset> other))
		{
			return false;
		}
		return Equals(other);
	}

	public bool Equals(NamedAssetCollection<TAsset> other)
	{
		if (this == other)
		{
			return true;
		}
		if (assetInfos.Length != other.assetInfos.Length)
		{
			return false;
		}
		int i = 0;
		for (int num = assetInfos.Length; i < num; i++)
		{
			if (!assetInfos[i].Equals(other.assetInfos[i]))
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static void Copy(NamedAssetCollection<TAsset> src, NamedAssetCollection<TAsset> dest)
	{
		dest.assetInfos = ArrayUtils.Clone(src.assetInfos);
		dest.assetToName = new Dictionary<TAsset, string>(src.assetToName);
		dest.nameToAsset = new Dictionary<string, TAsset>(src.nameToAsset);
		dest.nameProvider = src.nameProvider;
	}

	public void CopyTo(NamedAssetCollection<TAsset> dest)
	{
		Copy(this, dest);
	}

	public AssetEnumerator GetEnumerator()
	{
		return new AssetEnumerator(this);
	}

	IEnumerator<TAsset> IEnumerable<TAsset>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
