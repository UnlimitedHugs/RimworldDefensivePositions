using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/**
	 * Non-squad related hotkey presses are detected and processed here.
	 */
	public class MiscHotkeyHandler {
		public void OnGUI() {
			if (Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None) return;
			if (HotkeyDefOf.DPSelectAllColonists.JustPressed) {
				SelectAllColonists();
			}
			if (HotkeyDefOf.DPSendAllColonists.JustPressed) {
				SendAllColonistsToDefensivePosition();
			}
			if (HotkeyDefOf.DPUndraftAll.JustPressed) {
				UndraftAllColonists();
			}
		}

		private void SelectAllColonists() {
			Find.Selector.ClearSelection();
			foreach (var pawn in GetAllColonists()) {
				Find.Selector.Select(pawn);
			}
			Messages.Message("DefPos_msg_selectedAll".Translate(), MessageSound.Silent);
		}

		private void SendAllColonistsToDefensivePosition() {
			var hits = 0;
			foreach (var pawn in GetAllColonists()) {
				if (!pawn.IsColonistPlayerControlled || pawn.Downed) continue;
				var handler = DefensivePositionsManager.Instance.GetHandlerForPawn(pawn);
				if (handler.TrySendPawnToPosition()) {
					hits++;
				}
			}
			if (hits > 0) {
				Messages.Message("DefPos_msg_sentAll".Translate(hits), MessageSound.Benefit);
			} else {
				Messages.Message("DefPos_msg_nopositionAll".Translate(), MessageSound.Benefit);
			}
		}

		private void UndraftAllColonists() {
			var hits = 0;
			foreach (var pawn in GetAllColonists()) {
				if (pawn.drafter.Drafted) {
					pawn.drafter.Drafted = false;
					hits++;
				}
			}
			if (hits > 0) {
				Messages.Message("DefPos_msg_undraftedAll".Translate(hits), MessageSound.Benefit);
			} else {
				Messages.Message("DefPos_msg_nooneDrafted".Translate(), MessageSound.RejectInput);
			}
		}

		private IEnumerable<Pawn> GetAllColonists() {
			// converting to array to prevent collection modified exception- pawn may be carrying another pawn
			return Find.MapPawns.AllPawnsSpawned.Where(p => !p.Dead && p.IsColonist && p.Faction != null && p.Faction.IsPlayer).ToArray();
		}
	}
}