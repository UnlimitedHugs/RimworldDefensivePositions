using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	public static class Resources {
		[DefOf]
		public static class Hotkeys {
			public static KeyBindingDef DefensivePositionGizmo;
			public static KeyBindingDef DPSelectAllColonists;
			public static KeyBindingDef DPSendAllColonists;
			public static KeyBindingDef DPUndraftAll;
		}

		[DefOf]
		public static class Jobs {
			public static JobDef DPDraftToPosition;
		}

		[StaticConstructorOnStartup]
		public static class Textures {
			public static readonly Texture2D BasicButton = ContentFinder<Texture2D>.Get("UIPositionLarge");
			public static readonly Texture2D[] AdvancedButtonIcons = Enumerable.Range(0, 4)
				.Select(i => ContentFinder<Texture2D>.Get("UIPositionSmall_" + (i + 1))).ToArray();
		}
	}
}