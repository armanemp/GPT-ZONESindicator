using System;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderEqualLevels()
        {
            int equalCap = EqualMaxVisible > 0 ? EqualMaxVisible : int.MaxValue;
            int count = 0;

            for (int k = _equalLevelsState.Clusters.Count - 1; k >= 0; k--)
            {
                EqCluster cluster = _equalLevelsState.Clusters[k];
                bool ended = cluster.Swept || cluster.Broken;
                bool eligible = ShowEqual && (!ended || ShowSweptEqual);
                Item item;
                bool hasItem = _equalLevelsRenderingState.ByCluster.TryGetValue(cluster, out item);

                DateTime end = ended ? cluster.EndTime : _futureTime;
                if (end <= cluster.FirstTime)
                    end = cluster.FirstTime.Add(_barSpan);
                DateTime labelTime = MidTime(cluster.FirstTime, cluster.LastTime);
                bool inViewport = eligible
                    && (TimePriceIntersectsViewport(cluster.FirstTime, end, cluster.Level)
                        || ItemLabelIntersectsViewport(labelTime, cluster.Level));

                if (!inViewport || count >= equalCap)
                {
                    if (hasItem)
                    {
                        SyncItem(item, false);
                        _equalLevelsRenderingState.ByCluster.Remove(cluster);
                    }
                    continue;
                }

                if (!hasItem)
                {
                    item = new Item { Name = NextName("E") };
                    _equalLevelsRenderingState.ByCluster.Add(cluster, item);
                }

                item.Y = cluster.Level;
                item.T1 = cluster.FirstTime;
                item.T2 = end;
                item.Thick = 1;
                item.Style = LiquidityStyle;
                item.Col = WithOpacity(_cLiquidity, StructOpacity);
                item.Label = (cluster.High ? "EQH" : "EQL")
                    + (cluster.Count > 2 ? " x" + cluster.Count.ToString(CultureInfo.InvariantCulture) : "");
                item.LabelTime = labelTime;
                item.LabelY = cluster.Level;
                item.LabelH = HorizontalAlignment.Center;
                item.LabelV = cluster.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;

                SyncItem(item, true);
                count++;
            }
        }

        private void RenderSweeps()
        {
            int sweepCap = SweepMaxVisible > 0 ? SweepMaxVisible : int.MaxValue;
            int count = 0;

            for (int k = _liquiditySweepState.Events.Count - 1; k >= 0; k--)
            {
                SweepEv sweep = _liquiditySweepState.Events[k];
                Item item;
                bool hasItem = _liquiditySweepRenderingState.ByEvent.TryGetValue(sweep, out item);
                DateTime end = sweep.T2 <= sweep.T1 ? sweep.T1.Add(_barSpan) : sweep.T2;
                bool inViewport = ShowSweeps
                    && (TimePriceIntersectsViewport(sweep.T1, end, sweep.Level)
                        || ItemLabelIntersectsViewport(sweep.T2, sweep.Level));

                if (!inViewport || count >= sweepCap)
                {
                    if (hasItem)
                    {
                        SyncItem(item, false);
                        _liquiditySweepRenderingState.ByEvent.Remove(sweep);
                    }
                    continue;
                }

                if (!hasItem)
                {
                    item = new Item { Name = NextName("W") };
                    _liquiditySweepRenderingState.ByEvent.Add(sweep, item);
                }

                item.Y = sweep.Level;
                item.T1 = sweep.T1;
                item.T2 = sweep.T2;
                item.Thick = 1;
                item.Style = LineStyle.Dots;
                item.Col = WithOpacity(_cLiquidity, 95);
                item.Label = sweep.Bull ? "SSL sweep" : "BSL sweep";
                item.LabelTime = sweep.T2;
                item.LabelY = sweep.Level;
                item.LabelH = HorizontalAlignment.Center;
                item.LabelV = sweep.Bull ? VerticalAlignment.Top : VerticalAlignment.Bottom;

                SyncItem(item, true);
                count++;
            }
        }

    }
}
