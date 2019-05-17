using System.Collections.Generic;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Represents a saved squad of pawns or buildings
	/// </summary>
	public class PawnSquad : IExposable {
		// the key this squad is assigned to
		public int squadId;
		// thingIds of assigned pawns of buildings
		public List<int> pawnIds;

		public PawnSquad() {
			pawnIds = new List<int>();
		}

		public void ExposeData() {
			Scribe_Values.Look(ref squadId, "squadId", 0);
			Scribe_Collections.Look(ref pawnIds, "pawnIds", LookMode.Value);
			if (pawnIds == null) pawnIds = new List<int>();
		}
	}
}