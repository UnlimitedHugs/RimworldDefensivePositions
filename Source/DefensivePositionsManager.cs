using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib;
using RimWorld;
using UnityEngine;
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

		public int LastAdvancedControlUsed {
			get { return lastAdvancedControlUsed; }
			set { lastAdvancedControlUsed = value; }
		}

		public ScheduledReportManager Reporter { get; private set; }

		private bool modeSwitchScheduled;
		private MiscHotkeyHandler miscHotkeys;

		// saved fields
		private bool advancedModeEnabled;
		private int lastAdvancedControlUsed;
		private Dictionary<int, PawnSavedPositionHandler> handlers = new Dictionary<int, PawnSavedPositionHandler>();
		private PawnSquadHandler squads;

		public DefensivePositionsManager() {
			Instance = this;
			Reporter = new ScheduledReportManager();
			squads = new PawnSquadHandler();
			miscHotkeys = new MiscHotkeyHandler();
			TryDraftControllerDetour();
			EnsureComponentIsActive();
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref advancedModeEnabled, "advancedModeEnabled", false);
			Scribe_Values.LookValue(ref lastAdvancedControlUsed, "lastAdvancedControlUsed", 0);
			Scribe_Collections.LookDictionary(ref handlers, "handlers", LookMode.Value, LookMode.Deep);
			Scribe_Deep.LookDeep(ref squads, "squads");
			if (Scribe.mode == LoadSaveMode.LoadingVars) {
				if(handlers == null) handlers = new Dictionary<int, PawnSavedPositionHandler>();
				if(squads == null) squads = new PawnSquadHandler();
				lastAdvancedControlUsed = Mathf.Clamp(lastAdvancedControlUsed, 0, PawnSavedPositionHandler.NumAdvancedPositionButtons-1);
			}
		}

		public override void MapComponentUpdate() {
			if (modeSwitchScheduled) {
				advancedModeEnabled = !advancedModeEnabled;
				modeSwitchScheduled = false;
			}
			Reporter.Update();
		}

		public override void MapComponentOnGUI() {
			squads.OnGUI();
			miscHotkeys.OnGUI();
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
			if (controllerMethod == null || detouredMethod == null) DefensivePositionsMod.Instance.Logger.Error("Failed to reflect required methods.");
			DetourProvider.CompatibleDetour(controllerMethod, detouredMethod, DefensivePositionsMod.Instance.ModContentPack.Name);
		}

	}
}