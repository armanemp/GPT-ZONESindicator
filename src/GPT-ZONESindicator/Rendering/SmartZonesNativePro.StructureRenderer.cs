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
            int configuredCap = engine.Major ? StructMaxVisible : InternalStructMaxVisible;
            int cap = configuredCap > 0 ? configuredCap : int.MaxValue;
            int levelCount = 0;
            int markCount = 0;

            for (int k = engine.Swings.Count - 1; k >= 0; k--)
            {
                Swing swing = engine.Swings[k];
                StructureSwingRenderState renderState;
                _marketStructureRenderingState.BySwing.TryGetValue(swing, out renderState);

                bool wantLevel = StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    swing.Broken,
                    swing.Swept,
                    swing.SilentBreak,
                    swing.Choch,
                    showBos,
                    showChoch,
                    showActive);
                DateTime lineEnd = swing.Broken ? swing.BreakTime : _futureTime;
                bool inViewport = wantLevel
                    && TimePriceIntersectsViewport(swing.Time, lineEnd, swing.Price);

                if (inViewport && levelCount < cap)
                {
                    renderState = GetStructureSwingRenderState(swing);
                    EnsureLevelItem(swing, engine.Major, renderState);
                    SyncItem(renderState.Level, true);
                    levelCount++;
                }
                else if (renderState != null && renderState.Level != null)
                {
                    SyncItem(renderState.Level, false);
                }

                bool markInViewport = showMarks && ItemLabelIntersectsViewport(swing.Time, swing.Price);
                if (markInViewport && markCount < cap)
                {
                    renderState = GetStructureSwingRenderState(swing);
                    EnsureMarkItem(swing, renderState);
                    SyncItem(renderState.Mark, true);
                    markCount++;
                }
                else if (renderState != null && renderState.Mark != null)
                {
                    SyncItem(renderState.Mark, false);
                }

                if (renderState != null
                    && (renderState.Level == null || !renderState.Level.Shown)
                    && (renderState.Mark == null || !renderState.Mark.Shown))
                    _marketStructureRenderingState.BySwing.Remove(swing);
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
                item.LabelTime = swing.BreakTime; // Anchor BOS/CHoCH text to the candle that broke the level.
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
