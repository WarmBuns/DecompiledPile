using System;
using System.Runtime.CompilerServices;
using System.Text;
using HG;
using JetBrains.Annotations;

[Obsolete("Use HG.StringBuilderPool instead.", false)]
public static class StringBuilderPool
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NotNull]
	[Obsolete("Use HG.StringBuilderPool instead.", false)]
	public static StringBuilder RentStringBuilder()
	{
		return HG.StringBuilderPool.RentStringBuilder();
	}

	[CanBeNull]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Obsolete("Use HG.StringBuilderPool instead.", false)]
	public static StringBuilder ReturnStringBuilder([NotNull] StringBuilder stringBuilder)
	{
		return HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}
}
