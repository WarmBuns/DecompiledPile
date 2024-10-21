using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace RoR2.ContentManagement;

public readonly struct ReadOnlyNamedAssetCollection<TAsset> : IEnumerable<TAsset>, IEnumerable, IEquatable<ReadOnlyNamedAssetCollection<TAsset>>
{
	private readonly NamedAssetCollection<TAsset> src;

	public int Length => src.Length;

	public ReadOnlyNamedAssetCollection(NamedAssetCollection<TAsset> src)
	{
		this.src = src;
	}

	public TAsset Find(string assetName)
	{
		return src.Find(assetName);
	}

	public bool Find(string assetName, out object result)
	{
		return src.Find(assetName, out result);
	}

	public string GetAssetName(TAsset asset)
	{
		return src.GetAssetName(asset);
	}

	public bool Contains([NotNull] TAsset asset)
	{
		return src.Contains(asset);
	}

	public static void Copy(ReadOnlyNamedAssetCollection<TAsset> src, NamedAssetCollection<TAsset> dest)
	{
		NamedAssetCollection<TAsset>.Copy(src.src, dest);
	}

	public void CopyTo(NamedAssetCollection<TAsset> dest)
	{
		src.CopyTo(dest);
	}

	public override bool Equals(object obj)
	{
		if (obj is NamedAssetCollection<TAsset> namedAssetCollection)
		{
			return Equals(namedAssetCollection);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return src.GetHashCode();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public NamedAssetCollection<TAsset>.AssetEnumerator GetEnumerator()
	{
		return src.GetEnumerator();
	}

	IEnumerator<TAsset> IEnumerable<TAsset>.GetEnumerator()
	{
		return GetEnumerator();
	}

	public bool Equals(ReadOnlyNamedAssetCollection<TAsset> other)
	{
		return src.Equals(other.src);
	}

	public static implicit operator ReadOnlyNamedAssetCollection<TAsset>(NamedAssetCollection<TAsset> src)
	{
		return new ReadOnlyNamedAssetCollection<TAsset>(src);
	}
}
