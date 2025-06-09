using Newtonsoft.Json;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _3D_Rendering
{
    public partial class MainWindow : Window
    {
        private List<Mesh> sceneMeshes = [];
        private Matrix4x4 proj;
        private Matrix4x4 view;
        private readonly DrawingVisual visual = new DrawingVisual();

        // Camera state
        private Vector3 cameraPosition = new Vector3(0, 0, 0);
        private Vector3 cameraTarget = new Vector3(0, 0, 0);
        private Vector3 cameraUp = new Vector3(0, 1, 0);

        // Camera rotation for FPS-style controls
        private float cameraYaw = 0f;   // Y-axis rotation (left/right)
        private float cameraPitch = 0f; // X-axis rotation (up/down)

        // Movement speed
        private float moveSpeed = 0.1f;
        private float rotateSpeed = 2f; // degrees per frame

        // Key states
        private HashSet<Key> pressedKeys = new HashSet<Key>();

        public MainWindow()
        {
            InitializeComponent();

            ContentRendered += (s, e) =>
            {
                RenderCanvas.Children.Add(new VisualHost(visual));
                LoadScene("scene.json");
                InitMatrices();

                // Focus the window to receive key events
                Focus();
                Focusable = true;

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
                timer.Tick += (s2, e2) =>
                {
                    UpdateCamera();
                    Draw();
                };
                timer.Start();
            };

            // Key event handlers
            KeyDown += (s, e) =>
            {
                pressedKeys.Add(e.Key);
                e.Handled = true;
            };

            KeyUp += (s, e) =>
            {
                pressedKeys.Remove(e.Key);
                e.Handled = true;
            };
        }

        void InitMatrices()
        {
            float fov = MathF.PI / 4; // 45 degrees
            float sx = (float)RenderCanvas.ActualWidth;
            float sy = (float)RenderCanvas.ActualHeight;
            proj = Matrix4x4.Transpose(CreatePerspectiveProjection(fov, sx, sy));
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

            // Speed adjustment
            if (pressedKeys.Contains(Key.LeftShift))
                moveSpeed = 0.2f; // Fast
            else if (pressedKeys.Contains(Key.LeftCtrl))
                moveSpeed = 0.05f; // Slow
            else
                moveSpeed = 0.1f; // Normal

            // Movement with WASD + QE for up/down
            if (pressedKeys.Contains(Key.S))
                cameraPosition += forward * moveSpeed;
            if (pressedKeys.Contains(Key.W))
                cameraPosition -= forward * moveSpeed;
            if (pressedKeys.Contains(Key.D))
                cameraPosition -= right * moveSpeed;
            if (pressedKeys.Contains(Key.A))
                cameraPosition += right * moveSpeed;
            if (pressedKeys.Contains(Key.E))
                cameraPosition -= worldUp * moveSpeed;
            if (pressedKeys.Contains(Key.Q))
                cameraPosition += worldUp * moveSpeed;

            // Always update camera target based on current position and orientation
            // This ensures the camera looks in the right direction after movement
            cameraTarget = cameraPosition + forward;

            UpdateView();
        }

        void UpdateView()
        {
            view = Matrix4x4.Transpose(CreateCameraMatrix(cameraPosition, cameraTarget, cameraUp));
        }

        // Camera matrix construction
        Matrix4x4 CreateCameraMatrix(Vector3 cPos, Vector3 cTarget, Vector3 cUp)
        {
            // Calculate camera coordinate system axes
            Vector3 cZ = Vector3.Normalize(cPos - cTarget);
            Vector3 cX = Vector3.Normalize(Vector3.Cross(cUp, cZ));
            Vector3 cY = Vector3.Normalize(Vector3.Cross(cZ, cX));

            // Construct the camera matrix using the formula from section 3.1.2
            return new Matrix4x4(
                cX.X, cX.Y, cX.Z, -Vector3.Dot(cX, cPos),
                cY.X, cY.Y, cY.Z, -Vector3.Dot(cY, cPos),
                cZ.X, cZ.Y, cZ.Z, -Vector3.Dot(cZ, cPos),
                0, 0, 0, 1
            );
        }

        // Perspective projection matrix following the documentation (section 3.3.1)
        Matrix4x4 CreatePerspectiveProjection(float theta, float sx, float sy)
        {
            float cotHalfTheta = 1.0f / MathF.Tan(theta / 2.0f);

            // Following the formula from section 3.3.1
            return new Matrix4x4(
                -(sx / 2.0f) * cotHalfTheta, 0, sx / 2.0f, 0,
                0, (sx / 2.0f) * cotHalfTheta, sy / 2.0f, 0,
                0, 0, 0, 1,
                0, 0, 1, 0
            );
        }

        void LoadScene(string path)
        {
            var json = File.ReadAllText(path);
            var objs = JsonConvert.DeserializeObject<List<SceneObject>>(json);
            if (objs == null)
                throw new InvalidDataException("Failed to load scene objects from JSON.");
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
                // Clear background
                double w = RenderCanvas.ActualWidth;
                double h = RenderCanvas.ActualHeight;
                if (w < 1 || h < 1) return;

                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, h));

                // Draw camera info
                var typeface = new Typeface("Consolas");
                var text = new FormattedText(
                    $"Pos: ({cameraPosition.X:F1}, {cameraPosition.Y:F1}, {cameraPosition.Z:F1})\n" +
                    $"Target: ({cameraTarget.X:F1}, {cameraTarget.Y:F1}, {cameraTarget.Z:F1})\n" +
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
            // Transform vertex to clip space
            var p = Vector4.Transform(new Vector4(v, 1), mvp);

            // Check if vertex is behind camera (w <= 0)
            if (p.W <= 0.001f)
                return new Point(-10000, -10000);

            // Perspective divide
            float x_normalized = p.X / p.W;
            float y_normalized = p.Y / p.W;

            // Clip to reasonable bounds
            if (Math.Abs(x_normalized) > 10000 || Math.Abs(y_normalized) > 10000)
                return new Point(-10000, -10000);

            return new Point(x_normalized, y_normalized);
        }

        void DrawLine(DrawingContext dc, Point a, Point b)
        {
            dc.DrawLine(new Pen(Brushes.White, 1), a, b);
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