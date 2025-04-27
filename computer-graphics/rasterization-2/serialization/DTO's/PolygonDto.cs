namespace rasterization_2.serialization;

public class PolygonDto
{
    public required List<VertexDto> Vertices { get; set; }
    public double Thickness { get; set; }
    public required string Color { get; set; }
}
