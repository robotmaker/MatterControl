﻿/*
Copyright (c) 2016, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.DataStorage.ClassicDB;
using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MatterHackers.MatterControl.SlicerConfiguration
{
	public class PresetsContext
	{
		public Dictionary<string, SettingsLayer> PresetsDictionary { get; }
		public SettingsLayer PersistenceLayer { get; }
		public Action<string> SetAsActive { get; set; }
		public NamedSettingsLayers LayerType { get; set; }

		private string presetsKey;

		public PresetsContext(Dictionary<string, SettingsLayer> parentDictionary, string presetsKey)
		{
			this.presetsKey = presetsKey;
			this.PersistenceLayer = parentDictionary[presetsKey];
			this.PresetsDictionary = parentDictionary;
		}
	}

	public class SlicePresetsWindow : SystemWindow
	{
		private PresetsContext presetsContext;
		private MHTextEditWidget presetNameInput;

		private string initialPresetName = null;
		private string configFileExtension = "slice";

		private static Regex numberMatch = new Regex("\\s*\\(\\d+\\)", RegexOptions.Compiled);


		public SlicePresetsWindow(PresetsContext presetsContext)
				: base(641, 481)
		{
			this.presetsContext = presetsContext;
			this.AlwaysOnTopOfMain = true;
			this.Title = LocalizedString.Get("Slice Presets Editor");
			this.MinimumSize = new Vector2(640, 480);
			this.AnchorAll();

			var linkButtonFactory = new LinkButtonFactory()
			{
				fontSize = 8,
				textColor = ActiveTheme.Instance.SecondaryAccentColor
			};

			var buttonFactory = new TextImageButtonFactory()
			{
				normalTextColor = ActiveTheme.Instance.PrimaryTextColor,
				hoverTextColor = ActiveTheme.Instance.PrimaryTextColor,
				disabledTextColor = ActiveTheme.Instance.PrimaryTextColor,
				pressedTextColor = ActiveTheme.Instance.PrimaryTextColor,
				borderWidth = 0
			};

			FlowLayoutWidget mainContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Padding = new BorderDouble(3)
			};
			mainContainer.AnchorAll();

			mainContainer.AddChild(GetTopRow());
			mainContainer.AddChild(GetMiddleRow());
			mainContainer.AddChild(GetBottomRow(buttonFactory));

			this.AddChild(mainContainer);

			BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;
		}

		private FlowLayoutWidget GetTopRow()
		{
			var metaContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.ParentLeftRight,
				Padding = new BorderDouble(0, 3)
			};

			var labelText = new TextWidget("Preset Name:".Localize(), pointSize: 14)
			{
				TextColor = ActiveTheme.Instance.PrimaryTextColor,
				VAnchor = VAnchor.ParentCenter,
				Margin = new BorderDouble(right: 4)
			};

			initialPresetName = presetsContext.PersistenceLayer.Name;
			presetNameInput = new MHTextEditWidget(initialPresetName);
			presetNameInput.HAnchor = HAnchor.ParentLeftRight;

			var firstRow = new FlowLayoutWidget(hAnchor: HAnchor.ParentLeftRight);
			firstRow.AddChild(labelText);
			firstRow.AddChild(presetNameInput);

			var secondRow = new FlowLayoutWidget(hAnchor: HAnchor.ParentLeftRight);
			secondRow.AddChild(new GuiWidget(labelText.Width + 4, 1));

			metaContainer.AddChild(firstRow);
			metaContainer.AddChild(secondRow);

			return metaContainer;
		}

		private GuiWidget GetMiddleRow()
		{
			var settings = ActiveSliceSettings.Instance;
			var layerCascade = new List<SettingsLayer> { presetsContext.PersistenceLayer, settings.OemLayer, settings.BaseLayer};

			var settingsWidget = new SliceSettingsWidget(layerCascade, presetsContext.LayerType);
			settingsWidget.settingsControlBar.Visible = false;

			return settingsWidget;
		}

		private string GetNonCollidingName(string profileName, IEnumerable<string> existingNames)
		{
			if (!existingNames.Contains(profileName))
			{
				return profileName;
			}
			else
			{

				int currentIndex = 1;
				string possiblePrinterName;

				do
				{
					possiblePrinterName = String.Format("{0} ({1})", profileName, currentIndex++);
				} while (existingNames.Contains(possiblePrinterName));

				return possiblePrinterName;
			}
		}

		private FlowLayoutWidget GetBottomRow(TextImageButtonFactory buttonFactory)
		{
			var container = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.ParentLeftRight,
				Margin = new BorderDouble(top: 3)
			};

			Button duplicateButton = buttonFactory.Generate("Duplicate".Localize());
			duplicateButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					string sanitizedName = numberMatch.Replace(presetNameInput.Text, "").Trim();
					string newProfileName = GetNonCollidingName(sanitizedName, presetsContext.PresetsDictionary.Values.Select(preset => preset.Name));

					this.Close();

					var clonedLayer = presetsContext.PersistenceLayer.Clone();
					clonedLayer.Name = newProfileName;
					presetsContext.PresetsDictionary[clonedLayer.ID] = clonedLayer;

					presetsContext.SetAsActive(clonedLayer.ID);
				});
			};

			Button importButton = buttonFactory.Generate("Import".Localize());
			importButton.Click += (s, e) =>
			{
				throw new NotImplementedException();
			};

			Button exportButton = buttonFactory.Generate("Export".Localize());
			exportButton.Click += (s, e) => UiThread.RunOnIdle(SaveAs);

			Button closeButton = buttonFactory.Generate("Close".Localize());
			closeButton.Click += (sender, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					if (initialPresetName != presetNameInput.Text)
					{
						presetsContext.PersistenceLayer.Name = presetNameInput.Text;

						// TODO: If we get to the point where we refresh rather than reload, we need
						// to rebuild the target droplist to display the new name
						ApplicationController.Instance.ReloadAdvancedControlsPanel();
					}
					this.Close();
				});
			};

			container.AddChild(duplicateButton);

			//Only show duplicate/import/export buttons if setting has been saved.
			if (false)
			{
				container.AddChild(importButton);
				container.AddChild(exportButton);
			}

			container.AddChild(new HorizontalSpacer());
			container.AddChild(closeButton);

			return container;
		}

		private void SaveAs()
		{
			FileDialog.SaveFileDialog(
				new SaveFileDialogParams("Save Slice Preset|*." + configFileExtension)
				{
					FileName = presetNameInput.Text
				},
				(saveParams) =>
				{
					throw new NotImplementedException();

					if (!string.IsNullOrEmpty(saveParams.FileName))
					{
						// TODO: If we stil want this functionality, it should be moved to a common helper method off of SettingsLayer and resused throughout
						//
						// GenerateConfigFile(saveParams.FileName) ...

						//List<string> configFileAsList = new List<string>();

						//foreach (KeyValuePair<String, SliceSetting> setting in windowController.ActivePresetLayer.settingsDictionary)
						//{
						//	string settingString = string.Format("{0} = {1}", setting.Value.Name, setting.Value.Value);
						//	configFileAsList.Add(settingString);
						//}
						//string configFileAsString = string.Join("\n", configFileAsList.ToArray());

						//FileStream fs = new FileStream(fileName, FileMode.Create);
						//StreamWriter sw = new System.IO.StreamWriter(fs);
						//sw.Write(configFileAsString);
						//sw.Close();
					}
				});
		}
	}
}