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
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class BaseObject3DEditor : IObject3DEditor
	{
		private IObject3D item;
		private View3DWidget view3DWidget;

		public string Name => "General";

		public bool Unlocked => true;

		public IEnumerable<Type> SupportedTypes()
		{
			return new Type[] { typeof(Object3D) };
		}

		public GuiWidget Create(IObject3D item, View3DWidget view3DWidget, ThemeConfig theme)
		{
			this.view3DWidget = view3DWidget;
			this.item = item;
			FlowLayoutWidget mainContainer = new FlowLayoutWidget(FlowDirection.TopToBottom);

			FlowLayoutWidget behavior3DTypeButtons = new FlowLayoutWidget();
			mainContainer.AddChild(behavior3DTypeButtons);

			// put in the button for making the behavior solid
			Button solidBehaviorButton = theme.textImageButtonFactory.Generate("Solid".Localize());
			solidBehaviorButton.Margin = new BorderDouble(5);
			Random rand = new Random();
			solidBehaviorButton.Click += (s, e) =>
			{
				item.Color = new RGBA_Bytes(rand.Next(255), rand.Next(255), rand.Next(255));
				item.OutputType = PrintOutputTypes.Solid;
				view3DWidget.Invalidate();
			};

			behavior3DTypeButtons.AddChild(solidBehaviorButton);

			// put in the button for making the behavior a hole
			Button holeBehaviorButton = theme.textImageButtonFactory.Generate("Hole".Localize());
			holeBehaviorButton.Margin = new BorderDouble(5);
			holeBehaviorButton.Click += (s, e) =>
			{
				item.OutputType = PrintOutputTypes.Hole;
				view3DWidget.Invalidate();
			};

			behavior3DTypeButtons.AddChild(holeBehaviorButton);

			// put in the button for making the behavior support
			Button supportBehaviorButton = theme.textImageButtonFactory.Generate("Support".Localize());
			supportBehaviorButton.Margin = new BorderDouble(5);
			supportBehaviorButton.Click += (s, e) =>
			{
				item.OutputType = PrintOutputTypes.Support;
				view3DWidget.Invalidate();
			};

			behavior3DTypeButtons.AddChild(supportBehaviorButton);

			return mainContainer;
		}
	}
}