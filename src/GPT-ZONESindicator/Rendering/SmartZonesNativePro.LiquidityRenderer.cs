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

                if (!eligible)
                {
                    if (cluster.Vis != null)
                        SyncItem(cluster.Vis, false);
                    continue;
                }

                if (cluster.Vis == null)
                    cluster.Vis = new Item { Name = NextName("E") };

                Item item = cluster.Vis;
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
                if (!ShowSweeps)
                {
                    if (sweep.Vis != null)
                        SyncItem(sweep.Vis, false);
                    continue;
                }

                if (sweep.Vis == null)
                    sweep.Vis = new Item { Name = NextName("W") };

                Item item = sweep.Vis;
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
