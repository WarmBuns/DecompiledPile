using JetBrains.Annotations;
using RoR2.UI;

namespace RoR2;

public interface IInspectInfoProvider
{
	bool CanBeInspected();

	[NotNull]
	InspectInfo GetInfo();
}
