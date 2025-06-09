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

            // Center the cuboid around origin
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

            // Top pole
            mesh.Vertices.Add(new Vector3(0, r, 0)); // index 0

            // Generate vertices for each parallel (latitude ring)
            for (int i = 1; i < m; i++) // Skip poles (i=0 and i=m)
            {
                float phi = MathF.PI * i / m; // Latitude angle from 0 to PI
                float y = r * MathF.Cos(phi);
                float ringRadius = r * MathF.Sin(phi);

                for (int j = 0; j < n; j++) // Longitude
                {
                    float theta = 2 * MathF.PI * j / n;
                    mesh.Vertices.Add(new Vector3(
                        ringRadius * MathF.Cos(theta),
                        y,
                        ringRadius * MathF.Sin(theta)
                    ));
                }
            }

            // Bottom pole
            int bottomPoleIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, -r, 0));

            // Top cap triangles (connect top pole to first ring)
            for (int j = 0; j < n; j++)
            {
                int next = (j + 1) % n;
                mesh.Triangles.Add(new Triangle(0, 1 + j, 1 + next));
            }

            // Middle bands (connect each ring to the next)
            for (int i = 0; i < m - 2; i++) // m-2 bands between m-1 rings
            {
                for (int j = 0; j < n; j++)
                {
                    int next = (j + 1) % n;

                    int currentRing = 1 + i * n;
                    int nextRing = 1 + (i + 1) * n;

                    // Two triangles per quad
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

            // Bottom cap triangles (connect last ring to bottom pole)
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

            // Apex and base center
            int apexIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, h / 2, 0)); // Center the cone

            int baseCenterIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, -h / 2, 0)); // Center the cone

            // Base rim vertices
            int baseStart = mesh.Vertices.Count;
            for (int i = 0; i < n; i++) // Use < instead of <=
            {
                float theta = MathF.PI * 2 * i / n;
                mesh.Vertices.Add(new Vector3(r * MathF.Cos(theta), -h / 2, r * MathF.Sin(theta)));
            }

            // Base cap triangles (clockwise winding for downward-facing)
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(baseCenterIndex, baseStart + i, baseStart + next));
            }

            // Side triangles (apex to each rim edge)
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

            // === TOP CAP ===
            // Center vertex for top cap
            mesh.Vertices.Add(new Vector3(0, h / 2, 0)); // index 0

            // Top ring vertices
            for (int i = 0; i < n; i++)
            {
                float theta = MathF.PI * 2 * i / n;
                mesh.Vertices.Add(new Vector3(r * MathF.Cos(theta), h / 2, r * MathF.Sin(theta)));
            }
            // Top ring indices: 1 to n

            // Top cap triangles (center to ring)
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(0, i + 1, next + 1)); // CCW winding
            }

            // === BOTTOM CAP ===
            // Center vertex for bottom cap
            int bottomCenter = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(0, -h / 2, 0)); // index n+1

            // Bottom ring vertices
            int bottomRingStart = mesh.Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                float theta = MathF.PI * 2 * i / n;
                mesh.Vertices.Add(new Vector3(r * MathF.Cos(theta), -h / 2, r * MathF.Sin(theta)));
            }
            // Bottom ring indices: (n+2) to (2n+1)

            // Bottom cap triangles (center to ring) - reversed winding
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                mesh.Triangles.Add(new Triangle(bottomCenter, bottomRingStart + next, bottomRingStart + i)); // CW winding
            }

            // === SIDE SURFACE ===
            // Connect top ring to bottom ring
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;

                int topCurrent = i + 1;                    // Current vertex on top ring
                int topNext = next + 1;                    // Next vertex on top ring
                int bottomCurrent = bottomRingStart + i;   // Current vertex on bottom ring
                int bottomNext = bottomRingStart + next;   // Next vertex on bottom ring

                // Two triangles per side quad
                mesh.Triangles.Add(new Triangle(topCurrent, bottomCurrent, bottomNext));
                mesh.Triangles.Add(new Triangle(topCurrent, bottomNext, topNext));
            }

            return mesh;
        }
    }
}