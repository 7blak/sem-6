namespace rasterization_2.serialization;

public class ProjectData
{
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    public required List<LineDto> Lines { get; set; }
    public required List<CircleDto> Circles { get; set; }
    public required List<PolygonDto> Polygons { get; set; }
    public required List<RectangleDto> Rectangles { get; set; }
}
