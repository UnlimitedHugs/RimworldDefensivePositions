using System.Collections.Generic;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;

namespace DefensivePositions {
	/**
	 * Provides the detour target method for Pawn_DraftController.GetGizmos.
	 * The new method reporoduces the vanilla draft toggle and displays our custom position button.
	 * TODO: when updating, check that the vanilla draft toggle still provides the same behaviour 
	 */
	internal static class DraftControllerDetour {
		//Pawn_DraftController: internal IEnumerable<Gizmo> GetGizmos()
		[DetourMethod(typeof(Pawn_DraftController), "GetGizmos")]
		public static IEnumerable<Gizmo> _GetGizmos(this Pawn_DraftController controller) {
			var pawn = controller.pawn;
			if(!pawn.IsColonistPlayerControlled) yield break;

			var draftToggle = new Command_Toggle {
				hotKey = KeyBindingDefOf.CommandColonistDraft,
				isActive = (() => controller.Drafted),
				toggleAction = delegate {
					controller.Drafted = !controller.Drafted;
					PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Drafting, KnowledgeAmount.SpecificInteraction);
				},
				defaultDesc = "CommandToggleDraftDesc".Translate(),
				icon = TexCommand.Draft,
				turnOnSound = SoundDef.Named("DraftOn"),
				turnOffSound = SoundDef.Named("DraftOff")
			};
			if (controller.Drafted) {
				draftToggle.defaultLabel = "CommandUndraftLabel".Translate();
				draftToggle.tutorTag = "Undraft";
			} else {
				draftToggle.defaultLabel = "CommandDraftLabel".Translate();
				draftToggle.tutorTag = "Draft";
			}
			if (pawn.Downed) {
				draftToggle.Disable("IsIncapped".Translate(pawn.NameStringShort));
			}
			yield return draftToggle;

			if (!draftToggle.disabled) {
				yield return DefensivePositionsManager.Instance.GetHandlerForPawn(pawn).GetGizmo(pawn);
			}
		}
		 
	}
}