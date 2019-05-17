using RimWorld;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Stores and resolves references to defs and textures used in the code.
	/// </summary>
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

		[DefOf]
		public static class Things {
			public static ThingDef DPPositionMote;
		}

		[StaticConstructorOnStartup]
		public static class Textures {
			public static readonly Texture2D BasicButton = ContentFinder<Texture2D>.Get("UIPositionLarge");
			public static readonly Texture2D BasicButtonActive = ContentFinder<Texture2D>.Get("UIPositionLargeActive");
			public static readonly Texture2D AdvancedButtonAtlas = ContentFinder<Texture2D>.Get("UIPositionSmallAtlas");

			private const int atlasRows = 4;
			private const float atlasCell = 1f / atlasRows;
			private const float topRow = 1f - atlasCell;
			public static readonly Rect[] IconUVsInactive = {
				new Rect(0, topRow, atlasCell, atlasCell),
				new Rect(atlasCell, topRow, atlasCell, atlasCell), 
				new Rect(0, topRow - atlasCell, atlasCell, atlasCell),
				new Rect(atlasCell, topRow - atlasCell, atlasCell, atlasCell)
			};
			public static readonly Rect[] IconUVsActive = {
				new Rect(atlasCell * 2f, topRow, atlasCell, atlasCell),
				new Rect(atlasCell * 3f, topRow, atlasCell, atlasCell), 
				new Rect(atlasCell * 2f, topRow - atlasCell, atlasCell, atlasCell),
				new Rect(atlasCell * 3f, topRow - atlasCell, atlasCell, atlasCell)
			};
		}
	}
}