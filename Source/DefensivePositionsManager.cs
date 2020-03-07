using System;
using System.Collections.Generic;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/// <summary>
	/// The hub of the mod. Stores the handlers for the individual colonists and other support info.
	/// </summary>
	public class DefensivePositionsManager : ModBase {
		public static DefensivePositionsManager Instance { get; private set; }

		public enum HotkeyMode {
			FirstSlotOnly,
			LastUsedSlot,
			MultiPress
		}

		public enum ShiftKeyMode {
			AssignSlot,
			QueueOrder
		}

		public override string ModIdentifier {
			get { return "DefensivePositions"; }
		}

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		public bool AdvancedModeEnabled {
			get { return saveData.AdvancedModeEnabled; }
		}

		public int LastAdvancedControlUsed {
			get { return saveData.LastAdvancedControlUsed; }
			set { saveData.LastAdvancedControlUsed = value; }
		}

		public List<PawnSquad> SquadData {
			get { return saveData.PawnSquads; }
		} 

		public ScheduledReportManager Reporter { get; }

		public SettingHandle<HotkeyMode> SlotHotkeySetting { get; private set; }
		public SettingHandle<ShiftKeyMode> ShiftKeyModeSetting { get; private set; }
		public SettingHandle<int> SameGroupDistanceSetting { get; set; }
		private SettingHandle<bool> VanillaKeyOverridenSetting { get; set; }

		internal readonly PawnSquadHandler squadHandler;
		private readonly MiscHotkeyHandler miscHotkeys;
		private bool modeSwitchScheduled;
		private SoundDef scheduledSound;
		private WorldData saveData;


		private DefensivePositionsManager() {
			Instance = this;
			squadHandler = new PawnSquadHandler();
			miscHotkeys = new MiscHotkeyHandler();
			Reporter = new ScheduledReportManager();
		}

		public override void StaticInitialize() {
			Compat_MultiplayerAPI.Initialize();
		}

		public override void DefsLoaded() {
			SlotHotkeySetting = Settings.GetHandle("slotHotkeyMode", 
				"setting_slotHotkeyMode_label".Translate(), "setting_slotHotkeyMode_desc".Translate(), 
				HotkeyMode.MultiPress, null, "setting_slotHotkeyMode_");
			VanillaKeyOverridenSetting = Settings.GetHandle("vanillaKeyOverriden", null, null, false);
			VanillaKeyOverridenSetting.NeverVisible = true;
			VanillaKeyOverridenSetting.CanBeReset = false;
			ShiftKeyModeSetting = Settings.GetHandle("shiftKeyMode", 
				"setting_shiftKeyMode_label".Translate(), "setting_shiftKeyMode_desc".Translate(), 
				ShiftKeyMode.AssignSlot, null, "setting_shiftKeyMode_");
			const int defaultSameGroupDistance = 30;
			SameGroupDistanceSetting = Settings.GetHandle("sameGroupDistance", "settings_sameGroupDistance_label".Translate(), 
				"settings_sameGroupDistance_desc".Translate(), defaultSameGroupDistance);
			SameGroupDistanceSetting.Validator = Validators.IntRangeValidator(0, 1000);
			SameGroupDistanceSetting.SpinnerIncrement = 5;
			OverrideVanillaKeyIfNeeded();
		}

		public override void WorldLoaded() {
			saveData = Find.World.GetComponent<WorldData>();
		}

		public override void FixedUpdate() {
			if (Current.ProgramState != ProgramState.Playing) return;
			if (modeSwitchScheduled) {
				ToggleAdvancedMode(!saveData.AdvancedModeEnabled);
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

		public override void MapDiscarded(Map map) {
			saveData?.OnMapDiscarded(map);
		}

		public PawnSavedPositionHandler GetHandlerForPawn(Pawn pawn) {
			if(saveData == null) throw new Exception("Cannot get handler- saveData not loaded");
			return saveData.GetOrAddPawnHandler(pawn);
		}

		// actual switching will occur on next frame- due to possible multiple calls
		public void ScheduleAdvancedModeToggle() {
			modeSwitchScheduled = true;
		}

		public void ScheduleSoundOnCamera(SoundDef sound) {
			scheduledSound = sound;
		}

		internal void ToggleAdvancedMode(bool enable) {
			saveData.AdvancedModeEnabled = enable;
		}

		// free the "T" key, claimed by vanilla in 1.0. This is only done once and in the interest of not breaking player habits
		private void OverrideVanillaKeyIfNeeded() {
			if (!VanillaKeyOverridenSetting) {
				var keyDef = KeyBindingDefOf.ToggleBeautyDisplay;
				if (keyDef.MainKey == KeyCode.T) {
					KeyBindingData kbd;
					if (KeyPrefs.KeyPrefsData.keyPrefs.TryGetValue(keyDef, out kbd)) {
						kbd.keyBindingA = KeyCode.None;
						KeyPrefs.Save();
						VanillaKeyOverridenSetting.Value = true;
						HugsLibController.SettingsManager.SaveChanges();
						Logger.Message("Cleared 'toggle beauty display' key binding");
					}
				}
			}
		}
	}
}