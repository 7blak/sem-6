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

        static Mesh GenSphere(float r, int steps)
        {
            var m = new Mesh();
            for (int i = 0; i <= steps; i++)
            {
                float phi = MathF.PI * i / steps;
                for (int j = 0; j <= steps * 2; j++)
                {
                    float theta = 2 * MathF.PI * j / (steps * 2);
                    m.Vertices.Add(new Vector3(
                        r * MathF.Sin(phi) * MathF.Cos(theta),
                        r * MathF.Cos(phi),
                        r * MathF.Sin(phi) * MathF.Sin(theta)
                    ));
                }
            }
            int w = steps * 2 + 1;
            for (int i = 0; i < steps; i++) for (int j = 0; j < w - 1; j++)
                {
                    int a = i * w + j, b = a + 1, c = a + w, d = b + w;
                    m.Triangles.Add(new Triangle { A = a, B = b, C = c });
                    m.Triangles.Add(new Triangle { A = b, B = d, C = c });
                }
            return m;
        }

        //static Mesh GenCylinder(float r, float h, int steps)
        //{
        //    var m = new Mesh();
        //    // top & bottom circles + side
        //    for (int sign = -1; sign <= 1; sign += 2)
        //    {
        //        float y = sign * h / 2;
        //        int centerIdx = m.Vertices.Count;
        //        m.Vertices.Add(new Vector3(0, y, 0));
        //        for (int i = 0; i < steps; i++)
        //        {
        //            float theta = 2 * MathF.PI * i / steps;
        //            m.Vertices.Add(new Vector3(r * MathF.Cos(theta), y, r * MathF.Sin(theta)));
        //        }
        //        for (int i = 0; i < steps; i++)
        //        {
        //            m.Triangles.Add(new Triangle { A = centerIdx, B = centerIdx + 1 + (i + 1) % steps, C = centerIdx + 1 + i });
        //        }
        //    }
        //    int baseSideIdx = m.Vertices.Count;
        //    for (int i = 0; i < steps; i++)
        //    {
        //        float theta = 2 * MathF.PI * i / steps;
        //        m.Vertices.Add(new Vector3(r * MathF.Cos(theta), -h / 2, r * MathF.Sin(theta)));
        //        m.Vertices.Add(new Vector3(r * MathF.Cos(theta), h / 2, r * MathF.Sin(theta)));
        //    }
        //    for (int i = 0; i < steps; i++)
        //    {
        //        int a = baseSideIdx + 2 * i, b = a + 2, c = a + 1, d = b + 1;
        //        m.Triangles.Add(new Triangle { A = a, B = b, C = c });
        //        m.Triangles.Add(new Triangle { A = b, B = d, C = c });
        //    }
        //    return m;
        //}

        static Mesh GenCylinder(float radius, float height, int density)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = mesh.Vertices;
            List<Triangle> triangles = mesh.Triangles;

            int slices = Math.Max(3, density); // Ensure minimum of 3 slices

            // Bottom circle
            vertices.Add(new Vector3(0, 0, 0)); // center bottom
            for (int i = 0; i < slices; i++)
            {
                float angle = 2 * MathF.PI * i / slices;
                vertices.Add(new Vector3(radius * MathF.Cos(angle), 0, radius * MathF.Sin(angle)));
            }

            // Top circle
            vertices.Add(new Vector3(0, height, 0)); // center top
            int topCenterIndex = vertices.Count - 1;
            int baseOffset = 1; // index where bottom circle verts start

            for (int i = 0; i < slices; i++)
            {
                float angle = 2 * MathF.PI * i / slices;
                vertices.Add(new Vector3(radius * MathF.Cos(angle), height, radius * MathF.Sin(angle)));
            }

            int topOffset = baseOffset + slices;

            // Side triangles
            for (int i = 0; i < slices; i++)
            {
                int next = (i + 1) % slices;

                int bottomA = baseOffset + i;
                int bottomB = baseOffset + next;
                int topA = topOffset + i;
                int topB = topOffset + next;

                // First triangle (bottomA, topA, topB)
                triangles.Add(new Triangle { A = bottomA, B = topA, C = topB });

                // Second triangle (bottomA, topB, bottomB)
                triangles.Add(new Triangle { A = bottomA, B = topB, C = bottomB });
            }

            // Bottom cap
            for (int i = 0; i < slices; i++)
            {
                int next = (i + 1) % slices;
                triangles.Add(new Triangle
                {
                    A = 0, // center bottom
                    B = baseOffset + next,
                    C = baseOffset + i
                });
            }

            // Top cap
            for (int i = 0; i < slices; i++)
            {
                int next = (i + 1) % slices;
                triangles.Add(new Triangle
                {
                    A = topCenterIndex,
                    B = topOffset + i,
                    C = topOffset + next
                });
            }

            return mesh;
        }

        static Mesh GenCone(float r, float h, int steps)
        {
            var m = new Mesh();
            // base circle
            int baseCenter = m.Vertices.Count;
            m.Vertices.Add(new Vector3(0, -h / 2, 0));
            for (int i = 0; i < steps; i++)
            {
                float th = 2 * MathF.PI * i / steps;
                m.Vertices.Add(new Vector3(r * MathF.Cos(th), -h / 2, r * MathF.Sin(th)));
            }
            for (int i = 0; i < steps; i++)
            {
                m.Triangles.Add(new Triangle { A = baseCenter, B = baseCenter + 1 + i, C = baseCenter + 1 + (i + 1) % steps });
            }
            Vector3 apex = new Vector3(0, h / 2, 0);
            int apexIdx = m.Vertices.Count;
            m.Vertices.Add(apex);
            for (int i = 0; i < steps; i++)
            {
                int vi = baseCenter + 1 + i;
                int vj = baseCenter + 1 + (i + 1) % steps;
                m.Triangles.Add(new Triangle { A = vi, B = vj, C = apexIdx });
            }
            return m;
        }

        static Mesh GenCuboid(float x, float y, float z)
        {
            var m = new Mesh();
            var verts = new[] {
                new Vector3(-x/2,-y/2,-z/2),new Vector3(x/2,-y/2,-z/2),
                new Vector3(x/2,y/2,-z/2), new Vector3(-x/2,y/2,-z/2),
                new Vector3(-x/2,-y/2,z/2), new Vector3(x/2,-y/2,z/2),
                new Vector3(x/2,y/2,z/2), new Vector3(-x/2,y/2,z/2)
            };
            m.Vertices.AddRange(verts);
            int[] idx = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int[,] faces = { { 0, 1, 2, 3 }, { 7, 6, 5, 4 }, { 0, 4, 5, 1 }, { 1, 5, 6, 2 }, { 2, 6, 7, 3 }, { 3, 7, 4, 0 } };
            for (int i = 0; i < 6; i++)
            {
                int a = faces[i, 0], b = faces[i, 1], c = faces[i, 2], d = faces[i, 3];
                m.Triangles.Add(new Triangle { A = a, B = b, C = d });
                m.Triangles.Add(new Triangle { A = b, B = c, C = d });
            }
            return m;
        }
    }
}