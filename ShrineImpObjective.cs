using RoR2.UI;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using IL.JBooth.VertexPainterPro;
using JetBrains.Annotations;

namespace Evaisa.BetterShrines
{
    public class ShrineImpObjective : ObjectivePanelController.ObjectiveTracker
	{
		public int cachedTotalImpCount;
		public int cachedKilledImpCount;
		public int timeLeft;
		public string color;

		private ShrineImpBehaviour shrineImpBehaviour
		{
			get
			{
				return this.sourceDescriptor.source as ShrineImpBehaviour;
			}
		}

		private bool UpdateCachedValues()
		{
			var totalImpCount = this.shrineImpBehaviour.instance.originalImpCount;
			var killedImpCount = this.shrineImpBehaviour.instance.killedImpCount;

			/*
			if(this.shrineImpBehaviour.timeLeft < 0 || this.shrineImpBehaviour.instance.killedImpCount == this.shrineImpBehaviour.instance.originalImpCount)
            {
				HUD.readOnlyInstanceList[0].objectivePanelController.RemoveObjectiveTracker(this);
            }
			*/

			if (this.cachedTotalImpCount != totalImpCount || this.cachedKilledImpCount != killedImpCount || timeLeft != this.shrineImpBehaviour.timeLeft)
            {
				this.color = this.shrineImpBehaviour.instance.impColor;
				this.cachedKilledImpCount = killedImpCount;
				this.cachedTotalImpCount = totalImpCount;
				this.timeLeft = this.shrineImpBehaviour.timeLeft;
				return true;
			}
			return false;
		}

		public ShrineImpObjective()
		{
			this.baseToken = "OBJECTIVE_KILL_TINY_IMPS";
		}

		public override string GenerateString()
		{
			this.UpdateCachedValues();
			return string.Format(Language.GetString(this.baseToken), this.color, this.cachedKilledImpCount, this.cachedTotalImpCount, this.timeLeft);
		}

		public override bool IsDirty()
		{
			return !(this.sourceDescriptor.source as ShrineImpBehaviour) || this.UpdateCachedValues();
		}
	}
}
