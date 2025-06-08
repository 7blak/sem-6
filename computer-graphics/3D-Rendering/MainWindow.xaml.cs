using Newtonsoft.Json;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace _3D_Rendering
{
    public partial class MainWindow : Window
    {
        private List<Mesh> sceneMeshes = [];
        private Matrix4x4 proj;
        private Matrix4x4 view;
        private readonly DrawingVisual visual = new DrawingVisual();

        public MainWindow()
        {
            InitializeComponent();

            ContentRendered += (s, e) =>
            {
                RenderCanvas.Children.Add(new VisualHost(visual));
                LoadScene("scene.json");
                InitMatrices();

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(30);
                timer.Tick += (s2, e2) => Draw();
                timer.Start();
            };

            SliderDistance.ValueChanged += (s, e) => UpdateView();
            SliderRotX.ValueChanged += (s, e) => UpdateView();
            SliderRotY.ValueChanged += (s, e) => UpdateView();
        }

        void InitMatrices()
        {
            float fov = MathF.PI / 4;
            float aspect = (float)RenderCanvas.ActualWidth / (float)RenderCanvas.ActualHeight;
            float near = 0.1f, far = 100f;
            proj = CreatePerspectiveFieldOfView(fov, aspect, near, far);
            UpdateView();
        }

        void UpdateView()
        {
            float dist = (float)SliderDistance.Value;
            float rotX = (float)(SliderRotX.Value * Math.PI / 180);
            float rotY = (float)(SliderRotY.Value * Math.PI / 180);
            Matrix4x4 t = Matrix4x4.CreateFromYawPitchRoll(rotY, rotX, 0) * Matrix4x4.CreateTranslation(0, 0, dist);
            Matrix4x4.Invert(t, out view);
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
                // Clear background
                double w = RenderCanvas.ActualWidth;
                double h = RenderCanvas.ActualHeight;
                if (w < 1 || h < 1) return; // Skip drawing

                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, h));


                foreach (var mesh in sceneMeshes)
                {
                    var mt = mesh.Model;
                    var mvp = mt * view * proj;
                    foreach (var tri in mesh.Triangles)
                    {
                        var v0 = Project(mesh.Vertices[tri.A], mvp);
                        var v1 = Project(mesh.Vertices[tri.B], mvp);
                        var v2 = Project(mesh.Vertices[tri.C], mvp);
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
            if (p.W != 0)
            {
                float x = (p.X / p.W + 1) * 0.5f * (float)RenderCanvas.ActualWidth;
                float y = (1 - p.Y / p.W) * 0.5f * (float)RenderCanvas.ActualHeight;
                return new Point(x, y);
            }
            return new Point(-1000, -1000);
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