using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/**
	 * Non-squad related hotkey presses are detected and processed here.
	 */
	public class MiscHotkeyHandler {
		public void OnGUI() {
			if (Current.ProgramState != ProgramState.Playing || Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None) return;
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
			foreach (var pawn in GetColonistsOnVisibleMap()) {
				Find.Selector.Select(pawn);
			}
			Messages.Message("DefPos_msg_selectedAll".Translate(), MessageSound.Silent);
		}

		private void SendAllColonistsToDefensivePosition() {
			var hits = 0;
			foreach (var pawn in GetColonistsOnAllMaps()) {
				if (!pawn.IsColonistPlayerControlled || pawn.Downed) continue;
				var handler = DefensivePositionsManager.Instance.GetHandlerForPawn(pawn);
				if (handler.TrySendPawnToPositionByHotkey()) {
					hits++;
				}
			}
			if (hits > 0) {
				Messages.Message("DefPos_msg_sentAll".Translate(hits), MessageSound.Silent);
			} else {
				Messages.Message("DefPos_msg_nopositionAll".Translate(), MessageSound.RejectInput);
			}
		}

		private void UndraftAllColonists() {
			var hits = 0;
			foreach (var pawn in GetColonistsOnAllMaps()) {
				if (pawn.drafter != null && pawn.drafter.Drafted) {
					pawn.drafter.Drafted = false;
					hits++;
				}
			}
			if (hits > 0) {
				Messages.Message("DefPos_msg_undraftedAll".Translate(hits), MessageSound.Silent);
				SoundDefOf.DraftOff.PlayOneShotOnCamera();
			} else {
				Messages.Message("DefPos_msg_nooneDrafted".Translate(), MessageSound.RejectInput);
			}
		}

		private IEnumerable<Pawn> GetColonistsOnVisibleMap() {
			var map = Find.VisibleMap;
			var result = new List<Pawn>();
			if (map == null) return result;
			var playerFaction = Faction.OfPlayer;
			foreach (var pawn in map.mapPawns.AllPawnsSpawned) {
				if (pawn.Faction != playerFaction || !pawn.IsColonist) continue;
				result.Add(pawn);
			}
			return result;
		}

		// make sure to crate a new list, because the map pawn list can change during the operation (pawns carried by other pawns)
		private IEnumerable<Pawn> GetColonistsOnAllMaps() {
			var result = new List<Pawn>();
			var playerFaction = Faction.OfPlayer;
			foreach (var map in Current.Game.Maps) {
				foreach (var pawn in map.mapPawns.AllPawnsSpawned) {
					if (pawn.Faction != playerFaction || !pawn.IsColonist) continue;
					result.Add(pawn);
				}
			}
			return result;
		}
	}
}