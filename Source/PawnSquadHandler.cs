using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Allows pawns to be assigned to persistent squads, the members of which can be selected via hotkey press.
	/// </summary>
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
			var squad = TryFindSquad(squadNumber);
			if (assignMode) {
				// Control is held, assign pawns to squad
				var members = new List<Thing>();
				members.AddRange(EnumeratePlayerPawnsInSelectedCaravans());
				if (members.Count == 0) {
					members.AddRange(EnumerateSelectedPlayerThingsOnCurrentMap());
				}
				if (members.Count > 0) {
					// reassign squad with selected pawns
					SetSquadMembers(squadNumber, members);
				} else {
					// no pawns selected, clear squad
					ClearSquad(squadNumber);
				}
			} else {
				// Select pawns that belong to squad
				var selectionBeforeClear = Find.Selector.SelectedObjects.ToList();
				if(!HugsLibUtility.ShiftIsHeld) Find.Selector.ClearSelection();
				var members = squad?.ValidMembers.ToArray();
				if (members != null && members.Length > 0) {
					var matchingCaravans = EnumerateCaravansWithPawns(members.OfType<Pawn>()).ToArray();
					if (matchingCaravans.Length > 0) {
						SelectAndFocusWorldObjects(matchingCaravans);
					} else {
						var things = SelectOnlyThingsOnSameMap(squad.members);
						// focus view on squad if repeat squad key press OR if not currently viewing the map
						if (Find.CurrentMap != things[0].Map || InWorldView() || ThingsAlreadyMatchSelection(things, selectionBeforeClear)) {
							TryEscapeWorldView();
							TryFocusThingGroupCenter(things);
						}
						foreach (var thing in things) {
							Find.Selector.Select(thing);
						}
					}
				} else {
					Messages.Message("DefPos_msg_squadEmpty".Translate(squadNumber), MessageTypeDefOf.RejectInput);
				}
			}
		}

		private static IEnumerable<Pawn> EnumeratePlayerPawnsInSelectedCaravans() {
			foreach (var obj in Find.WorldSelector.SelectedObjects) {
				if (obj is Caravan car && car.Faction != null && car.Faction.IsPlayer && car.pawns != null) {
					foreach (var pawn in car.pawns) {
						if (pawn?.Faction != null && pawn.Faction.IsPlayer) {
							yield return pawn;
						}
					}
				}
			}
		}

		private static IEnumerable<Thing> EnumerateSelectedPlayerThingsOnCurrentMap() {
			foreach (var obj in Find.Selector.SelectedObjects) {
				if (obj is Thing thing && thing.Faction != null && thing.Faction.IsPlayer && (thing is Pawn || thing is Building)) {
					yield return thing;
				}
			}
		}

		private static void SelectAndFocusWorldObjects(IEnumerable<WorldObject> objects) {
			CameraJumper.TryJump(objects.FirstOrDefault());
			Find.WorldSelector.ClearSelection();
			foreach (var obj in objects) {
				Find.WorldSelector.Select(obj);
			}
		}

		private PawnSquad TryFindSquad(int squadNumber) {
			var squadsList = DefensivePositionsManager.Instance.SquadData;
			for (var i = 0; i < squadsList.Count; i++) {
				if (squadsList[i].SquadId == squadNumber) {
					return squadsList[i];
				}
			}
			return null;
		}

		internal void SetSquadMembers(int squadNumber, List<Thing> members) {
			var squad = TryFindSquad(squadNumber);
			if (squad == null) {
				squad = new PawnSquad {SquadId = squadNumber};
				DefensivePositionsManager.Instance.SquadData.Add(squad);
			}
			squad.AssignMembers(members);
			Messages.Message("DefPos_msg_squadAssigned".Translate(members.Count, squadNumber), MessageTypeDefOf.TaskCompletion);
		}

		internal void ClearSquad(int squadNumber) {
			if (DefensivePositionsManager.Instance.SquadData.Remove(TryFindSquad(squadNumber))) {
				Messages.Message("DefPos_msg_squadCleared".Translate(squadNumber), MessageTypeDefOf.TaskCompletion);
			}
		}
		
		// filter the list by dropping all pawns that are on a different map than the first one
		private List<Thing> SelectOnlyThingsOnSameMap(IEnumerable<Thing> things) {
			Map firstMap = null;
			var results = new List<Thing>();
			foreach (var thing in things) {
				if (firstMap == null) firstMap = thing.Map;
				if(thing.Map != firstMap) continue;
				results.Add(thing);
			}
			return results;
		}

		private IEnumerable<Caravan> EnumerateCaravansWithPawns(IEnumerable<Pawn> pawns) {
			var caravans = Find.WorldObjects.Caravans;
			var pawnSet = pawns.ToHashSet();
			foreach (var caravan in caravans) {
				if(!caravan.IsPlayerControlled) continue;
				foreach (var pawn in caravan.PawnsListForReading) {
					if (pawnSet.Contains(pawn)) {
						yield return caravan;
					}
				}
			}
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
			return Find.World.renderer.wantedMode == WorldRenderMode.Planet;
		}

		private void TryEscapeWorldView() {
			if(!InWorldView()) return;
			Find.World.renderer.wantedMode = WorldRenderMode.None;
			if (Find.MainTabsRoot.OpenTab != null) {
				Find.MainTabsRoot.EscapeCurrentTab();
			}
		}

		private bool ThingsAlreadyMatchSelection(List<Thing> things, List<object> selection) {
			foreach (var thing in things) {
				if (!selection.Contains(thing)) return false;
			}
			return selection.Count == things.Count;
		}
	}
}