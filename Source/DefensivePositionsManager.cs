using System;
using System.Collections.Generic;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Multiplayer.API;
using Verse;
using Verse.Sound;

namespace DefensivePositions
{

    /// <summary>
    /// The hub of the mod. Stores the handlers for the individual colonists and other support info.
    /// </summary>
    public class DefensivePositionsManager : ModBase
    {
        public static DefensivePositionsManager Instance { get; private set; }
        private static bool FirstLaunch = true;

        public enum HotkeyMode
        {
            FirstSlotOnly,
            LastUsedSlot,
            MultiPress
        }

        public enum ShiftKeyMode
        {
            AssignSlot,
            QueueOrder
        }

        public override string ModIdentifier
        {
            get { return "DefensivePositions"; }
        }

        internal new ModLogger Logger
        {
            get { return base.Logger; }
        }

        public bool AdvancedModeEnabled
        {
            get { return saveData.advancedModeEnabled; }
        }

        public int LastAdvancedControlUsed
        {
            get { return saveData.lastAdvancedControlUsed; }
            set { saveData.lastAdvancedControlUsed = value; }
        }

        public List<PawnSquad> SquadData
        {
            get { return saveData.pawnSquads; }
        }

        public ScheduledReportManager Reporter { get; }

        public SettingHandle<HotkeyMode> SlotHotkeySetting { get; private set; }
        public SettingHandle<ShiftKeyMode> ShiftKeyModeSetting { get; private set; }
        public SettingHandle<bool> VanillaKeyOverridenSetting { get; private set; }

        internal readonly PawnSquadHandler squadHandler;
        private readonly MiscHotkeyHandler miscHotkeys;
        private bool modeSwitchScheduled;
        private SoundDef scheduledSound;
        //[SyncField]
        private DefensivePositionsData saveData;


        private DefensivePositionsManager()
        {
            Instance = this;
            squadHandler = new PawnSquadHandler();
            miscHotkeys = new MiscHotkeyHandler();
            Reporter = new ScheduledReportManager();
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();

            SlotHotkeySetting = Settings.GetHandle("slotHotkeyMode", "setting_slotHotkeyMode_label".Translate(), "setting_slotHotkeyMode_desc".Translate(), HotkeyMode.MultiPress, null, "setting_slotHotkeyMode_");
            VanillaKeyOverridenSetting = Settings.GetHandle("vanillaKeyOverriden", null, null, false);
            VanillaKeyOverridenSetting.NeverVisible = true;
            ShiftKeyModeSetting = Settings.GetHandle("shiftKeyMode", "setting_shiftKeyMode_label".Translate(), "setting_shiftKeyMode_desc".Translate(), ShiftKeyMode.AssignSlot, null, "setting_shiftKeyMode_");
            OverrideVanillaKeyIfNeeded();

            if (MP.enabled && FirstLaunch)
            {
                if (!MP.API.Equals("0.1"))
                {
                    Log.Error("DefensivePosition: MP API version mismatch. This mod is designed to work with MPAPI version 0.1");
                }
                else
                {
                    MP.RegisterAll();
                    //Log.Message("DefensivePosition: MP init");
                }
                FirstLaunch = false;
            }
        }

        public override void WorldLoaded()
        {
            saveData = UtilityWorldObjectManager.GetUtilityWorldObject<DefensivePositionsData>();

            base.WorldLoaded();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (Current.ProgramState != ProgramState.Playing) return;
            if (modeSwitchScheduled)
            {
                ToggleAdvancedMode(!saveData.advancedModeEnabled);
                modeSwitchScheduled = false;
            }
            Reporter.Update();
            if (scheduledSound != null)
            {
                scheduledSound.PlayOneShotOnCamera();
                scheduledSound = null;
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (Current.ProgramState != ProgramState.Playing || saveData == null) return;
            squadHandler.OnGUI();
            miscHotkeys.OnGUI();
        }

        public PawnSavedPositionHandler GetHandlerForPawn(Pawn pawn)
        {
            if (saveData == null) throw new Exception("Cannot get handler- saveData not loaded");
            var pawnId = pawn.thingIDNumber;
            if (!saveData.handlers.TryGetValue(pawnId, out PawnSavedPositionHandler handler))
            {
                handler = new PawnSavedPositionHandler();
                saveData.handlers.Add(pawnId, handler);
            }
            handler.Owner = pawn;
            return handler;
        }

        // actual switching will occur on next frame- due to possible multiple calls
        public void ScheduleAdvancedModeToggle()
        {
            modeSwitchScheduled = true;
        }

        public void ScheduleSoundOnCamera(SoundDef sound)
        {
            scheduledSound = sound;
        }

        [SyncMethod]
        private void ToggleAdvancedMode(bool enable)
        {
            saveData.advancedModeEnabled = enable;
        }

        // free the "T" key, claimed by vanilla in 1.0. This is only done once and in the interest of not breaking player habits
        private void OverrideVanillaKeyIfNeeded()
        {
            if (!VanillaKeyOverridenSetting)
            {
                var keyDef = KeyBindingDefOf.ToggleBeautyDisplay;
                if (keyDef.MainKey == KeyCode.T)
                {
                    KeyBindingData kbd;
                    if (KeyPrefs.KeyPrefsData.keyPrefs.TryGetValue(keyDef, out kbd))
                    {
                        kbd.keyBindingA = KeyCode.None;
                        KeyPrefs.Save();
                        VanillaKeyOverridenSetting.Value = true;
                        HugsLibController.SettingsManager.SaveChanges();
                        //Logger.Message("Cleared 'toggle beauty display' key binding");
                    }
                }
            }
        }

        #region MP_Compatibility

        [SyncWorker(shouldConstruct = false)]
        public static void SyncDefensivePositionsManager(SyncWorker sync, ref DefensivePositionsManager inst)
        {
            if (!sync.isWriting) inst = DefensivePositionsManager.Instance;
        }

        [SyncWorker(shouldConstruct = false)]
        public static void SyncPawnSavedPositionHandler(SyncWorker sync, ref PawnSavedPositionHandler inst)
        {
            var ownerPawn = inst?.Owner;
            sync.Bind(ref ownerPawn);
            if (!sync.isWriting && ownerPawn != null) inst = DefensivePositionsManager.Instance.GetHandlerForPawn(ownerPawn);
        }

        [SyncWorker(shouldConstruct = false)]
        public static void SyncPawnSquadHandler(SyncWorker sync, ref PawnSquadHandler inst)
        {
            if (!sync.isWriting) inst = DefensivePositionsManager.Instance.squadHandler;
        }

        #endregion
    }
}