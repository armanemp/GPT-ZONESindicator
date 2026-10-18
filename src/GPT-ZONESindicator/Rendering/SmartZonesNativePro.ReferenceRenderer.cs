using System;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderRef()
        {
            int cap = ReferenceMaxVisible > 0 ? ReferenceMaxVisible : int.MaxValue;
            int count = 0;

            for (int k = _referenceLevelRenderingState.Items.Count - 1; k >= 0; k--)
            {
                Item item = _referenceLevelRenderingState.Items[k];

                if (item.Dead)
                {
                    SyncItem(item, false);
                    _referenceLevelRenderingState.Items.RemoveAt(k);
                    continue;
                }

                if (item.Follow)
                {
                    item.LabelTime = _lastBarTime;
                    item.LabelY = item.Y;
                }

                DateTime end = item.T2 <= item.T1 ? item.T1.Add(_barSpan) : item.T2;
                bool inViewport = ItemLineIntersectsViewport(item, end)
                    || ItemLabelIntersectsViewport(item.LabelTime, item.LabelY);
                if (inViewport && count < cap)
                {
                    count++;
                    SyncItem(item, true);
                }
                else
                {
                    SyncItem(item, false);
                }
            }
        }
    }
}
