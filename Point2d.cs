namespace GeoTiffReaderTest
{
  class Point2d
  {
    public static Point2d Create(double x = 0.0, double y = 0.0 )
    {
      return new Point2d( x, y );
    }

    public Point2d( double x, double y )
    {
      X = x;
      Y = y;
    }

    public double X { get; set; }
    public double Y { get; set; }

    public static Point2d operator + ( Point2d a, Point2d b )
    {
      return Create( a.X + b.X, a.Y + b.Y );
    }

    public void Clone( Point2d other )
    {
      X = other.X;
      Y = other.Y;
    }

    public override string ToString()
    {
      return $"[{X}, {Y}]";
    }

    public string AsWkt()
    {
      return $"wkt;\npoint({X} {Y})";
    }
  }
}
