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
		private bool slotSuffix;

		public ISlotAwareContextMenuProvider AtSlot(int newSlotId) {
			slot = newSlotId;
			return this;
		}

		public ISlotAwareContextMenuProvider WithSlotSuffix(bool useSuffix) {
			slotSuffix = useSuffix;
			return this;
		}

		public DefensivePositionContextMenuProvider(PawnSavedPositionHandler handler) {
			this.handler = handler;
		}

		public IEnumerator<FloatMenuOption> GetEnumerator() {
			string TranslateWithSuffix(string key) => key.Translate(slotSuffix ? "DefPos_context_slotSuffix".Translate(slot + 1) : string.Empty);
			yield return new FloatMenuOption(TranslateWithSuffix("DefPos_context_assignPosition"), () => handler.SetDefensivePosition(slot));
			if (handler.OwnerHasValidSavedPositionInSlot(slot)) {
				yield return new FloatMenuOption(TranslateWithSuffix("DefPos_context_clearPosition"), () => handler.DiscardSavedPosition(slot));
			}
			yield return new FloatMenuOption(TranslateWithSuffix("DefPos_context_toggleAdvanced"), () => DefensivePositionsManager.Instance.ScheduleAdvancedModeToggle());
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}