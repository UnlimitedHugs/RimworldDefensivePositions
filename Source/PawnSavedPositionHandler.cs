using System;
using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Multiplayer.API;
using Verse;
using Verse.AI;

namespace DefensivePositions {
	public interface IDefensivePositionGizmoHandler {
		void OnBasicGizmoAction();
		void OnBasicGizmoHover();
		void OnAdvancedGizmoClick(int controlIndex);
		void OnAdvancedGizmoHover(int controlIndex);
		void OnAdvancedGizmoHotkeyDown();
		IEnumerable<FloatMenuOption> GetGizmoContextMenuOptions(int slotIndex, bool showSlotSuffix);
	}

	/// <summary>
	/// Stores position information for a single pawn and displays the gizmo that allows use and modify that information.
	/// </summary>
	public class PawnSavedPositionHandler : IExposable, IDefensivePositionGizmoHandler {
		public const int NumAdvancedPositionButtons = 4;
		public const float HotkeyMultiPressTimeout = .5f;
		public const float PositionMoteEmissionInterval = .5f;
		private const int InvalidMapValue = -1;

		private Pawn _owner;

		public Pawn Owner {
			get {
				if (_owner == null) DefensivePositionsManager.Instance.Logger.Error("Owner pawn has not been resolved yet\n" + Environment.StackTrace);
				return _owner;
			}
			set { _owner = value; }
		}

		private float lastMultiPressTime;
		private int lastMultiPressSlot;
		private float positionMoteExpireTime;

		// --- saved fields ---
		private List<IntVec3> savedPositions; // the positions saved in the 4 slots for this pawn
		private List<int> originalMaps; // the map ids these positions were saved on
		// ---

		public PawnSavedPositionHandler() {
			InitializePositionList();
		}

		public void ExposeData() {
			Scribe_Collections.Look(ref savedPositions, "savedPositions", LookMode.Value);
			Scribe_Collections.Look(ref originalMaps, "originalMaps", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.LoadingVars && savedPositions == null) {
				InitializePositionList();
			}
		}

		public HotkeyActivationResult TrySendPawnToPositionByHotkey() {
			var index = GetHotkeyControlIndex();
			var success = false;
			var position = savedPositions[index];
			if (OwnerHasValidSavedPositionInSlot(index)) {
				DraftPawnToPosition(Owner, position);
				success = true;
			}
			return new HotkeyActivationResult(success, index);
		}

		public Command GetGizmo(Pawn forPawn) {
			Owner = forPawn;
			if (DefensivePositionsManager.Instance.AdvancedModeEnabled) {
				return new Gizmo_QuadButtonPanel {
					atlasTexture = Resources.Textures.AdvancedButtonAtlas,
					iconUVsInactive = Resources.Textures.IconUVsInactive,
					iconUVsActive = Resources.Textures.IconUVsActive,
					activeIconMask = GetAdvancedIconActivationMask(),
					hotKey = Resources.Hotkeys.DefensivePositionGizmo,
					defaultLabel = "DefPos_advanced_label".Translate(),
					defaultDesc = "DefPos_advanced_desc".Translate(),
					activateSound = SoundDefOf.Tick_Tiny,
					interactionHandler = this
				};
			} else {
				var useActiveIcon = OwnerHasValidSavedPositionInSlot(0);
				return new Gizmo_DefensivePositionButton {
					defaultLabel = "DefPos_basic_label".Translate(),
					defaultDesc = "DefPos_basic_desc".Translate(),
					hotKey = Resources.Hotkeys.DefensivePositionGizmo,
					icon = useActiveIcon ? Resources.Textures.BasicButtonActive : Resources.Textures.BasicButton,
					hasHighPriorityIcon = useActiveIcon,
					activateSound = SoundDefOf.Tick_Tiny,
					interactionHandler = this
				};
			}
		}

		[SyncMethod]
		private void SetDefensivePosition(int positionIndex) {
            var targetPos = GetOwnerDestinationOrPosition();
            savedPositions[positionIndex] = targetPos;
            originalMaps[positionIndex] = Owner.Map.uniqueID;
            DefensivePositionsManager.Instance.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SavedPosition, Owner, true, positionIndex);
		}

