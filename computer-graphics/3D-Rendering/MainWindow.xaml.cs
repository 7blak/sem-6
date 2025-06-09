using Newtonsoft.Json;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace _3D_Rendering
{
    public partial class MainWindow : Window
    {
        private readonly List<Mesh> sceneMeshes = [];
        private Matrix4x4 proj;
        private Matrix4x4 view;
        private readonly DrawingVisual visual = new DrawingVisual();

        private Vector3 cameraPosition = new Vector3(0, 0, 0);
        private float cameraYaw = 0f;
        private float cameraPitch = 0f;

        private float moveSpeed = 0.1f;
        private float rotateSpeed = 2f;

        private HashSet<Key> pressedKeys = new HashSet<Key>();

        public MainWindow()
        {
            InitializeComponent();

            ContentRendered += (s, e) =>
            {
                RenderCanvas.Children.Add(new VisualHost(visual));
                LoadScene("scene.json");
                InitMatrices();

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(16);
                timer.Tick += (s2, e2) =>
                {
                    UpdateCamera();
                    Draw();
                };
                timer.Start();
            };

            this.KeyDown += (s, e) =>
            {
                pressedKeys.Add(e.Key);
                e.Handled = true;
            };

            this.KeyUp += (s, e) =>
            {
                pressedKeys.Remove(e.Key);
                e.Handled = true;
            };
        }

        void InitMatrices()
        {
            float fov = MathF.PI / 2;
            float aspect = (float)RenderCanvas.ActualWidth / (float)RenderCanvas.ActualHeight;
            float near = 0.1f, far = 100f;
            proj = CreatePerspectiveFieldOfView(fov, aspect, near, far);
            UpdateView();
        }

        void UpdateCamera()
        {
            // Rotation with arrow keys
            if (pressedKeys.Contains(Key.Right))
                cameraYaw -= rotateSpeed;
            if (pressedKeys.Contains(Key.Left))
                cameraYaw += rotateSpeed;
            if (pressedKeys.Contains(Key.Up))
                cameraPitch -= rotateSpeed;
            if (pressedKeys.Contains(Key.Down))
                cameraPitch += rotateSpeed;

            // Clamp pitch to prevent flipping
            cameraPitch = Math.Clamp(cameraPitch, -89f, 89f);

            // Convert angles to radians
            float yawRad = cameraYaw * MathF.PI / 180f;
            float pitchRad = cameraPitch * MathF.PI / 180f;

            // Calculate forward, right, and up vectors from rotation
            Vector3 forward = new Vector3(
                MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                -MathF.Sin(pitchRad),
                MathF.Cos(yawRad) * MathF.Cos(pitchRad)
            );

            Vector3 worldUp = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, worldUp));
            Vector3 up = Vector3.Cross(right, forward);

            // Movement with WASD + QE for up/down
            if (pressedKeys.Contains(Key.W))
                cameraPosition += forward * moveSpeed;
            if (pressedKeys.Contains(Key.S))
                cameraPosition -= forward * moveSpeed;
            if (pressedKeys.Contains(Key.A))
                cameraPosition -= right * moveSpeed;
            if (pressedKeys.Contains(Key.D))
                cameraPosition += right * moveSpeed;
            if (pressedKeys.Contains(Key.Q))
                cameraPosition -= worldUp * moveSpeed;
            if (pressedKeys.Contains(Key.E))
                cameraPosition += worldUp * moveSpeed;

            // Speed adjustment
            if (pressedKeys.Contains(Key.LeftShift))
                moveSpeed = 0.2f; // Fast
            else if (pressedKeys.Contains(Key.LeftCtrl))
                moveSpeed = 0.05f; // Slow
            else
                moveSpeed = 0.1f; // Normal

            UpdateView();
        }

        void UpdateView()
        {
            // Convert angles to radians
            float yawRad = cameraYaw * MathF.PI / 180f;
            float pitchRad = cameraPitch * MathF.PI / 180f;

            // Calculate camera's forward direction
            Vector3 forward = new Vector3(
                MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                -MathF.Sin(pitchRad),
                MathF.Cos(yawRad) * MathF.Cos(pitchRad)
            );

            Vector3 target = cameraPosition + forward;
            Vector3 up = Vector3.UnitY;

            // Create view matrix using LookAt
            view = CreateLookAt(cameraPosition, target, up);
        }

        // Helper method to create LookAt matrix
        Matrix4x4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 f = Vector3.Normalize(target - eye);
            Vector3 s = Vector3.Normalize(Vector3.Cross(f, up));
            Vector3 u = Vector3.Cross(s, f);

            return new Matrix4x4(
                s.X, u.X, -f.X, 0,
                s.Y, u.Y, -f.Y, 0,
                s.Z, u.Z, -f.Z, 0,
                -Vector3.Dot(s, eye), -Vector3.Dot(u, eye), Vector3.Dot(f, eye), 1
            );
        }

        void LoadScene(string path)
        {
            var json = File.ReadAllText(path);
            var objs = JsonConvert.DeserializeObject<List<SceneObject>>(json);
            foreach (var obj in objs)
            {
                var m = MeshGenerator.Generate(obj);
                m.Model = obj.Transform;
                sceneMeshes.Add(m);
            }
        }

        void Draw()
        {
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, RenderCanvas.ActualWidth, RenderCanvas.ActualHeight));

                var typeface = new Typeface("Consolas");
                var text = new FormattedText(
                    $"Pos: ({cameraPosition.X:F1}, {cameraPosition.Y:F1}, {cameraPosition.Z:F1})\n" +
                    $"Rot: ({cameraPitch:F0}°, {cameraYaw:F0}°)\n" +
                    $"WASD: Move, Arrows: Look, QE: Up/Down\n" +
                    $"Shift: Fast, Ctrl: Slow",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    12,
                    Brushes.White,
                    1.0);

                dc.DrawText(text, new Point(10, 10));

                foreach (var mesh in sceneMeshes)
                {
                    var mt = mesh.Model;
                    var mvp = mt * view * proj;
                    foreach (var tri in mesh.Triangles)
                    {
                        var v0 = Project(mesh.Vertices[tri.A], mvp);
                        var v1 = Project(mesh.Vertices[tri.B], mvp);
                        var v2 = Project(mesh.Vertices[tri.C], mvp);

                        // Skip triangles with invalid projections
                        if (v0.X < -5000 || v1.X < -5000 || v2.X < -5000)
                            continue;

                        DrawLine(dc, v0, v1);
                        DrawLine(dc, v1, v2);
                        DrawLine(dc, v2, v0);
                    }
                }
            }
        }

        Point Project(Vector3 v, Matrix4x4 mvp)
        {
            var p = Vector4.Transform(new Vector4(v, 1), mvp);

            // Check if vertex is behind camera or W is too small
            if (p.W <= 0.001f)
                return new Point(-10000, -10000); // Far offscreen

            float x = (p.X / p.W + 1) * 0.5f * (float)RenderCanvas.ActualWidth;
            float y = (1 - p.Y / p.W) * 0.5f * (float)RenderCanvas.ActualHeight;

            // Optional: clip to screen bounds
            if (x < -1000 || x > RenderCanvas.ActualWidth + 1000 ||
                y < -1000 || y > RenderCanvas.ActualHeight + 1000)
                return new Point(-10000, -10000);

            return new Point(x, y);
        }

        void DrawLine(DrawingContext dc, Point a, Point b)
        {
            dc.DrawLine(new Pen(Brushes.White, 1), a, b);
        }

        Matrix4x4 CreatePerspectiveFieldOfView(float fov, float aspect, float zn, float zf)
        {
            // Hardcoded projection init values
            float yScale = 1 / MathF.Tan(fov / 2);
            float xScale = yScale / aspect;
            return new Matrix4x4(
                xScale, 0, 0, 0,
                0, yScale, 0, 0,
                0, 0, zf / (zn - zf), -1,
                0, 0, zn * zf / (zn - zf), 0);
        }
    }

    public class VisualHost : FrameworkElement
    {
        private readonly Visual _visual;

        public VisualHost(Visual visual)
        {
            _visual = visual;
            AddVisualChild(_visual);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _visual;
    }
}