namespace GeoTiffReaderTest
{
  class Point2i
  {
    public static Point2i Create( int x = 0, int y = 0 )
    {
      return new Point2i( x, y );
    }

    public Point2i( int x, int y )
    {
      X = x;
      Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public static Point2i operator + ( Point2i a, Point2i b )
    {
      return Create( a.X + b.X, a.Y + b.Y );
    }

    public void Clone( Point2i other )
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
