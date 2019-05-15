using System.Collections;
using System.Collections.Generic;
using Verse;

namespace DefensivePositions {
	public interface ISlotAwareContextMenuProvider : IEnumerable<FloatMenuOption> {
		ISlotAwareContextMenuProvider AtSlot(int newSlotId);
	}

	/// <summary>
	/// Creates context menu options for the basic and advanced position buttons.
	/// </summary>
	public class DefensivePositionContextMenuProvider : ISlotAwareContextMenuProvider {
		private readonly PawnSavedPositionHandler handler;
		private int slot;

		public ISlotAwareContextMenuProvider AtSlot(int newSlotId) {
			slot = newSlotId;
			return this;
		}

		public DefensivePositionContextMenuProvider(PawnSavedPositionHandler handler) {
			this.handler = handler;
		}

		public IEnumerator<FloatMenuOption> GetEnumerator() {
			yield return new FloatMenuOption("DefPos_context_assignPosition".Translate(), () => handler.SetDefensivePosition(slot));
			if (handler.OwnerHasValidSavedPositionInSlot(slot)) {
				yield return new FloatMenuOption("DefPos_context_clearPosition".Translate(), () => handler.DiscardSavedPosition(slot));
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}