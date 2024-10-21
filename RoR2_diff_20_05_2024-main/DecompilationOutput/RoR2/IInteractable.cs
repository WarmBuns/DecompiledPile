using JetBrains.Annotations;

namespace RoR2;

public interface IInteractable
{
	[CanBeNull]
	string GetContextString([NotNull] Interactor activator);

	Interactability GetInteractability([NotNull] Interactor activator);

	void OnInteractionBegin([NotNull] Interactor activator);

	bool ShouldIgnoreSpherecastForInteractibility([NotNull] Interactor activator);

	bool ShouldShowOnScanner();

	bool ShouldProximityHighlight();
}
