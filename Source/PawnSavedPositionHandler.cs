using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace DefensivePositions {
	/**
	 * This is where the gizmos are displayed and the positions are stored for saving.
	 */
	[StaticConstructorOnStartup]
	public class PawnSavedPositionHandler : IExposable {
		public const int NumAdvancedPositionButtons = 4;

		private static readonly Texture2D UITex_Basic = ContentFinder<Texture2D>.Get("UIPositionLarge");
		private static readonly Texture2D[] UITex_AdvancedIcons;
		static PawnSavedPositionHandler() {
			UITex_AdvancedIcons = new Texture2D[NumAdvancedPositionButtons];
			for (int i = 0; i < UITex_AdvancedIcons.Length; i++) {
				UITex_AdvancedIcons[i] = ContentFinder<Texture2D>.Get("UIPositionSmall_"+(i+1));
			}
		}

		public Pawn Owner { get; set; }

		private List<IntVec3> savedPositions;

		public PawnSavedPositionHandler() {
			InitalizePositionList();
		}

		public void ExposeData() {
			Scribe_Collections.LookList(ref savedPositions, "savedPositions", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.LoadingVars && savedPositions == null) {
				InitalizePositionList();
			}
		}

		public bool TrySendPawnToPosition() {
			var index = GetHotkeyControlIndex();
			var position = savedPositions[index];
			if(!position.IsValid) return false;
			DraftPawnToPosition(Owner, position);
			return true;
		}

		public Command GetGizmo(Pawn forPawn) {
			Owner = forPawn;
			if (DefensivePositionsManager.Instance.AdvancedModeEnabled) {
				return new Gizmo_QuadButtonPanel {
					iconTextures = UITex_AdvancedIcons,
					iconClickAction = OnAdvancedGizmoClick,
					hotkeyAction = OnAdvancedHotkeyDown,
					hotKey = HotkeyDefOf.DefensivePositionGizmo,
					defaultLabel = "DefPos_advanced_label".Translate(),
					defaultDesc = "DefPos_advanced_desc".Translate(),
					activateSound = SoundDefOf.TickTiny
				};
			} else {
				return new Command_Action {
					defaultLabel = "DefPos_basic_label".Translate(),
					defaultDesc = "DefPos_basic_desc".Translate(),
					hotKey = HotkeyDefOf.DefensivePositionGizmo,
					action = OnBasicGizmoAction,
					icon = UITex_Basic,
					activateSound = SoundDefOf.TickTiny
				};
			}
		}

		private void OnAdvancedHotkeyDown() {
			var controlToActivate = GetHotkeyControlIndex();
			HandleControlInteraction(controlToActivate);
		}

		private void OnAdvancedGizmoClick(int controlIndex) {
			DefensivePositionsManager.Instance.LastAdvancedControlUsed = controlIndex;
			HandleControlInteraction(controlIndex);
		}

		private void OnBasicGizmoAction() {
			HandleControlInteraction(0);
		}

		private int GetHotkeyControlIndex() {
			return DefensivePositionsMod.Instance.FirstSlotHotkeySetting.Value ? 0 : DefensivePositionsManager.Instance.LastAdvancedControlUsed;
		}

		private void HandleControlInteraction(int controlIndex) {
			var manager = DefensivePositionsManager.Instance;
			if (DefensivePositionsUtility.ShiftIsHeld) {
				// save new spot
				SetDefensivePosition(Owner, controlIndex);
				manager.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SavedPosition, Owner, true, controlIndex);
			} else if (DefensivePositionsUtility.ControlIsHeld) {
				// unset saved spot
				var hadPosition = DiscardSavedPosition(controlIndex);
				manager.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.ClearedPosition, Owner, hadPosition, controlIndex);
			} else if (DefensivePositionsUtility.AltIsHeld) {
				// switch mode
				manager.ScheduleAdvancedModeToggle();
			} else {
				// draft and send to saved spot
				var spot = savedPositions[controlIndex];
				if (spot.IsValid) {
					DraftPawnToPosition(Owner, spot);
					manager.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SentToSavedPosition, Owner, true, controlIndex);
				} else {
					manager.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SentToSavedPosition, Owner, false, controlIndex);
				}
			}
		}

		private void SetDefensivePosition(Pawn pawn, int postionIndex) {
			var targetPos = pawn.Position;
			var curPawnJob = pawn.jobs.curJob;
			if (pawn.Drafted && curPawnJob != null && curPawnJob.def == JobDefOf.Goto) {
				targetPos = curPawnJob.targetA.Cell;
			}
			savedPositions[postionIndex] = targetPos;
		}

		private bool DiscardSavedPosition(int controlIndex) {
			var hadPosition = savedPositions[controlIndex].IsValid;
			savedPositions[controlIndex] = IntVec3.Invalid;
			return hadPosition;
		}

		private void DraftPawnToPosition(Pawn pawn, IntVec3 position) {
			if (!pawn.IsColonistPlayerControlled || pawn.Downed || pawn.drafter == null) return;
			if (!pawn.Drafted) {
				pawn.drafter.Drafted = true;
				SoundDef.Named("DraftOn").PlayOneShotOnCamera();
			}
			var turret = TryFindMannableGunAtPosition(pawn, position);
			if (turret != null) {
				var newJob = new Job(JobDefOf.ManTurret, turret.parent);
				pawn.drafter.TakeOrderedJob(newJob);
			} else {
				var intVec = Pawn_DraftController.BestGotoDestNear(position, pawn);
				var job = new Job(JobDefOf.Goto, intVec) {playerForced = true};
				pawn.drafter.TakeOrderedJob(job);
				MoteMaker.MakeStaticMote(intVec, ThingDefOf.Mote_FeedbackGoto);
			}
		}

		// check cardinal adjacent cells for mannable things
		private CompMannable TryFindMannableGunAtPosition(Pawn forPawn, IntVec3 position) {
			if (!forPawn.RaceProps.ToolUser) return null;
			var cardinals = GenAdj.CardinalDirections;
			for (int i = 0; i < cardinals.Length; i++) {
				var things = Find.ThingGrid.ThingsListAt(cardinals[i] + position);
				for (int j = 0; j < things.Count; j++) {
					var thing = things[j] as ThingWithComps;
					if (thing == null) continue;
					var comp = thing.GetComp<CompMannable>();
					if (comp == null || thing.InteractionCell != position) continue;
					var props = comp.Props;
					if (props == null || props.manWorkType == WorkTags.None || forPawn.story == null || forPawn.story.WorkTagIsDisabled(props.manWorkType)) continue;
					return comp;
				}
			}
			return null;
		}

		private void InitalizePositionList() {
			savedPositions = new List<IntVec3>(NumAdvancedPositionButtons);
			for (int i = 0; i < NumAdvancedPositionButtons; i++) {
				savedPositions.Add(IntVec3.Invalid);
			}
		}
	}
}