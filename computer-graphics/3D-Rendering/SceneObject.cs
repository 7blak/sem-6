using Newtonsoft.Json;
using System.Numerics;

namespace _3D_Rendering
{
    public class SceneObject
    {
        public required string Type { get; set; }
        public required float[] Size { get; set; }
        public int MeshDensity { get; set; }

        [JsonIgnore]
        public Matrix4x4 Transform { get; set; }

        [JsonProperty("Transform")]
        public float[] TransformData
        {
            get => new float[]
            {
            Transform.M11, Transform.M12, Transform.M13, Transform.M14,
            Transform.M21, Transform.M22, Transform.M23, Transform.M24,
            Transform.M31, Transform.M32, Transform.M33, Transform.M34,
            Transform.M41, Transform.M42, Transform.M43, Transform.M44,
            };
            set
            {
                if (value.Length == 16)
                {
                    Transform = new Matrix4x4(
                        value[0], value[1], value[2], value[3],
                        value[4], value[5], value[6], value[7],
                        value[8], value[9], value[10], value[11],
                        value[12], value[13], value[14], value[15]);
                }
                else
                {
                    Transform = Matrix4x4.Identity;
                }
            }
        }
    }

}