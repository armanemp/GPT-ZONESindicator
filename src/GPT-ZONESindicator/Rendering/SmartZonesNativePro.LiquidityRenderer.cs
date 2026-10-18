using System;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderEqualAndSweeps()
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
                if (!eligible)
                {
                    if (hasItem)
                        SyncItem(item, false);
                    continue;
                }

                if (!hasItem)
                {
                    item = new Item { Name = NextName("E") };
                    _equalLevelsRenderingState.ByCluster.Add(cluster, item);
                }
                item.Y = cluster.Level;
                item.T1 = cluster.FirstTime;
                item.T2 = ended ? cluster.EndTime : _futureTime;
                item.Thick = 1;
                item.Style = LiquidityStyle;
                item.Col = WithOpacity(_cLiquidity, StructOpacity);
                item.Label = (cluster.High ? "EQH" : "EQL")
                    + (cluster.Count > 2 ? " x" + cluster.Count.ToString(CultureInfo.InvariantCulture) : "");
                item.LabelTime = MidTime(cluster.FirstTime, cluster.LastTime);
                item.LabelY = cluster.Level;
                item.LabelH = HorizontalAlignment.Center;
                item.LabelV = cluster.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;

                DateTime end = item.T2 <= item.T1 ? item.T1.Add(_barSpan) : item.T2;
                bool inViewport = ItemLineIntersectsViewport(item, end)
                    || ItemLabelIntersectsViewport(item.LabelTime, item.LabelY);
                if (inViewport && count < equalCap)
                {
                    count++;
                    SyncItem(item, true);
                }
                else
                {
                    SyncItem(item, false);
                }
            }

            int sweepCap = SweepMaxVisible > 0 ? SweepMaxVisible : int.MaxValue;
            count = 0;
            for (int k = _liquiditySweepState.Events.Count - 1; k >= 0; k--)
            {
                SweepEv sweep = _liquiditySweepState.Events[k];
                Item item;
                bool hasItem = _liquiditySweepRenderingState.ByEvent.TryGetValue(sweep, out item);
                if (!ShowSweeps)
                {
                    if (hasItem)
                        SyncItem(item, false);
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

                DateTime end = item.T2 <= item.T1 ? item.T1.Add(_barSpan) : item.T2;
                bool inViewport = ItemLineIntersectsViewport(item, end)
                    || ItemLabelIntersectsViewport(item.LabelTime, item.LabelY);
                if (inViewport && count < sweepCap)
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
