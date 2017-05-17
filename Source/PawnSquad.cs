using System.Collections.Generic;
using Verse;

namespace DefensivePositions {
	/**
	 * Represents a saved squad of pawns
	 */
	public class PawnSquad : IExposable {
		public int squadId;
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