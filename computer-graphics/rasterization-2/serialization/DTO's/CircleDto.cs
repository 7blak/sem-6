namespace rasterization_2.serialization;

public class CircleDto
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double Thickness { get; set; }
    public required string Color { get; set; }
}