		[SyncMethod]
		private void DiscardSavedPosition(int controlIndex) {
			var hadPosition = savedPositions[controlIndex].IsValid;
			savedPositions[controlIndex] = IntVec3.Invalid;
			originalMaps[controlIndex] = InvalidMapValue;
			DefensivePositionsManager.Instance.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.ClearedPosition, Owner, hadPosition, controlIndex);
		}

		private byte GetAdvancedIconActivationMask() {
			int mask = 0;
			for (int i = 0; i < NumAdvancedPositionButtons; i++) {
				mask |= (OwnerHasValidSavedPositionInSlot(i) ? 1 : 0) << i;
			}
			return (byte)mask;
		}

		private IntVec3 GetOwnerDestinationOrPosition() {
			var curJob = Owner.jobs.curJob;
			return Owner.Drafted && curJob != null && curJob.def == JobDefOf.Goto ? curJob.targetA.Cell : Owner.Position;
		}
		
		public void OnAdvancedGizmoHotkeyDown() {
			var controlToActivate = GetHotkeyControlIndex();
			HandleControlInteraction(controlToActivate);
		}

		public void OnAdvancedGizmoClick(int controlIndex) {
			DefensivePositionsManager.Instance.LastAdvancedControlUsed = controlIndex;
			HandleControlInteraction(controlIndex);
		}

		private void OnBasicGizmoAction() {
			HandleControlInteraction(0);
		}

		public void OnBasicGizmoHover() {
			HighlightDefensivePositionLocation(0);
		}

		public void OnAdvancedGizmoHover(int controlIndex) {
			HighlightDefensivePositionLocation(controlIndex);
		}

		public IEnumerable<FloatMenuOption> GetGizmoContextMenuOptions(int slotIndex, bool showSlotSuffix) {
			string TranslateWithSuffix(string key) => key.Translate(showSlotSuffix ? "DefPos_context_slotSuffix".Translate(slotIndex + 1) : string.Empty);
			yield return new FloatMenuOption(TranslateWithSuffix("DefPos_context_assignPosition"), () => SetDefensivePosition(slotIndex));
			if (OwnerHasValidSavedPositionInSlot(slotIndex)) {
				yield return new FloatMenuOption(TranslateWithSuffix("DefPos_context_clearPosition"), () => DiscardSavedPosition(slotIndex));
			}
			yield return new FloatMenuOption(TranslateWithSuffix("DefPos_context_toggleAdvanced"), () => DefensivePositionsManager.Instance.ScheduleAdvancedModeToggle());
		}

		private int GetHotkeyControlIndex() {
			switch (DefensivePositionsManager.Instance.SlotHotkeySetting.Value) {
				case DefensivePositionsManager.HotkeyMode.FirstSlotOnly:
					return 0;
				case DefensivePositionsManager.HotkeyMode.LastUsedSlot:
					return DefensivePositionsManager.Instance.LastAdvancedControlUsed;
				case DefensivePositionsManager.HotkeyMode.MultiPress:
					if (DefensivePositionsManager.Instance.AdvancedModeEnabled && Time.unscaledTime - lastMultiPressTime < HotkeyMultiPressTimeout) {
						lastMultiPressSlot = (lastMultiPressSlot + 1) % NumAdvancedPositionButtons;
					} else {
						lastMultiPressSlot = 0;
					}
					lastMultiPressTime = Time.unscaledTime;
					return lastMultiPressSlot;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

        //[SyncMethod]
		private void HandleControlInteraction(int controlIndex) {
			var manager = DefensivePositionsManager.Instance;
			if (HugsLibUtility.ShiftIsHeld && DefensivePositionsManager.Instance.ShiftKeyModeSetting.Value == DefensivePositionsManager.ShiftKeyMode.AssignSlot) {
				// save new spot
				SetDefensivePosition(controlIndex);
			} else if (HugsLibUtility.ControlIsHeld) {
				// unset saved spot
				DiscardSavedPosition(controlIndex);
			} else if (HugsLibUtility.AltIsHeld) {
				// switch mode
				manager.ScheduleAdvancedModeToggle();
			} else {
				// draft and send to saved spot
				var spot = savedPositions[controlIndex];
				if (OwnerHasValidSavedPositionInSlot(controlIndex)) {
					DraftPawnToPosition(Owner, spot);
					manager.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SentToSavedPosition, Owner, true, controlIndex);
				} else {
					manager.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SentToSavedPosition, Owner, false, controlIndex);
				}
			}
		}


		internal bool OwnerHasValidSavedPositionInSlot(int controlIndex) {
			// ensures that control index has a saved position and that position was saved on the map the pawn is on
			return savedPositions[controlIndex].IsValid && originalMaps[controlIndex] == Owner.Map.uniqueID;
		}

        [SyncMethod]
        private void DraftPawnToPosition(Pawn pawn, IntVec3 position) {
			var job = new Job(Resources.Jobs.DPDraftToPosition, position) {playerForced = true};
			pawn.jobs.TryTakeOrderedJob(job);
			DefensivePositionsManager.Instance.ScheduleSoundOnCamera(SoundDefOf.DraftOn);
		}

		private void HighlightDefensivePositionLocation(int controlIndex) {
			if (positionMoteExpireTime > Time.unscaledTime) return;
			if (OwnerHasValidSavedPositionInSlot(controlIndex)) {
				positionMoteExpireTime = Time.unscaledTime + PositionMoteEmissionInterval;
				var savedPosition = savedPositions[controlIndex];
				MoteMaker.MakeStaticMote(savedPosition, Owner.Map, Resources.Things.DPPositionMote);
			}
		}

		private void InitializePositionList() {
			savedPositions = new List<IntVec3>(NumAdvancedPositionButtons);
			originalMaps = new List<int>(NumAdvancedPositionButtons);
			for (int i = 0; i < NumAdvancedPositionButtons; i++) {
				savedPositions.Add(IntVec3.Invalid);
				originalMaps.Add(InvalidMapValue);
			}
		}

		public struct HotkeyActivationResult {
			public readonly bool success;
			public readonly int activatedSlot;

			public HotkeyActivationResult(bool success, int activatedSlot) {
				this.success = success;
				this.activatedSlot = activatedSlot;
			}
		}

		void IDefensivePositionGizmoHandler.OnBasicGizmoAction() {
			OnBasicGizmoAction();
		}

		public override string ToString() {
			return $"[{nameof(PawnSavedPositionHandler)} {Owner.ToStringSafe()}]";
		}
	}
}