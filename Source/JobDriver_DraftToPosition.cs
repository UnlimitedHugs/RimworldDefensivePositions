using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DefensivePositions {
	/// <summary>
	/// Allows drafting and moving to the pawn's defensive position to be added to the job queue
	/// </summary>
	public class JobDriver_DraftToPosition : JobDriver {
		public override bool TryMakePreToilReservations(bool errorOnFailed) {
			return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(() => !pawn.IsColonistPlayerControlled || pawn.Downed || pawn.drafter == null);
			yield return new Toil {
				initAction = () => {
					pawn.drafter.Drafted = true;
					var turret = TryFindMannableGunAtPosition(pawn, TargetLocA);
					if (turret != null) {
						var newJob = new Job(JobDefOf.ManTurret, turret.parent);
						pawn.jobs.TryTakeOrderedJob(newJob);
					} else {
						var amendedPosition = RCellFinder.BestOrderedGotoDestNear(TargetLocA, pawn);
						var gotoJob = new Job(JobDefOf.Goto, amendedPosition) {playerForced = true};
						pawn.jobs.TryTakeOrderedJob(gotoJob);
						MoteMaker.MakeStaticMote(amendedPosition, pawn.Map, ThingDefOf.Mote_FeedbackGoto);
					}
				}
			};
		}

		// check cardinal adjacent cells for mannable things
		private static CompMannable TryFindMannableGunAtPosition(Pawn forPawn, IntVec3 position) {
			if (!forPawn.RaceProps.ToolUser) return null;
			var cardinals = GenAdj.CardinalDirections;
			for (int i = 0; i < cardinals.Length; i++) {
				var things = forPawn.Map.thingGrid.ThingsListAt(cardinals[i] + position);
				for (int j = 0; j < things.Count; j++) {
					var thing = things[j] as ThingWithComps;
					var comp = thing?.GetComp<CompMannable>();
					if (comp == null || thing.InteractionCell != position) continue;
					var props = comp.Props;
					if (props == null || props.manWorkType == WorkTags.None || forPawn.story == null || 
						forPawn.story.WorkTagIsDisabled(props.manWorkType)) continue;
					return comp;
				}
			}
			return null;
		}
	}
}