using System;
using System.Collections.Generic;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/**
	 * The hub of the mod. Stores the handlers for the individual colonists and other support info.
	 */
	public class DefensivePositionsManager : ModBase {
		public static DefensivePositionsManager Instance { get; private set; }

		public enum HotkeyMode {
			FirstSlotOnly,
			LastUsedSlot,
			MultiPress
		}

		public override string ModIdentifier {
			get { return "DefensivePositions"; }
		}

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		public bool AdvancedModeEnabled {
			get { return saveData.advancedModeEnabled; }
		}

		public int LastAdvancedControlUsed {
			get { return saveData.lastAdvancedControlUsed; }
			set { saveData.lastAdvancedControlUsed = value; }
		}

		public List<PawnSquad> SquadData {
			get { return saveData.pawnSquads; }
		} 

		public ScheduledReportManager Reporter { get; private set; }

		public SettingHandle<HotkeyMode> SlotHotkeySetting { get; private set; }

		private readonly PawnSquadHandler squadHandler;
		private readonly MiscHotkeyHandler miscHotkeys;
		private bool modeSwitchScheduled;
		private SoundDef scheduledSound;
		private DefensivePositionsData saveData;

		private DefensivePositionsManager() {
			Instance = this;
			squadHandler = new PawnSquadHandler();
			miscHotkeys = new MiscHotkeyHandler();
			Reporter = new ScheduledReportManager();
		}

		public override void DefsLoaded() {
			SlotHotkeySetting = Settings.GetHandle("slotHotkeyMode", "setting_slotHotkeyMode_label".Translate(), "setting_slotHotkeyMode_desc".Translate(), HotkeyMode.MultiPress, null, "setting_slotHotkeyMode_");
			Settings.TryRemoveUnclaimedValue("firstSlotHotkey");
		}

		public override void WorldLoaded() {
			saveData = UtilityWorldObjectManager.GetUtilityWorldObject<DefensivePositionsData>();
		}

		public override void FixedUpdate() {
			if (Current.ProgramState != ProgramState.Playing) return;
			if (modeSwitchScheduled) {
				saveData.advancedModeEnabled = !saveData.advancedModeEnabled;
				modeSwitchScheduled = false;
			}
			Reporter.Update();
			if (scheduledSound != null) {
				scheduledSound.PlayOneShotOnCamera();
				scheduledSound = null;
			}
		}

		public override void OnGUI() {
			if (Current.ProgramState != ProgramState.Playing || saveData == null) return;
			squadHandler.OnGUI();
			miscHotkeys.OnGUI();
		}

		public PawnSavedPositionHandler GetHandlerForPawn(Pawn pawn) {
			if(saveData == null) throw new Exception("Cannot get handler- saveData not loaded");
			var pawnId = pawn.thingIDNumber;
			PawnSavedPositionHandler handler;
			if (!saveData.handlers.TryGetValue(pawnId, out handler)) {
				handler = new PawnSavedPositionHandler();
				saveData.handlers.Add(pawnId, handler);
			}
			handler.Owner = pawn;
			return handler;
		}

		// actual switching will occur on next frame- due to possible multiple calls
		public void ScheduleAdvancedModeToggle() {
			modeSwitchScheduled = true;
		}

		public void ScheduleSoundOnCamera(SoundDef sound) {
			scheduledSound = sound;
		}
	}
}