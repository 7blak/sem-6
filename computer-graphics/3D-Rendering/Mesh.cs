using System.Numerics;

namespace _3D_Rendering
{
    public class Mesh
    {
        public List<Vector3> Vertices = [];
        public List<Triangle> Triangles = [];
        public Matrix4x4 Model = Matrix4x4.Identity;
    }
}