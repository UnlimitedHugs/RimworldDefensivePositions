using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/**
	 * Allows pawns to be assigned to persistent squads, the members of which can be selected via hotkey press.
	 */
	public class PawnSquadHandler : IExposable {
		private const string SquadHotkeyNameBase = "DPSquad";
		private const int NumSquadHotkeys = 9;

		private readonly List<KeyValuePair<KeyBindingDef, int>> squadKeys = new List<KeyValuePair<KeyBindingDef, int>>();
		private List<PawnSquad> pawnSquads = new List<PawnSquad>();

		public PawnSquadHandler() {
			PrepareSquadHotkeys();
		}

		public void ExposeData() {
			Scribe_Collections.LookList(ref pawnSquads, "pawnSquads", LookMode.Deep);
			if (pawnSquads == null) pawnSquads = new List<PawnSquad>();
		}

		public void OnGUI() {
			if(Event.current.type != EventType.KeyDown) return;
			PollSquadHotkeys(Event.current.keyCode);
		}

		private void PrepareSquadHotkeys() {
			for (int i = 1; i < NumSquadHotkeys+1; i++) {
				var hotkeyDef = DefDatabase<KeyBindingDef>.GetNamedSilentFail(SquadHotkeyNameBase + i);
				if(hotkeyDef == null) continue;
				squadKeys.Add(new KeyValuePair<KeyBindingDef, int>(hotkeyDef, i));
			}
		}

		private void PollSquadHotkeys(KeyCode pressedKey) {
			if(pressedKey == KeyCode.None) return;
			for (int i = 0; i < squadKeys.Count; i++) {
				var key = squadKeys[i].Key;
				KeyBindingData binding;
				if (KeyPrefs.KeyPrefsData.keyPrefs.TryGetValue(key, out binding) && (pressedKey == binding.keyBindingA || pressedKey == binding.keyBindingB)) {
					ProcessSquadCommand(squadKeys[i].Value);
					return;
				}
			}
		}

		private void ProcessSquadCommand(int squadNumber) {
			var assignMode = DefensivePositionsUtility.ControlIsHeld;
			var squad = pawnSquads.Find(s => s.squadId == squadNumber);
			if (assignMode) {
				var idList = new List<int>();
				foreach (var obj in Find.Selector.SelectedObjects) {
					var pawn = obj as Pawn;
					if(pawn == null || pawn.Faction == null || !pawn.Faction.IsPlayer) continue;
					idList.Add(pawn.thingIDNumber);
				}
				if (idList.Count > 0) {
					Messages.Message("DefPos_msg_squadAssigned".Translate(idList.Count, squadNumber), MessageSound.Benefit);
					if (squad == null) {
						squad = new PawnSquad {squadId = squadNumber};
						pawnSquads.Add(squad);
					}
					squad.pawnIds = idList;
				} else {
					Messages.Message("DefPos_msg_squadCleared".Translate(squadNumber), MessageSound.Benefit);
					if (squad != null) pawnSquads.Remove(squad);
				}
			} else {
				if(!DefensivePositionsUtility.ShiftIsHeld) Find.Selector.ClearSelection();
				if (squad != null && squad.pawnIds.Count > 0) {
					var mapPawns = Find.MapPawns.AllPawnsSpawned;
					for (int i = 0; i < mapPawns.Count; i++) {
						var pawn = mapPawns[i];
						if(pawn.Dead) continue;
						if (squad.pawnIds.Contains(pawn.thingIDNumber)) Find.Selector.Select(pawn);
					}
				} else {
					Messages.Message("DefPos_msg_squadEmpty".Translate(squadNumber), MessageSound.RejectInput);
				}
			}
		}

		private class PawnSquad : IExposable {
			public int squadId;
			public List<int> pawnIds;

			public PawnSquad() {
				pawnIds = new List<int>();
			}
			
			public void ExposeData() {
				Scribe_Values.LookValue(ref squadId, "squadId", 0);
				Scribe_Collections.LookList(ref pawnIds, "pawnIds", LookMode.Value);
				if(pawnIds == null) pawnIds = new List<int>();
			}
		}
	}
}