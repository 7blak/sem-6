using System.Windows.Media.Imaging;

namespace rasterization_2.serialization;

public class PolygonDto
{
    public required bool IsFillColor { get; set; }
    public required List<VertexDto> Vertices { get; set; }
    public required double Thickness { get; set; }
    public required string Color { get; set; }
    public required string FillColor { get; set; }
    public required string? BitmapSource { get; set; }
}
