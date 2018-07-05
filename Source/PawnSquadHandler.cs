using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/**
	 * Allows pawns to be assigned to persistent squads, the members of which can be selected via hotkey press.
	 */
	public class PawnSquadHandler {
		private const string SquadHotkeyNameBase = "DPSquad";
		private const int NumSquadHotkeys = 9;

		private readonly List<KeyValuePair<KeyBindingDef, int>> squadKeys = new List<KeyValuePair<KeyBindingDef, int>>();
		
		public PawnSquadHandler() {
			PrepareSquadHotkeys();
		}

		public void OnGUI() {
			if(Current.ProgramState != ProgramState.Playing || Event.current.type != EventType.KeyDown) return;
			PollSquadHotkeys(Event.current);
		}

		private void PrepareSquadHotkeys() {
			for (int i = 1; i < NumSquadHotkeys+1; i++) {
				var hotkeyDef = DefDatabase<KeyBindingDef>.GetNamedSilentFail(SquadHotkeyNameBase + i);
				if(hotkeyDef == null) continue;
				squadKeys.Add(new KeyValuePair<KeyBindingDef, int>(hotkeyDef, i));
			}
		}

		private void PollSquadHotkeys(Event evt) {
			var pressedKey = evt.keyCode;
			if(pressedKey == KeyCode.None) return;
			for (int i = 0; i < squadKeys.Count; i++) {
				var key = squadKeys[i].Key;
				KeyBindingData binding;
				if (KeyPrefs.KeyPrefsData.keyPrefs.TryGetValue(key, out binding) && (pressedKey == binding.keyBindingA || pressedKey == binding.keyBindingB)) {
					ProcessSquadCommand(squadKeys[i].Value);
					evt.Use();
					return;
				}
			}
		}

		private void ProcessSquadCommand(int squadNumber) {
			var assignMode = HugsLibUtility.ControlIsHeld;
			var pawnSquads = DefensivePositionsManager.Instance.SquadData;
			var squad = pawnSquads.Find(s => s.squadId == squadNumber);
			if (assignMode) {
				// Control is held, assign pawns to squad
				var idList = new List<int>();
				foreach (var obj in Find.Selector.SelectedObjects) {
					var thing = obj as Thing;
					if(thing == null || thing.Faction == null || !thing.Faction.IsPlayer || !(thing is Pawn || thing is Building)) continue;
					idList.Add(thing.thingIDNumber);
				}
				if (idList.Count > 0) {
					// reassign squad with selected pawns
					Messages.Message("DefPos_msg_squadAssigned".Translate(idList.Count, squadNumber), MessageTypeDefOf.TaskCompletion);
					if (squad == null) {
						squad = new PawnSquad {squadId = squadNumber};
						pawnSquads.Add(squad);
					}
					squad.pawnIds = idList;
				} else {
					// no pawns selected, clear squad
					Messages.Message("DefPos_msg_squadCleared".Translate(squadNumber), MessageTypeDefOf.TaskCompletion);
					if (squad != null) pawnSquads.Remove(squad);
				}
			} else {
				// Select pawns that belong to squad
				var selectionBeforeClear = Find.Selector.SelectedObjects.ToList();
				if(!HugsLibUtility.ShiftIsHeld) Find.Selector.ClearSelection();
				List<Thing> matchingThingsOnMaps = null;
				Caravan matchingCaravan = null;
				if (squad != null && squad.pawnIds.Count > 0) {
					matchingThingsOnMaps = GetLivePawnsAndBuildingsOnAllMapsById(squad.pawnIds);
					if (matchingThingsOnMaps.Count == 0) {
						matchingCaravan = TryGetFirstCaravanWithPawnsById(squad.pawnIds);
					}
				}
				if (matchingThingsOnMaps!=null && matchingThingsOnMaps.Count>0) {
					var things = SelectOnlyThingsOnSameMap(matchingThingsOnMaps);
					// focus view on squad if repeat squad key press OR if not currently viewing the map
					if (Find.CurrentMap != things[0].Map || InWorldView() || ThingsAlreadyMatchSelection(things, selectionBeforeClear)) {	
						TryEscapeWorldView();
						TryFocusThingGroupCenter(things);
					}
					// select pawns on map, switch map if necessary
					foreach (var thing in things) {
						Find.Selector.Select(thing);
					}
				} else if (matchingCaravan != null) {
					// select caravan with pawns
					CameraJumper.TryJumpAndSelect(matchingCaravan);
				} else {
					Messages.Message("DefPos_msg_squadEmpty".Translate(squadNumber), MessageTypeDefOf.RejectInput);
				}
			}
		}

		private List<Thing> GetLivePawnsAndBuildingsOnAllMapsById(List<int> thingIds) {
			var idSet = new HashSet<int>(thingIds);
			var results = new List<Thing>();
			for (int i = 0; i < Current.Game.Maps.Count; i++) {
				var map = Current.Game.Maps[i];
				var candidates = map.mapPawns.AllPawnsSpawned.Cast<Thing>().Concat(map.listerBuildings.allBuildingsColonist.Cast<Thing>());
				foreach (var thing in candidates) {
					var pawn = thing as Pawn;
					if (pawn != null && pawn.Dead) continue;
					if (idSet.Contains(thing.thingIDNumber)) {
						results.Add(thing);
					} 
				}
			}
			return results;
		}

		// filter the list by dropping all pawns that are on a different map than the first one
		private List<Thing> SelectOnlyThingsOnSameMap(List<Thing> things) {
			Map firstMap = null;
			var results = new List<Thing>();
			foreach (var thing in things) {
				if (firstMap == null) firstMap = thing.Map;
				if(thing.Map != firstMap) continue;
				results.Add(thing);
			}
			return results;
		}

		private Caravan TryGetFirstCaravanWithPawnsById(List<int> pawnIds) {
			var caravans = Find.WorldObjects.Caravans;
			foreach (var caravan in caravans) {
				if(!caravan.IsPlayerControlled) continue;
				foreach (var pawn in caravan.PawnsListForReading) {
					if (pawnIds.Contains(pawn.thingIDNumber)) return caravan;
				}
			}
			return null;
		}

		// switches to the map the pawns are on and moves the camera to the center point of the group
		private void TryFocusThingGroupCenter(List<Thing> things) {
			if (things.Count == 0) return;
			var sum = IntVec3.Zero;
			foreach (var thing in things) {
				sum += thing.Position;
			}
			var average = new IntVec3(sum.x/things.Count, 0, sum.z/things.Count);
			CameraJumper.TryJump(new GlobalTargetInfo(average, things[0].Map));
		}

		private bool InWorldView() {
			return Find.MainTabsRoot.OpenTab == MainButtonDefOf.World;
		}

		private void TryEscapeWorldView() {
			if(!InWorldView()) return;
			Find.MainTabsRoot.EscapeCurrentTab();
		}

		private bool ThingsAlreadyMatchSelection(List<Thing> things, List<object> selection) {
			foreach (var thing in things) {
				if (!selection.Contains(thing)) return false;
			}
			return selection.Count == things.Count;
		}
	}
}