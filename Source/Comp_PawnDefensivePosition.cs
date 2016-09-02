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
	public class Comp_PawnDefensivePosition : ThingComp {
		private const int NumAdvancedPositionButtons = 4;

		private static readonly Texture2D UITex_Basic = ContentFinder<Texture2D>.Get("UIPositionLarge");
		private static readonly Texture2D[] UITex_AdvancedIcons;
		static Comp_PawnDefensivePosition() {
			UITex_AdvancedIcons = new Texture2D[NumAdvancedPositionButtons];
			for (int i = 0; i < UITex_AdvancedIcons.Length; i++) {
				UITex_AdvancedIcons[i] = ContentFinder<Texture2D>.Get("UIPositionSmall_"+(i+1));
			}
		}

		private static int lastAdvancedControlUsed;

		private List<IntVec3> savedPositions;
		
		public override void Initialize(CompProperties compProps) {
			base.Initialize(compProps);
			InitalizePositionList();
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Collections.LookList(ref savedPositions, "savedPositions", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.LoadingVars && savedPositions == null) {
				InitalizePositionList();
			}
		}

		public override IEnumerable<Command> CompGetGizmosExtra() {
			if (DefensivePositionManager.Instance.AdvancedModeEnabled) {
				yield return new Gizmo_QuadButtonPanel {
					iconTextures = UITex_AdvancedIcons,
					iconClickAction = OnAdvancedGizmoClick,
					hotkeyAction = OnAdvancedHotkeyDown,
					hotKey = KeyBindingDef.Named("DefensivePositionGizmo"),
					defaultLabel = "DefPos_advanced_label".Translate(),
					defaultDesc = "DefPos_advanced_desc".Translate(),
					activateSound = SoundDefOf.TickTiny
				};
			} else {
				yield return new Command_Action {
					defaultLabel = "DefPos_basic_label".Translate(),
					defaultDesc = "DefPos_basic_desc".Translate(),
					hotKey = KeyBindingDef.Named("DefensivePositionGizmo"),
					action = OnBasicGizmoAction,
					icon = UITex_Basic,
					activateSound = SoundDefOf.TickTiny
				};
			}
		}

		private void OnAdvancedHotkeyDown() {
			var controlToActivate = DefensivePositionManager.Instance.SettingsDef.hotkeyActivatesLastUsedPosition ? lastAdvancedControlUsed : 0;
			HandleControlInteraction(controlToActivate);
		}

		private void OnAdvancedGizmoClick(int controlIndex) {
			lastAdvancedControlUsed = controlIndex;
			HandleControlInteraction(controlIndex);
		}

		private void OnBasicGizmoAction() {
			HandleControlInteraction(0);
		}

		private void HandleControlInteraction(int controlIndex) {
			var pawn = (Pawn) parent;
			var manager = DefensivePositionManager.Instance;
			if (ShiftIsHeld()) {
				// save new spot
				SetDefensivePosition(pawn, controlIndex);
				manager.ReportPawnInteraction(DefensivePositionManager.PawnInteractionType.SavedPosition, pawn, true, controlIndex);
			} else if (AltIsHeld()) {
				// switch mode
				manager.ScheduleAdvancedModeToggle();
			} else {
				// draft and send to saved spot
				var spot = savedPositions[controlIndex];
				if (spot.IsValid) {
					if (!pawn.Drafted) {
						pawn.drafter.Drafted = true;
						SoundDef.Named("DraftOn").PlayOneShotOnCamera();
					}
					SendDraftedPawnToPosition(pawn, spot);
					manager.ReportPawnInteraction(DefensivePositionManager.PawnInteractionType.SentToSavedPosition, pawn, true, controlIndex);
				} else {
					manager.ReportPawnInteraction(DefensivePositionManager.PawnInteractionType.SentToSavedPosition, pawn, false, controlIndex);
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

		private void SendDraftedPawnToPosition(Pawn pawn, IntVec3 position) {
			var intVec = Pawn_DraftController.BestGotoDestNear(position, pawn);
			var job = new Job(JobDefOf.Goto, intVec);
			job.playerForced = true;
			pawn.drafter.TakeOrderedJob(job);
			MoteMaker.MakeStaticMote(intVec, ThingDefOf.Mote_FeedbackGoto);
		}

		private void InitalizePositionList() {
			savedPositions = new List<IntVec3>(NumAdvancedPositionButtons);
			for (int i = 0; i < NumAdvancedPositionButtons; i++) {
				savedPositions.Add(IntVec3.Invalid);
			}
		}

		private bool ShiftIsHeld() {
			return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		}

		private bool AltIsHeld() {
			return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
		}
	}
}