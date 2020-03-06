using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Represents a saved squad of pawns or buildings
	/// </summary>
	public class PawnSquad : IExposable {
		// the key this squad is assigned to
		private int squadId;
		public int SquadId {
			get { return squadId; }
			set { squadId = value; }
		}

		// pawns and buildings assigned to this squad
		public List<Thing> members = new List<Thing>();

		public IEnumerable<Thing> ValidMembers {
			get { return members.Where(IsEligibleForMembership); }
		}

		public bool ShouldBeSaved {
			get { return ValidMembers.Any(); }
		}

		public PawnSquad() {
			EnsureMembersInitialized();
		}

		public void AssignMembers(IEnumerable<Thing> newMembers) {
			members = new List<Thing>(newMembers);
		}

		public void ExposeData() {
			if (Scribe.mode == LoadSaveMode.Saving) {
				RemoveInvalidSquadMembers();
			}
			Scribe_Values.Look(ref squadId, "squadId");
			Scribe_Collections.Look(ref members, "members", LookMode.Reference);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
				EnsureMembersInitialized();
				RemoveInvalidSquadMembers();
			}
		}

		private static bool IsEligibleForMembership(Thing t) {
			return t != null && !t.Destroyed && t.Faction != null && t.Faction.IsPlayer;
		}

		private void EnsureMembersInitialized() {
			if(members == null) members = new List<Thing>();
		}

		private void RemoveInvalidSquadMembers() {
			members.RemoveAll(t => !IsEligibleForMembership(t));
		}
	}
}