using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace DefensivePositions {
	/**
	 * The hub of the mod. Stores the handlers for the individual colonists and other support info.
	 */
	public class DefensivePositionsManager : MapComponent {
		public static DefensivePositionsManager Instance { get; private set; }

		public bool AdvancedModeEnabled {
			get { return advancedModeEnabled; }
		}

		public ModSettingsDef SettingsDef { get; private set; }

		public ScheduledReportManager Reporter { get; private set; }

		private bool modeSwitchScheduled;

		// saved fields
		private bool advancedModeEnabled;
		private Dictionary<int, PawnSavedPositionHandler> handlers = new Dictionary<int, PawnSavedPositionHandler>();

		public DefensivePositionsManager() {
			Instance = this;
			Reporter = new ScheduledReportManager();
			LoadSettingsDef();
			TryDraftControllerDetour();
			EnsureComponentIsActive();
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref advancedModeEnabled, "advancedModeEnabled", false);
			Scribe_Collections.LookDictionary(ref handlers, "handlers", LookMode.Value, LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.LoadingVars && handlers == null) {
				handlers = new Dictionary<int, PawnSavedPositionHandler>();
			}
		}

		public override void MapComponentUpdate() {
			base.MapComponentUpdate();
			if (modeSwitchScheduled) {
				advancedModeEnabled = !advancedModeEnabled;
				modeSwitchScheduled = false;
			}
			Reporter.Update();
		}

		public PawnSavedPositionHandler GetHandlerForPawn(Pawn pawn) {
			var pawnId = pawn.thingIDNumber;
			PawnSavedPositionHandler handler;
			if (!handlers.TryGetValue(pawnId, out handler)) {
				handler = new PawnSavedPositionHandler();
				handlers.Add(pawnId, handler);
			}
			return handler;
		}

		// actual switching will occur on next frame- due to possible multiple calls
		public void ScheduleAdvancedModeToggle() {
			modeSwitchScheduled = true;
		}

		private void LoadSettingsDef() {
			SettingsDef = DefDatabase<ModSettingsDef>.GetNamed("DefensivePositionsSettings", false);
			if (SettingsDef == null) {
				DefensivePositionsUtility.Error("Missing setting def named DefensivePositionsSettings");
				SettingsDef = new ModSettingsDef();
			}
		}

		// ensure the component is active even on maps where the mod was not active at map creation
		private void EnsureComponentIsActive() {
			LongEventHandler.ExecuteWhenFinished(() => {
				var components = Find.Map.components;
				if (components.Any(c => c is DefensivePositionsManager)) return;
				Find.Map.components.Add(this);
			});
		}

		private void TryDraftControllerDetour() {
			var controllerMethod = typeof (Pawn_DraftController).GetMethod("GetGizmos", BindingFlags.Instance | BindingFlags.NonPublic);
			var detouredMethod = typeof (DraftControllerDetour).GetMethod("_GetGizmos", BindingFlags.Static | BindingFlags.Public);
			var success = false;
			if (controllerMethod != null && detouredMethod != null) {
				success = DefensivePositionsUtility.TryDetourFromTo(controllerMethod, detouredMethod);
			}
			if (!success) DefensivePositionsUtility.Error("Failed to detour method Pawn_DraftController.GetGizmos");
		}

	}
}