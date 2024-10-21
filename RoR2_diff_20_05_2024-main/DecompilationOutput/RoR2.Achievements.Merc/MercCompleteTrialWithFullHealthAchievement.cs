namespace RoR2.Achievements.Merc;

[RegisterAchievement("MercCompleteTrialWithFullHealth", "Skills.Merc.EvisProjectile", "CompleteUnknownEnding", 3u, typeof(MercCompleteTrialWithFullHealthServerAchievement))]
public class MercCompleteTrialWithFullHealthAchievement : BaseAchievement
{
	private class MercCompleteTrialWithFullHealthServerAchievement : BaseServerAchievement
	{
		private ToggleAction listenForDamage;

		private ToggleAction listenForGameOver;

		private bool failed;

		private bool runOk;

		public override void OnInstall()
		{
			base.OnInstall();
			listenForDamage = new ToggleAction(delegate
			{
				RoR2Application.onFixedUpdate += OnFixedUpdate;
			}, delegate
			{
				RoR2Application.onFixedUpdate -= OnFixedUpdate;
			});
			listenForGameOver = new ToggleAction(delegate
			{
				Run.onServerGameOver += OnServerGameOver;
			}, delegate
			{
				Run.onServerGameOver -= OnServerGameOver;
			});
			Run.onRunStartGlobal += OnRunStart;
			Run.onRunDestroyGlobal += OnRunDestroy;
			if ((bool)Run.instance)
			{
				OnRunDiscovered(Run.instance);
			}
		}

		public override void OnUninstall()
		{
			Run.onRunDestroyGlobal -= OnRunDestroy;
			Run.onRunStartGlobal -= OnRunStart;
			listenForGameOver.SetActive(newActive: false);
			listenForDamage.SetActive(newActive: false);
			base.OnUninstall();
		}

		private bool CharacterIsAtFullHealthOrNull()
		{
			CharacterBody currentBody = GetCurrentBody();
			if ((bool)currentBody)
			{
				return currentBody.healthComponent.fullCombinedHealth <= currentBody.healthComponent.combinedHealth;
			}
			return true;
		}

		private void OnFixedUpdate()
		{
			if (!CharacterIsAtFullHealthOrNull())
			{
				Fail();
			}
		}

		private void Fail()
		{
			failed = true;
			listenForDamage.SetActive(newActive: false);
			listenForGameOver.SetActive(newActive: false);
		}

		private void OnServerGameOver(Run run, GameEndingDef gameEndingDef)
		{
			if (gameEndingDef.isWin)
			{
				if (runOk && !failed)
				{
					Grant();
				}
				runOk = false;
			}
		}

		private void OnRunStart(Run run)
		{
			OnRunDiscovered(run);
		}

		private void OnRunDiscovered(Run run)
		{
			runOk = run is WeeklyRun;
			if (runOk)
			{
				listenForGameOver.SetActive(newActive: true);
				listenForDamage.SetActive(newActive: true);
				failed = false;
			}
		}

		private void OnRunDestroy(Run run)
		{
			OnRunLost(run);
		}

		private void OnRunLost(Run run)
		{
			listenForGameOver.SetActive(newActive: false);
			listenForDamage.SetActive(newActive: false);
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("MercBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
	}
}
