using System;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderStructure()
        {
            RenderMajorStructure();
            RenderInternalStructure();
        }

        private void RenderMajorStructure()
        {
            SyncEngine(_marketStructureState.MajorEngine, ShowMajorBos, ShowMajorChoch, ShowMajorSwings, ShowMajorActiveLevels);
        }

        private void RenderInternalStructure()
        {
            SyncEngine(_marketStructureState.InternalEngine, ShowInternalBos, ShowInternalChoch, ShowInternalSwings, ShowInternalActiveLevels);
        }

        private void SyncEngine(Engine engine, bool showBos, bool showChoch, bool showMarks, bool showActive)
        {
            // Process every feature even when all its visibility switches are off, so
            // objects created by a prior settings state are removed rather than left behind.
            int configuredCap = engine.Major ? StructMaxVisible : InternalStructMaxVisible;
            int cap = configuredCap > 0 ? configuredCap : int.MaxValue;
            int levelCount = 0;
            int markCount = 0;

            for (int k = engine.Swings.Count - 1; k >= 0; k--)
            {
                Swing swing = engine.Swings[k];
                StructureSwingRenderState renderState;
                _marketStructureRenderingState.BySwing.TryGetValue(swing, out renderState);

                bool wantLevel = swing.Broken
                    ? (!swing.SilentBreak && (swing.Choch ? showChoch : showBos))
                    : showActive;

                if (wantLevel)
                {
                    renderState = GetStructureSwingRenderState(swing);
                    EnsureLevelItem(swing, engine.Major, renderState);
                    Item level = renderState.Level;
                    DateTime end = level.T2 <= level.T1 ? level.T1.Add(_barSpan) : level.T2;
                    bool inViewport = ItemLineIntersectsViewport(level, end)
                        || ItemLabelIntersectsViewport(level.LabelTime, level.LabelY);

                    // Off-screen history does not consume the visible-item budget.
                    if (inViewport && levelCount < cap)
                    {
                        levelCount++;
                        SyncItem(level, true);
                    }
                    else
                    {
                        SyncItem(level, false);
                    }
                }
                else if (renderState != null && renderState.Level != null)
                {
                    SyncItem(renderState.Level, false);
                }

                if (showMarks)
                {
                    renderState = GetStructureSwingRenderState(swing);
                    EnsureMarkItem(swing, renderState);
                    Item mark = renderState.Mark;
                    bool inViewport = ItemLabelIntersectsViewport(mark.LabelTime, mark.LabelY);
                    if (inViewport && markCount < cap)
                    {
                        markCount++;
                        SyncItem(mark, true);
                    }
                    else
                    {
                        SyncItem(mark, false);
                    }
                }
                else if (renderState != null && renderState.Mark != null)
                {
                    SyncItem(renderState.Mark, false);
                }
            }
        }

        private StructureSwingRenderState GetStructureSwingRenderState(Swing swing)
        {
            StructureSwingRenderState state;
            if (_marketStructureRenderingState.BySwing.TryGetValue(swing, out state))
                return state;

            state = new StructureSwingRenderState();
            _marketStructureRenderingState.BySwing.Add(swing, state);
            return state;
        }

        private void EnsureLevelItem(Swing swing, bool major, StructureSwingRenderState renderState)
        {
            if (renderState.Level == null)
                renderState.Level = new Item { Name = NextName("S") };

            Item item = renderState.Level;
            item.Y = swing.Price;
            item.T1 = swing.Time;
            item.Thick = StructThickness;
            item.Style = major ? StructStyle : InternalStyle;

            if (swing.Broken)
            {
                item.T2 = swing.BreakTime;
                Color baseColor = swing.Choch
                    ? _cChoch
                    : (swing.BreakBull ? _cStructBull : _cStructBear);

                item.Col = WithOpacity(baseColor, StructOpacity);
                item.Label = swing.Choch ? "CHoCH" : "BOS";
                item.LabelTime = MidTime(item.T1, item.T2);
                item.LabelY = swing.Price;
                item.LabelH = HorizontalAlignment.Center;
                item.LabelV = swing.BreakBull ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            }
            else
            {
                item.T2 = _futureTime;
                item.Col = WithOpacity(swing.High ? _cSwingHigh : _cSwingLow, StructOpacity);
                item.Label = (major ? "Major " : "Internal ")
                    + (swing.High ? "Swing High" : "Swing Low");
                // Spread active swing tags along their own level, instead of stacking all
                // of them on the currently forming candle.
                item.LabelTime = MidTime(item.T1, _lastBarTime);
                item.LabelY = swing.Price;
                item.LabelH = HorizontalAlignment.Left;
                item.LabelV = swing.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            }
        }

        private void EnsureMarkItem(Swing swing, StructureSwingRenderState renderState)
        {
            if (renderState.Mark == null)
            {
                renderState.Mark = new Item
                {
                    Name = NextName("S"),
                    TextOnly = true,
                    LabelH = HorizontalAlignment.Center
                };
            }

            Item item = renderState.Mark;
            item.Y = swing.Price;
            item.T1 = swing.Time;
            item.T2 = swing.Time;
            item.Label = swing.High ? "v" : "^";
            item.LabelTime = swing.Time;
            item.LabelY = swing.Price;
            item.LabelV = swing.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            item.Col = WithOpacity(swing.High ? _cSwingHigh : _cSwingLow, 95);
        }
    }
}
