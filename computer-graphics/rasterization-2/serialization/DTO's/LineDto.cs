namespace rasterization_2.serialization;

public class LineDto
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Thickness { get; set; }
    public required string Color { get; set; }
}
