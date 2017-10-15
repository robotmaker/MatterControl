﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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
using System;
using System.Threading;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ActionBar;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.EeProm;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class PrinterActionsBar : FlowLayoutWidget
	{
		private PrinterConfig printer;
		private EventHandler unregisterEvents;
		private static EePromMarlinWindow openEePromMarlinWidget = null;
		private static EePromRepetierWindow openEePromRepetierWidget = null;
		private string noEepromMappingMessage = "Oops! There is no eeprom mapping for your printer's firmware.".Localize() + "\n\n" + "You may need to wait a minute for your printer to finish initializing.".Localize();
		private string noEepromMappingTitle = "Warning - No EEProm Mapping".Localize();

		private OverflowMenu overflowDropdown;

		private CancellationTokenSource gcodeLoadCancellationTokenSource;

		public PrinterActionsBar(PrinterConfig printer, PrinterTabPage printerTabPage, ThemeConfig theme)
		{
			this.printer = printer;
			this.HAnchor = HAnchor.Stretch;
			this.VAnchor = VAnchor.Fit;

			this.AddChild(new PrinterConnectButton(printer, theme));
			this.AddChild(new PrintActionRow(printer, theme, this));
			this.AddChild(new SlicePopupMenu(printer, theme, printerTabPage));

			// put in the detail message
			var printerConnectionDetail = new TextWidget("")
			{
				Margin = new BorderDouble(5, 0),
				TextColor = ActiveTheme.Instance.PrimaryTextColor,
				AutoExpandBoundsToText = true,
				PointSize = 8
			};
			printer.Connection.PrintingStateChanged.RegisterEvent((s, e) =>
			{
				printerConnectionDetail.Text = printer.Connection.PrinterConnectionStatus;
			}, ref unregisterEvents);
			this.AddChild(printerConnectionDetail);

			this.AddChild(new HorizontalSpacer());

			bool shareTemp = printer.Settings.GetValue<bool>(SettingsKey.extruders_share_temperature);
			int extruderCount = shareTemp ? 1 : printer.Settings.GetValue<int>(SettingsKey.extruder_count);

			for (int extruderIndex = 0; extruderIndex < extruderCount; extruderIndex++)
			{
				this.AddChild(new TemperatureWidgetHotend(printer, extruderIndex, theme.MenuButtonFactory)
				{
					Margin = new BorderDouble(right: 10)
				});
			}

			if (printer.Settings.GetValue<bool>(SettingsKey.has_heated_bed))
			{
				this.AddChild(new TemperatureWidgetBed(printer));
			}

			overflowDropdown = new OverflowMenu(IconColor.Theme)
			{
				AlignToRightEdge = true,
				Name = "Printer Overflow Menu",
				Margin = theme.ButtonSpacing
			};
			overflowDropdown.DynamicPopupContent = GeneratePrinterOverflowMenu;

			// Deregister on close
			this.Closed += (s, e) =>
			{
				overflowDropdown.DynamicPopupContent = GeneratePrinterOverflowMenu;
			};

			this.AddChild(overflowDropdown);
		}

		public override void AddChild(GuiWidget childToAdd, int indexInChildrenList = -1)
		{
			childToAdd.VAnchor |= VAnchor.Center;
			base.AddChild(childToAdd, indexInChildrenList);
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			gcodeLoadCancellationTokenSource?.Cancel();
			base.OnClosed(e);
		}

		private GuiWidget GeneratePrinterOverflowMenu()
		{
			var menuActions = new NamedAction[]
			{
				new NamedAction()
				{
					Icon = AggContext.StaticData.LoadIcon("memory_16x16.png", 16, 16),
					Title = "Configure EEProm".Localize(),
					Action = configureEePromButton_Click
				},
				new NamedAction()
				{
					Title = "Rename Printer".Localize(),
					Action = () =>
					{
						var renameItemPage = new RenameItemPage(
						"Rename Printer".Localize() + ":",
						printer.Settings.GetValue(SettingsKey.printer_name),
						(newName) =>
						{
							if (!string.IsNullOrEmpty(newName))
							{
								printer.Settings.SetValue(SettingsKey.printer_name, newName);
							}
						});

						WizardWindow.Show(renameItemPage);
					}
				},
				new NamedAction() { Title = "----" },
				new NamedAction()
				{
					Title = "Delete Printer".Localize(),
					Action = () =>
					{
						StyledMessageBox.ShowMessageBox(
							(doDelete) =>
							{
								if (doDelete)
								{
									printer.Settings.Helpers.SetMarkedForDelete(true);
								}
							},
							"Are you sure you want to delete your currently selected printer?".Localize(),
							"Delete Printer?".Localize(),
							StyledMessageBox.MessageType.YES_NO,
							"Delete Printer".Localize());
					}
				}
			};
			return ApplicationController.Instance.Theme.CreatePopupMenu(menuActions);
		}

		private void configureEePromButton_Click()
		{
			UiThread.RunOnIdle(() =>
			{
#if false // This is to force the creation of the repetier window for testing when we don't have repetier firmware.
                        new MatterHackers.MatterControl.EeProm.EePromRepetierWidget();
#else
				switch (printer.Connection.FirmwareType)
				{
					case FirmwareTypes.Repetier:
						if (openEePromRepetierWidget != null)
						{
							openEePromRepetierWidget.BringToFront();
						}
						else
						{
							openEePromRepetierWidget = new EePromRepetierWindow(printer.Connection);
							openEePromRepetierWidget.Closed += (RepetierWidget, RepetierEvent) =>
							{
								openEePromRepetierWidget = null;
							};
						}
						break;

					case FirmwareTypes.Marlin:
						if (openEePromMarlinWidget != null)
						{
							openEePromMarlinWidget.BringToFront();
						}
						else
						{
							openEePromMarlinWidget = new EePromMarlinWindow(printer.Connection);
							openEePromMarlinWidget.Closed += (marlinWidget, marlinEvent) =>
							{
								openEePromMarlinWidget = null;
							};
						}
						break;

					default:
						printer.Connection.SendLineToPrinterNow("M115");
						StyledMessageBox.ShowMessageBox(null, noEepromMappingMessage, noEepromMappingTitle, StyledMessageBox.MessageType.OK);
						break;
				}
#endif
			});
		}
	}
}