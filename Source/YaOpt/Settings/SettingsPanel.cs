using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace YaOpt.Settings
{
	public class SettingsPanel
	{
		internal readonly YaOptSettings _settings;

		private enum SettingsTab
		{
			Main,
			Fps,
			Tps,
			Misc
		}

		private SettingsTab _selectedTab = SettingsTab.Main;
		private Vector2 _optionScrollPos = Vector2.zero;
		private Vector2 _descTextScrollPos = Vector2.zero;
		private float _optionViewHeight;
		private OptimizationCategory _categoryFilter = OptimizationCategory.Any;
		private OptimizationOption _lastMouseOverOption = null;
		private Window _lastWindow = null;
		private string _showingDesc = string.Empty;
		private bool _checkOptionChanged = false;

		public SettingsPanel(YaOptSettings settings)
		{
			_settings = settings;
		}

		public void Draw(Rect inRect)
		{
			var currentWindow = Find.WindowStack.currentlyDrawnWindow;
			if (_lastWindow != currentWindow)
			{
				_lastWindow = currentWindow;
				_optionScrollPos = Vector2.zero;
				_descTextScrollPos = Vector2.zero;
				_lastMouseOverOption = null;
				_showingDesc = string.Empty;
			}

			var tabHeader = inRect;
			tabHeader.y += 35f;

			var tabBody = tabHeader;
			tabBody.height -= 40f;
			Widgets.DrawMenuSection(tabBody);

			var list = new List<TabRecord>
			{
				new TabRecord("YaOpt.Setting.Tab.Main".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Any;
					_selectedTab = SettingsTab.Main;
				}, _selectedTab == SettingsTab.Main),
				new TabRecord("YaOpt.Setting.Tab.Fps".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Fps;
					_selectedTab = SettingsTab.Fps;
				}, _selectedTab == SettingsTab.Fps),
				new TabRecord("YaOpt.Setting.Tab.Tps".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Tps;
					_selectedTab = SettingsTab.Tps;
				}, _selectedTab == SettingsTab.Tps),
				new TabRecord("YaOpt.Setting.Tab.Misc".Translate(), delegate
				{
					_optionScrollPos = Vector2.zero;
					_descTextScrollPos = Vector2.zero;
					_lastMouseOverOption = null;
					_showingDesc = string.Empty;
					_categoryFilter = OptimizationCategory.Misc;
					_selectedTab = SettingsTab.Misc;
				}, _selectedTab == SettingsTab.Misc),
			};
			TabDrawer.DrawTabs(tabHeader, list);
			DrawPage(tabBody.ContractedBy(10));

			if (_checkOptionChanged)
			{
				if (!YaOptGlobal.AnyOptionChanged())
				{
					_checkOptionChanged = false;
				}
				else
				{
					var messageRect = new Rect(inRect.x + 5, inRect.yMax + 5, inRect.width * 0.4f, 50);
					Text.Font = GameFont.Tiny;
					Widgets.Label(messageRect, "YaOpt.Setting.Message.RequireReload".Translate());
					Text.Font = GameFont.Small;
				}
			}
		}

		private void DrawPage(Rect inRect)
		{
			inRect.SplitVertically(inRect.width * 0.6f, out var leftRect, out var rightRect);

			Widgets.DrawLineVertical(leftRect.xMax - 5f, leftRect.yMin, leftRect.height);
			leftRect = leftRect.ContractedBy(0, 5);
			leftRect.width -= 25;
			var viewRect = new Rect(0, 0, leftRect.width - 25, _optionViewHeight);
			Widgets.BeginScrollView(leftRect, ref _optionScrollPos, viewRect, true);
			var listing = new Listing_Standard
			{
				verticalSpacing = 4f,
				maxOneColumn = true,
				ColumnWidth = viewRect.width * 0.93f
			};
			listing.Begin(viewRect);
			var lastCategory = string.Empty;
			var lastSubCategory = string.Empty;
			switch (_selectedTab)
			{
				case SettingsTab.Main:
					lastCategory = GetCategoryText(OptimizationCategory.Main);
					break;
				case SettingsTab.Fps:
					lastCategory = GetCategoryText(OptimizationCategory.Fps);
					break;
				case SettingsTab.Tps:
					lastCategory = GetCategoryText(OptimizationCategory.Tps);
					break;
				case SettingsTab.Misc:
					lastCategory = GetCategoryText(OptimizationCategory.Misc);
					break;
			}
			foreach (var option in _settings.AllOptimizations)
			{
				if ((_categoryFilter & option.Category) > 0 && option.ShouldShow(_settings))
				{
					var cateText = GetCategoryText(option.Category);
					if (lastCategory != cateText)
					{
						lastCategory = cateText;
						lastSubCategory = string.Empty;
						if (!string.IsNullOrWhiteSpace(cateText))
						{
							Text.Font = GameFont.Medium;
							listing.Label(cateText);
							Text.Font = GameFont.Small;
						}
					}

					var subCateText = !string.IsNullOrWhiteSpace(option.SubCategory) ?
						option.SubCategory.Translate().ToString() :
						string.Empty;
					if (lastSubCategory != subCateText)
					{
						lastSubCategory = subCateText;
						if (!string.IsNullOrWhiteSpace(subCateText))
						{
							listing.Label($"<b><i>{subCateText}</i></b>");
						}
					}
					DrawOption(listing, option);
				}
			}
			listing.End();
			Widgets.EndScrollView();
			if (Event.current.type == EventType.Layout)
			{
				_optionViewHeight = listing.CurHeight;
			}

			Rect drawRect;
#if DEBUG
			rightRect.SplitHorizontally(rightRect.height - 35f, out rightRect, out drawRect);
			Widgets.ButtonText(drawRect.ContractedBy(30, 2.5f), "YaOpt.Setting.Button.ShowDebugMenu".Translate());
			string btnTextDisable;
			string btnTextEnable;
#endif
			OptimizationCategory category;
			switch (_selectedTab)
			{
				case SettingsTab.Main:
					btnTextDisable = "YaOpt.Setting.Button.DisableAll";
					btnTextEnable = "YaOpt.Setting.Button.EnableAll";
					category = OptimizationCategory.Any;
					break;
				case SettingsTab.Fps:
					btnTextDisable = "YaOpt.Setting.Button.DisableAllFps";
					btnTextEnable = "YaOpt.Setting.Button.EnableAllFps";
					category = OptimizationCategory.Fps;
					break;
				case SettingsTab.Tps:
					btnTextDisable = "YaOpt.Setting.Button.DisableAllTps";
					btnTextEnable = "YaOpt.Setting.Button.EnableAllTps";
					category = OptimizationCategory.Tps;
					break;
				case SettingsTab.Misc:
					btnTextDisable = "YaOpt.Setting.Button.DisableAllMisc";
					btnTextEnable = "YaOpt.Setting.Button.EnableAllMisc";
					category = OptimizationCategory.Misc;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			rightRect.SplitHorizontally(rightRect.height - 35f, out rightRect, out drawRect);
			if (Widgets.ButtonText(drawRect.ContractedBy(30, 2.5f), btnTextDisable.Translate()))
			{
				SetAllOption(false, category);
			}
			rightRect.SplitHorizontally(rightRect.height - 35f, out rightRect, out drawRect);
			if (Widgets.ButtonText(drawRect.ContractedBy(30, 2.5f), btnTextEnable.Translate()))
			{
				SetAllOption(true, category);
			}

			rightRect = rightRect.ContractedBy(10);
			Widgets.LabelScrollable(rightRect, _showingDesc, ref _descTextScrollPos, true, false, true);
		}

		private void DrawOption(Listing_Standard listing, OptimizationOption option)
		{
			var label = option.Name.Translate().ToString();
			var hasNoteS = !string.IsNullOrWhiteSpace(option.NoteStability);
			var hasNoteC = !string.IsNullOrWhiteSpace(option.NoteCompatibility);
			if (hasNoteS && hasNoteC)
			{
				label = string.Concat(label, " <color=#FF4040>[S]</color><color=#DEB0D0>[C]</color>");
			}
			else if (hasNoteS)
			{
				label = string.Concat(label, " <color=#FF4040>[S]</color>");
			}
			else if (hasNoteC)
			{
				label = string.Concat(label, " <color=#DEB0D0>[C]</color>");
			}
			var enabled = option._enabled;
			var disabledByDef = CompatibilityDef.CachedBannedOptimizations.Contains(option.SettingId);
			DrawCheckboxLabeled(listing, label, enabled, disabledByDef, out var mouseOver, out var result);
			if (mouseOver && _lastMouseOverOption != option)
			{
				MouseOverOption(option);
			}
			if (result != enabled && !disabledByDef)
			{
				option._enabled = result;
				if (!option.Validate(false, true, out var reason))
				{
					Messages.Message("YaOpt.Setting.InvalidOption".Translate().ToString() + reason,
						null, MessageTypeDefOf.RejectInput, false);
				}
				CheckIfOptionChanged();
			}
			if (option.FuncPostDraw != null)
				option.FuncPostDraw(this, listing, option);
			listing.Gap(listing.verticalSpacing);
		}

		private static void DrawCheckboxLabeled(Listing_Standard listing, string label,
			bool isChecked, bool isDisabled, out bool mouseOver, out bool result, float widthOffset = 0)
		{
			mouseOver = false;
			result = false;
			Rect rect = listing.GetRect(Text.CalcHeight(label, listing.ColumnWidth));
			rect.width += widthOffset;
			//rect.width = Math.Min(rect.width + 24f, listing.ColumnWidth);
			Rect? boundingRectCached = listing.BoundingRectCached;
			if (boundingRectCached.HasValue)
			{
				ref Rect local = ref rect;
				Rect other = boundingRectCached.Value;
				if (!local.Overlaps(other))
				{
					listing.Gap(listing.verticalSpacing);
					return;
				}
			}
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
				mouseOver = true;
			}
			var enabled = isChecked;
			Widgets.CheckboxLabeled(rect, label, ref enabled, isDisabled);
			result = enabled;
		}

		private void MouseOverOption(OptimizationOption option)
		{
			_lastMouseOverOption = option;
			var sb = new StringBuilder();

			if (CompatibilityDef.CachedBannedOptimizations.Contains(option.SettingId))
			{
				sb.Append("<color=#FF2020>")
					.Append("YaOpt.Setting.Note.Banned".Translate(
						CompatibilityDef.CachedBannedBy[option.SettingId]))
					.AppendLine("</color>");
			}

			sb.AppendLine(option.Desc.Translate());

			if (!string.IsNullOrWhiteSpace(option.NoteStability))
			{
				sb.Append("\n\n").Append("<color=#FF4040>").Append("YaOpt.Setting.Note.Stability".Translate()).Append("\n")
					.Append(option.NoteStability.Translate()).Append("</color>");
			}

			if (!string.IsNullOrWhiteSpace(option.NoteCompatibility))
			{
				sb.Append("\n\n").Append("<color=#DEB0D0>").Append("YaOpt.Setting.Note.Compatibility".Translate()).Append("\n")
					.Append(option.NoteCompatibility.Translate()).Append("</color>");
			}
			_showingDesc = sb.ToString();
		}

		public static string GetCategoryText(OptimizationCategory category)
		{
			switch (category)
			{
				case OptimizationCategory.Hidden:
				case OptimizationCategory.Main:
					return string.Empty;
				case OptimizationCategory.Fps: return "YaOpt.Setting.Category.Fps".Translate();
				case OptimizationCategory.Tps: return "YaOpt.Setting.Category.Tps".Translate();
				case OptimizationCategory.Misc: return "YaOpt.Setting.Category.Misc".Translate();
				case OptimizationCategory.Any: return string.Empty;
				default:
					throw new ArgumentOutOfRangeException(nameof(category), category, null);
			}
		}

		private void SetAllOption(bool enable, OptimizationCategory category)
		{
			var filter = enable ? OptimizationFlags.IgnoreEnableAll : OptimizationFlags.IgnoreDisableAll;
			foreach (var optimization in _settings.AllOptimizations)
			{
				if ((optimization.Category & category) > 0 && (optimization.Flags & filter) == 0)
				{
					optimization.Enabled = enable;
				}
			}
			CheckIfOptionChanged();
		}

		private void CheckIfOptionChanged()
		{
			if (YaOptGlobal.AnyOptionChanged())
			{
				_checkOptionChanged = true;
			}
		}

		public static void MapMeshUpdateThrottlePostDraw(SettingsPanel panel,
			Listing_Standard listing, OptimizationOption option)
		{
			if (option.Enabled)
			{
				listing.Indent();
				var rect = listing.GetRect(30);
				listing.Gap(-30);
				var result = (int)listing.SliderLabeled(
					"YaOpt.Setting.Option.MapMeshUpdateThrottle.UpdateInterval".Translate(panel._settings.MapMeshUpdateInterval),
					panel._settings.MapMeshUpdateInterval, 100, 1000);
				panel._settings.MapMeshUpdateInterval = result / 100 * 100;
				listing.Outdent();
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
					if (panel._lastMouseOverOption != null)
					{
						panel._lastMouseOverOption = null;
						panel._showingDesc = "YaOpt.Setting.Option.MapMeshUpdateThrottle.UpdateInterval.Desc".Translate();
					}
				}
			}
		}

		public static void LazyTextureLoadPostDraw(SettingsPanel panel,
			Listing_Standard listing, OptimizationOption option)
		{
			if (option.Enabled)
			{
				listing.Gap(listing.verticalSpacing);
				listing.Indent();
				var ddsOnly = panel._settings.LazyTextureLoadDdsOnly;
				DrawCheckboxLabeled(listing, "YaOpt.Setting.Option.LazyTextureLoad.DdsOnly".Translate(),
					ddsOnly, false, out var mouseOver, out var result, -12);
				if (mouseOver)
				{
					panel._lastMouseOverOption = null;
					panel._showingDesc = "YaOpt.Setting.Option.LazyTextureLoad.DdsOnly.Desc".Translate();
				}
				if (ddsOnly != result)
					panel._settings.LazyTextureLoadDdsOnly = result;
				listing.Outdent();
				listing.Gap(listing.verticalSpacing);
			}
		}
	}
}
