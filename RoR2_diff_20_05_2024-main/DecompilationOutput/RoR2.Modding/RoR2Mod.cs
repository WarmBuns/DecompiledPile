using JetBrains.Annotations;
using RoR2.ContentManagement;

namespace RoR2.Modding;

public class RoR2Mod
{
	[NotNull]
	public readonly Mod mod;

	public IContentPackProvider contentPackProvider;

	public RoR2Mod([NotNull] Mod mod)
	{
		this.mod = mod;
	}
}
