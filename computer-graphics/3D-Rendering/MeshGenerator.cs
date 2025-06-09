using System.Numerics;

namespace _3D_Rendering
{
    public static class MeshGenerator
    {
        public static Mesh Generate(SceneObject obj)
        {
            return obj.Type.ToLower() switch
            {
                "sphere" => GenSphere(obj.Size[0], obj.MeshDensity),
                "cylinder" => GenCylinder(obj.Size[0], obj.Size[1], obj.MeshDensity),
                "cone" => GenCone(obj.Size[0], obj.Size[1], obj.MeshDensity),
                "cuboid" => GenCuboid(obj.Size[0], obj.Size[1], obj.Size[2]),
                _ => new Mesh()
            };
        }

        static Mesh GenCuboid(float x, float y, float z)
        {
            var mesh = new Mesh();

            float hx = x / 2, hy = y / 2, hz = z / 2;

            void AddFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
            {
                int start = mesh.Vertices.Count;
                mesh.Vertices.Add(v0);
                mesh.Vertices.Add(v1);
                mesh.Vertices.Add(v2);
                mesh.Vertices.Add(v3);

                // Two triangles with consistent winding (CCW when viewed from outside)
                mesh.Triangles.Add(new Triangle(start + 0, start + 1, start + 2));
                mesh.Triangles.Add(new Triangle(start + 0, start + 2, start + 3));
            }

            // Front face (z = +hz)
            AddFace(
                new Vector3(-hx, -hy, hz),
                new Vector3(hx, -hy, hz),
                new Vector3(hx, hy, hz),
                new Vector3(-hx, hy, hz)
            );

            // Back face (z = -hz) - reversed winding
            AddFace(
                new Vector3(hx, -hy, -hz),
                new Vector3(-hx, -hy, -hz),
                new Vector3(-hx, hy, -hz),
                new Vector3(hx, hy, -hz)
            );

            // Left face (x = -hx)
            AddFace(
                new Vector3(-hx, -hy, -hz),
                new Vector3(-hx, -hy, hz),
                new Vector3(-hx, hy, hz),
                new Vector3(-hx, hy, -hz)
            );

            // Right face (x = +hx)
            AddFace(
                new Vector3(hx, -hy, hz),
                new Vector3(hx, -hy, -hz),
                new Vector3(hx, hy, -hz),
                new Vector3(hx, hy, hz)
            );

            // Top face (y = +hy)
            AddFace(
                new Vector3(-hx, hy, hz),
                new Vector3(hx, hy, hz),
                new Vector3(hx, hy, -hz),
                new Vector3(-hx, hy, -hz)
            );

            // Bottom face (y = -hy) - reversed winding
            AddFace(
                new Vector3(-hx, -hy, -hz),
                new Vector3(hx, -hy, -hz),
                new Vector3(hx, -hy, hz),
                new Vector3(-hx, -hy, hz)
            );

            return mesh;
        }

        static Mesh GenSphere(float r, int steps)
        {
            var mesh = new Mesh();
            int n = steps; // Number of meridians (longitude divisions)
            int m = steps; // Number of parallels (latitude divisions)

            mesh.Vertices.Add(new Vector3(0, r, 0));

            for (int i = 1; i < m; i++)
            {
                float phi = MathF.PI * i / m;
                float y = r * MathF.Cos(phi);
                float ringRadius = r * MathF.Sin(phi);

                for (int j = 0; j < n; j++)
                {
                    float theta = 2 * MathF.PI * j / n;
                    mesh.Vertices.Add(new Vector3(
                        ringRadius * MathF.Cos(theta),
                        y,
                        ringRadius * MathF.Sin(theta)
                    ));
                }
            }

            int bottomPoleIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, -r, 0));

            for (int j = 0; j < n; j++)
            {
                int next = (j + 1) % n;
                mesh.Triangles.Add(new Triangle(0, 1 + j, 1 + next));
            }

            for (int i = 0; i < m - 2; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    int next = (j + 1) % n;

                    int currentRing = 1 + i * n;
                    int nextRing = 1 + (i + 1) * n;

                    mesh.Triangles.Add(new Triangle(
                        currentRing + j,
                        nextRing + j,
                        nextRing + next
                    ));
                    mesh.Triangles.Add(new Triangle(
                        currentRing + j,
                        nextRing + next,
                        currentRing + next
                    ));
                }
            }

            int lastRingStart = 1 + (m - 2) * n;
            for (int j = 0; j < n; j++)
            {
                int next = (j + 1) % n;
                mesh.Triangles.Add(new Triangle(bottomPoleIndex, lastRingStart + next, lastRingStart + j));
            }

            return mesh;
        }

        public static Mesh GenCone(float r, float h, int steps)
        {
            var mesh = new Mesh();
            int n = steps;

            int apexIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, h / 2, 0));

            int baseCenterIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, -h / 2, 0));

            int baseStart = mesh.Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                float theta = MathF.PI * 2 * i / n;
                mesh.Vertices.Add(new Vector3(r * MathF.Cos(theta), -h / 2, r * MathF.Sin(theta)));
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(baseCenterIndex, baseStart + i, baseStart + next));
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(apexIndex, baseStart + i, baseStart + next));
            }

            return mesh;
        }

        static Mesh GenCylinder(float r, float h, int steps)
        {
            var mesh = new Mesh();
            int n = steps;

            mesh.Vertices.Add(new Vector3(0, h / 2, 0));

            for (int i = 0; i < n; i++)
            {
                float theta = MathF.PI * 2 * i / n;
                mesh.Vertices.Add(new Vector3(r * MathF.Cos(theta), h / 2, r * MathF.Sin(theta)));
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(0, i + 1, next + 1)); // CCW winding
            }

            int bottomCenter = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, -h / 2, 0));

            int bottomRingStart = mesh.Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                float theta = MathF.PI * 2 * i / n;
                mesh.Vertices.Add(new Vector3(r * MathF.Cos(theta), -h / 2, r * MathF.Sin(theta)));
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(bottomCenter, bottomRingStart + next, bottomRingStart + i)); // CW winding
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;

                int topCurrent = i + 1;                    // Current vertex on top ring
                int topNext = next + 1;                    // Next vertex on top ring
                int bottomCurrent = bottomRingStart + i;   // Current vertex on bottom ring
                int bottomNext = bottomRingStart + next;   // Next vertex on bottom ring

                mesh.Triangles.Add(new Triangle(topCurrent, bottomCurrent, bottomNext));
                mesh.Triangles.Add(new Triangle(topCurrent, bottomNext, topNext));
            }

            return mesh;
        }
    }
}