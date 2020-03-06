using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/// <summary>
	/// Non-squad related hotkey presses are detected and processed here.
	/// </summary>
	public class MiscHotkeyHandler {
		public void OnGUI() {
			if (Current.ProgramState != ProgramState.Playing || Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None) return;
			if (Resources.Hotkeys.DPSelectAllColonists.JustPressed) {
				SelectAllColonists();
				Event.current.Use();
			}
			if (Resources.Hotkeys.DPSendAllColonists.JustPressed) {
				SendAllColonistsToDefensivePosition();
				Event.current.Use();
			}
			if (Resources.Hotkeys.DPUndraftAll.JustPressed) {
				UndraftAllColonists();
				Event.current.Use();
			}
		}

		private void SelectAllColonists() {
			Find.Selector.ClearSelection();
			foreach (var pawn in GetColonistsOnVisibleMap()) {
				// bypass the selection limit
				Find.Selector.SelectedObjects.Add(pawn);
				SelectionDrawer.Notify_Selected(pawn);
			}
			Messages.Message("DefPos_msg_selectedAll".Translate(), MessageTypeDefOf.SilentInput);
		}

		private void SendAllColonistsToDefensivePosition() {
			var hits = 0;
			var activatedSlot = 0;
			foreach (var pawn in GetColonistsOnVisibleMap()) {
				if (!pawn.IsColonistPlayerControlled || pawn.Downed) continue;
				var handler = DefensivePositionsManager.Instance.GetHandlerForPawn(pawn);
				var result = handler.TrySendPawnToPositionByHotkey();
				if (result.success) {
					hits++;
				}
				activatedSlot = result.activatedSlot + 1;
			}
			if (hits > 0) {
				Messages.Message("DefPos_msg_sentAllSlot".Translate(hits, activatedSlot), MessageTypeDefOf.SilentInput);
			} else {
				Messages.Message("DefPos_msg_nopositionAllSlot".Translate(activatedSlot), MessageTypeDefOf.RejectInput);
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
				Messages.Message("DefPos_msg_undraftedAll".Translate(hits), MessageTypeDefOf.SilentInput);
				SoundDefOf.DraftOff.PlayOneShotOnCamera();
			} else {
				Messages.Message("DefPos_msg_nooneDrafted".Translate(), MessageTypeDefOf.RejectInput);
			}
		}

		private IEnumerable<Pawn> GetColonistsOnVisibleMap() {
			return GetConlonistsOnMaps(Find.CurrentMap);
		}

		
		private IEnumerable<Pawn> GetColonistsOnAllMaps() {
			return GetConlonistsOnMaps(Current.Game.Maps.ToArray());
		}

		// make sure to crate a new list, because the map pawn list can change during the operation (pawns carried by other pawns)
		private IEnumerable<Pawn> GetConlonistsOnMaps(params Map[] maps) {
			var result = new List<Pawn>();
			var playerFaction = Faction.OfPlayer;
			foreach (var map in maps) {
				if (map == null) continue;
				foreach (var pawn in map.mapPawns.AllPawnsSpawned) {
					if (pawn.Faction != playerFaction || !pawn.IsColonist) continue;
					result.Add(pawn);
				}
			}
			return result;
		}
	}
}