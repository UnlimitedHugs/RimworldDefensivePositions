using HugsLib;
using HugsLib.Settings;
using Verse;

namespace DefensivePositions {
	/**
	 * This is a plug to make use of the facilities provided by HugsLib.
	 * TODO: it might be a good idea to convert the manager to ModBase when the Rimworld version rolls over
	 */
	public class DefensivePositionsMod : ModBase {
		public static DefensivePositionsMod Instance { get; private set; }

		public SettingHandle<bool> FirstSlotHotkeySetting { get; private set; }

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		public override string ModIdentifier {
			get { return "DefensivePositions"; }
		}

		private DefensivePositionsMod() {
			Instance = this;
		}

		public override void DefsLoaded() {
			FirstSlotHotkeySetting = Settings.GetHandle("firstSlotHotkey", "setting_hotkeyMode_label".Translate(), "setting_hotkeyMode_desc".Translate(), true);
		}
	}
}