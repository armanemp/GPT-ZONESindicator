using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        // Canvas is a layout panel rather than a low-level drawing context. The fast path
        // therefore uses one transparent SVG image for the whole visible zone layer instead
        // of creating three chart objects (rectangle, midline, label) for every visible zone.
        // The native renderer remains available as a safe fallback.
        private Canvas _zoneOverlayCanvas;
        private Image _zoneOverlayImage;
        private string _lastZoneOverlaySvg;
        private bool _canvasZoneOverlayFailed;
        private bool _canvasFallbackNoted;

        private readonly List<CanvasZoneVisual> _canvasZoneVisuals =
            new List<CanvasZoneVisual>(128);

        private struct CanvasZoneVisual
        {
            public Zone Zone;
            public double X1, X2, Y1, Y2, MidY, LabelY;
            public Color FillColor, MidlineColor, LabelColor;
            public string Label, Comment;
            public int BorderThickness;
            public bool DottedBorder;
        }

        private void SyncCanvasZone(Zone zone, bool refreshMetadata)
        {
            ZoneRenderState view = GetZoneRenderState(zone);

            if (!view.Want)
            {
                if (view.Shown || view.Rectangle != null || view.Midline != null || view.Label != null)
                    RemoveZoneObjects(zone);
                return;
            }

            // Handle a live mode change safely: never leave an old native copy under the SVG.
            if (view.Rectangle != null || view.Midline != null || view.Label != null)
                RemoveZoneObjects(zone);

            bool createView = !view.Shown;
            DateTime end = ZoneEndTime(zone);
            string comment = refreshMetadata || createView ? ZoneComment(zone) : view.AppliedComment;
            string labelText = !ShowZoneLabels ? null
                : (refreshMetadata || createView ? LabelText(zone) : view.AppliedLabelText);

            view.Shown = true;
            view.AppliedEndTime = end;
            view.AppliedLabelY = ComputeZoneLabelY(zone, end);
            view.AppliedColor = ZoneColor(zone);
            view.AppliedComment = comment;
            view.AppliedLabelText = labelText;
        }

        private bool UpdateCanvasZoneOverlay()
        {
            try
            {
                EnsureZoneOverlayControls();

                double width = Math.Max(1.0, Chart.Width);
                double height = Math.Max(1.0, Chart.Height);
                SetZoneOverlaySize(width, height);

                if (!HasUsableCanvasCoordinates())
                    throw new InvalidOperationException("Chart coordinate conversion is not ready for the Canvas overlay.");

                _canvasZoneVisuals.Clear();
                for (int i = 0; i < _zoneRenderingState.Tracked.Count; i++)
                {
                    Zone zone = _zoneRenderingState.Tracked[i];
                    ZoneRenderState view;
                    if (!_zoneRenderingState.States.TryGetValue(zone.Id, out view)
                        || !view.Want || !view.Shown)
                        continue;

                    double x1 = Chart.TimeToX(zone.StartTime);
                    double x2 = Chart.TimeToX(ZoneEndTime(zone));
                    double y1 = Chart.YValueToY(zone.Upper);
                    double y2 = Chart.YValueToY(zone.Lower);
                    double middleY = Chart.YValueToY((zone.Upper + zone.Lower) * 0.5);
                    double labelY = Chart.YValueToY(view.AppliedLabelY);

                    if (!IsFiniteCoordinate(x1) || !IsFiniteCoordinate(x2)
                        || !IsFiniteCoordinate(y1) || !IsFiniteCoordinate(y2)
                        || !IsFiniteCoordinate(middleY) || !IsFiniteCoordinate(labelY))
                        throw new InvalidOperationException("The chart returned a non-finite Canvas coordinate.");

                    if (x2 < x1)
                    {
                        double swap = x1;
                        x1 = x2;
                        x2 = swap;
                    }

                    double upperPixel = Math.Min(y1, y2);
                    double lowerPixel = Math.Max(y1, y2);
                    Color color = ZoneColor(zone);

                    _canvasZoneVisuals.Add(new CanvasZoneVisual
                    {
                        Zone = zone,
                        X1 = ClampPixel(x1, width),
                        X2 = ClampPixel(x2, width),
                        Y1 = ClampPixel(upperPixel, height),
                        Y2 = ClampPixel(lowerPixel, height),
                        MidY = ClampPixel(middleY, height),
                        // Labels belong at the projected right edge. Clamping the X coordinate
                        // keeps active-zone labels visible when their endpoint is beyond the view.
                        LabelY = labelY,
                        FillColor = color,
                        MidlineColor = ZoneMidlineColor(zone),
                        LabelColor = WithOpacity(color, 100),
                        Label = view.AppliedLabelText,
                        Comment = view.AppliedComment,
                        BorderThickness = zone.IsHigherTimeframe
                            ? Math.Max(1, ZoneBorderThickness)
                            : ZoneBorderThickness,
                        DottedBorder = zone.IsHigherTimeframe
                    });
                }

                string svg = BuildZoneOverlaySvg(width, height);
                if (!string.Equals(svg, _lastZoneOverlaySvg, StringComparison.Ordinal))
                {
                    _zoneOverlayImage.Source = new SvgIcon(svg);
                    _lastZoneOverlaySvg = svg;
                }

                return true;
            }
            catch (Exception error)
            {
                _canvasZoneOverlayFailed = true;
                LogError("Canvas zone overlay; reverting to native chart objects", error);
                DestroyZoneCanvasOverlay();
                return false;
            }
        }

        private bool HasUsableCanvasCoordinates()
        {
            int count = Bars.Count;
            if (count <= 0 || Chart.Width <= 0 || Chart.Height <= 0)
                return false;

            int first = Math.Max(0, Math.Min(Chart.FirstVisibleBarIndex, count - 1));
            int last = Math.Max(first, Math.Min(Chart.LastVisibleBarIndex, count - 1));
            double xFirst = Chart.TimeToX(Bars.OpenTimes[first]);
            double xLast = Chart.TimeToX(Bars.OpenTimes[last]);
            double yTop = Chart.YValueToY(Chart.TopY);
            double yBottom = Chart.YValueToY(Chart.BottomY);

            if (!IsFiniteCoordinate(xFirst) || !IsFiniteCoordinate(xLast)
                || !IsFiniteCoordinate(yTop) || !IsFiniteCoordinate(yBottom))
                return false;

            if (last > first && Math.Abs(xLast - xFirst) < 1.0)
                return false;

            return Math.Abs(yTop - yBottom) >= 1.0;
        }

        private void EnsureZoneOverlayControls()
        {
            if (_zoneOverlayCanvas != null && _zoneOverlayImage != null)
                return;

            double width = Math.Max(1.0, Chart.Width);
            double height = Math.Max(1.0, Chart.Height);

            _zoneOverlayCanvas = new Canvas
            {
                Left = 0,
                Top = 0,
                Width = width,
                Height = height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false
            };

            _zoneOverlayImage = new Image
            {
                Left = 0,
                Top = 0,
                Width = width,
                Height = height,
                Stretch = Stretch.Fill,
                IsHitTestVisible = false
            };

            _zoneOverlayCanvas.AddChild(_zoneOverlayImage);
            Chart.AddControl(_zoneOverlayCanvas);
            _lastZoneOverlaySvg = null;
        }

        private void SetZoneOverlaySize(double width, double height)
        {
            if (_zoneOverlayCanvas.Width != width) _zoneOverlayCanvas.Width = width;
            if (_zoneOverlayCanvas.Height != height) _zoneOverlayCanvas.Height = height;
            if (_zoneOverlayImage.Width != width) _zoneOverlayImage.Width = width;
            if (_zoneOverlayImage.Height != height) _zoneOverlayImage.Height = height;
        }

        private string BuildZoneOverlaySvg(double width, double height)
        {
            StringBuilder svg = new StringBuilder(Math.Max(512, _canvasZoneVisuals.Count * 300));
            svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"")
                .Append(Fmt(width)).Append("\" height=\"").Append(Fmt(height))
                .Append("\" viewBox=\"0 0 ").Append(Fmt(width)).Append(' ').Append(Fmt(height))
                .Append("\" overflow=\"hidden\">");

            // Pass 1: fills and borders. Drawing all fills first prevents a later overlapping
            // zone from painting over the centre line of an earlier zone.
            for (int i = 0; i < _canvasZoneVisuals.Count; i++)
            {
                CanvasZoneVisual v = _canvasZoneVisuals[i];
                double rectWidth = Math.Max(0, v.X2 - v.X1);
                double rectHeight = Math.Max(0, v.Y2 - v.Y1);
                if (rectWidth <= 0 || rectHeight <= 0)
                    continue;

                string color = Hex(v.FillColor);
                string dash = v.DottedBorder ? "1,3" : null;

                svg.Append("<g><title>").Append(EscapeSvg(v.Comment)).Append("</title><rect x=\"")
                    .Append(Fmt(v.X1)).Append("\" y=\"").Append(Fmt(v.Y1))
                    .Append("\" width=\"").Append(Fmt(rectWidth)).Append("\" height=\"")
                    .Append(Fmt(rectHeight)).Append("\" fill=\"").Append(color)
                    .Append("\" fill-opacity=\"").Append(Alpha(v.FillColor))
                    .Append("\" stroke=\"").Append(color).Append("\" stroke-opacity=\"")
                    .Append(Alpha(v.FillColor)).Append("\" stroke-width=\"")
                    .Append(v.BorderThickness.ToString(CultureInfo.InvariantCulture)).Append('"');

                if (!string.IsNullOrEmpty(dash))
                    svg.Append(" stroke-dasharray=\"").Append(dash).Append('"');

                svg.Append(" /></g>");
            }

            // Pass 2: centre lines are painted above every zone fill, matching the native
            // renderer's higher ZIndex and keeping them visible where zones overlap.
            if (ShowMidline)
            {
                string midlineDash = StrokeDash(MidlineStyle);
                for (int i = 0; i < _canvasZoneVisuals.Count; i++)
                {
                    CanvasZoneVisual v = _canvasZoneVisuals[i];
                    if (v.X2 <= v.X1)
                        continue;

                    svg.Append("<line x1=\"").Append(Fmt(v.X1)).Append("\" y1=\"")
                        .Append(Fmt(v.MidY)).Append("\" x2=\"").Append(Fmt(v.X2))
                        .Append("\" y2=\"").Append(Fmt(v.MidY)).Append("\" stroke=\"")
                        .Append(Hex(v.MidlineColor)).Append("\" stroke-opacity=\"")
                        .Append(Alpha(v.MidlineColor)).Append("\" stroke-width=\"")
                        .Append(Math.Max(1, MidlineThickness).ToString(CultureInfo.InvariantCulture))
                        .Append("\" stroke-linecap=\"butt\"");

                    if (!string.IsNullOrEmpty(midlineDash))
                        svg.Append(" stroke-dasharray=\"").Append(midlineDash).Append('"');

                    svg.Append(" />");
                }
            }

            // Pass 3: labels are last so no zone fill can obscure their text.
            if (ShowZoneLabels)
            {
                double labelMaxX = Math.Max(0, width - 2);
                int fontSize = Math.Max(11, LabelFontSize + 2);
                for (int i = 0; i < _canvasZoneVisuals.Count; i++)
                {
                    CanvasZoneVisual v = _canvasZoneVisuals[i];
                    if (string.IsNullOrEmpty(v.Label))
                        continue;

                    double labelX = ClampPixel(v.X2, labelMaxX);
                    svg.Append("<text x=\"").Append(Fmt(labelX)).Append("\" y=\"")
                        .Append(Fmt(v.LabelY)).Append("\" fill=\"").Append(Hex(v.LabelColor))
                        .Append("\" font-family=\"Segoe UI Light\" font-size=\"")
                        .Append(fontSize.ToString(CultureInfo.InvariantCulture))
                        .Append("\" font-weight=\"300\" dominant-baseline=\"central\">")
                        .Append(EscapeSvg(v.Label)).Append("</text>");
                }
            }

            svg.Append("</svg>");
            return svg.ToString();
        }

        private void DestroyZoneCanvasOverlay()
        {
            try
            {
                if (_zoneOverlayCanvas != null)
                    Chart.RemoveControl(_zoneOverlayCanvas);
            }
            catch (Exception)
            {
                // cTrader can be tearing down the chart at the same time as the indicator.
            }

            _zoneOverlayCanvas = null;
            _zoneOverlayImage = null;
            _lastZoneOverlaySvg = null;
            _canvasZoneVisuals.Clear();
        }

        private void NoteCanvasFallback(string message)
        {
            if (_canvasFallbackNoted)
                return;

            _canvasFallbackNoted = true;
            Log("Canvas zone overlay is using the native renderer: " + message);
        }

        private static bool IsFiniteCoordinate(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value)
                && value > -10000000.0 && value < 10000000.0;
        }

        private static double ClampPixel(double value, double extent)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;
            double maximum = Math.Max(0, extent);
            return Math.Max(0, Math.Min(maximum, value));
        }

        private static string Fmt(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Hex(Color color)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}",
                color.R, color.G, color.B);
        }

        private static string Alpha(Color color)
        {
            return (color.A / 255.0).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string EscapeSvg(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static string StrokeDash(LineStyle style)
        {
            string name = style.ToString();
            if (name.IndexOf("Dot", StringComparison.OrdinalIgnoreCase) >= 0)
                return "1,3";
            if (name.IndexOf("Dash", StringComparison.OrdinalIgnoreCase) >= 0)
                return "5,3";
            return null;
        }
    }
}
