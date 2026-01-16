using Microsoft.Maui.Graphics;
using TGL;

namespace RubikCube.Maui.Controls
{
    /// <summary>
    /// Cross-platform 3D cube view using MAUI Graphics (SkiaSharp-backed).
    /// Renders the Rubik's Cube using 2D projection of 3D/4D geometry.
    /// </summary>
    public class CubeView : GraphicsView, IDrawable
    {
        public TShape Root { get; set; } = new TShape();
        public bool IsTransparencyOn { get; set; }
        public new Color BackgroundColor { get; set; } = Colors.DarkSlateGray;

        public CubeView()
        {
            Drawable = this;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = BackgroundColor;
            canvas.FillRectangle(dirtyRect);

            var centerX = dirtyRect.Width / 2;
            var centerY = dirtyRect.Height / 2;
            var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.4f;

            var transform = new TAffine();
            DrawObject(canvas, Root, transform, centerX, centerY, scale);
        }

        private void DrawObject(ICanvas canvas, TShape obj, TAffine parentTransform, float centerX, float centerY, float scale)
        {
            var transform = parentTransform * obj.Transform;

            // Draw children first (back to front ordering)
            foreach (var child in obj.Children)
            {
                DrawObject(canvas, child, transform, centerX, centerY, scale);
            }

            // Draw this object's faces
            if (obj.Faces.Count > 0 && obj.Vertices.Count > 0)
            {
                DrawShape(canvas, obj, transform, centerX, centerY, scale);
            }
        }

        private void DrawShape(ICanvas canvas, TShape obj, TAffine transform, float centerX, float centerY, float scale)
        {
            // Sort faces by Z for proper depth ordering (painter's algorithm)
            var facesToDraw = new List<(int faceIndex, float avgZ, Color color)>();

            for (int i = 0; i < obj.Faces.Count; i += 4)
            {
                if (i + 3 >= obj.Faces.Count) break;

                var colorIndex = i / 4;
                Color color = colorIndex < obj.Colors.Count ? obj.Colors[colorIndex] : Colors.White;

                // Transform vertices
                var v0 = transform * obj.Vertices[obj.Faces[i]];
                var v1 = transform * obj.Vertices[obj.Faces[i + 1]];
                var v2 = transform * obj.Vertices[obj.Faces[i + 2]];
                var v3 = transform * obj.Vertices[obj.Faces[i + 3]];

                // Calculate average Z for sorting
                float avgZ = (v0.Z + v1.Z + v2.Z + v3.Z) / 4;

                facesToDraw.Add((i, avgZ, color));
            }

            // Sort by Z (back to front)
            facesToDraw.Sort((a, b) => a.avgZ.CompareTo(b.avgZ));

            // Draw faces
            foreach (var (faceIndex, _, color) in facesToDraw)
            {
                var v0 = transform * obj.Vertices[obj.Faces[faceIndex]];
                var v1 = transform * obj.Vertices[obj.Faces[faceIndex + 1]];
                var v2 = transform * obj.Vertices[obj.Faces[faceIndex + 2]];
                var v3 = transform * obj.Vertices[obj.Faces[faceIndex + 3]];

                // Project to 2D (simple orthographic projection)
                var p0 = new PointF(centerX + v0.X * scale, centerY - v0.Y * scale);
                var p1 = new PointF(centerX + v1.X * scale, centerY - v1.Y * scale);
                var p2 = new PointF(centerX + v2.X * scale, centerY - v2.Y * scale);
                var p3 = new PointF(centerX + v3.X * scale, centerY - v3.Y * scale);

                var path = new PathF();
                path.MoveTo(p0);
                path.LineTo(p1);
                path.LineTo(p2);
                path.LineTo(p3);
                path.Close();

                // Apply transparency
                float alpha = IsTransparencyOn ? obj.Transparency : 1.0f;
                canvas.FillColor = color.WithAlpha(alpha);
                canvas.FillPath(path);

                // Draw outline
                canvas.StrokeColor = Colors.Black;
                canvas.StrokeSize = 1;
                canvas.DrawPath(path);
            }
        }
    }
}
