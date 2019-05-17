using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Injects the "Defensive position" control right after the draft pawn toggle.
	/// </summary>
	[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
	internal static class DraftController_GetGizmos_Patch {
		//Pawn_DraftController: internal IEnumerable<Gizmo> GetGizmos()
		[HarmonyPostfix]
		public static void InsertDefensivePositionGizmo(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result) {
			// try insert our gizmo right after the draft toggle
			var pawn = __instance.pawn;
			var gizmos = __result.ToList();
			var draftToggleIndex = TryFindDraftToggleIndex(gizmos);
			var insertAtIndex = gizmos.Count > 0 ? 1 : 0;
			var draftAllowed = true;
			if (draftToggleIndex >= 0) {
				draftAllowed = !gizmos[draftToggleIndex].disabled;
				insertAtIndex = draftToggleIndex + 1;
			}
			// not drawn if pawn is downed
			if (draftAllowed) {
				gizmos.Insert(insertAtIndex, DefensivePositionsManager.Instance.GetHandlerForPawn(pawn).GetGizmo(pawn));
			}
			__result = gizmos;
		}

		private static int TryFindDraftToggleIndex(List<Gizmo> gizmos) {
			// identify draft toggle by its icon
			var index = -1;
			for (int i = 0; i < gizmos.Count; i++) {
				var toggle = gizmos[i] as Command_Toggle;
				if (toggle != null && toggle.icon == TexCommand.Draft) {
					index = i;
					break;
				}
			}
			return index;
		}
	}
}