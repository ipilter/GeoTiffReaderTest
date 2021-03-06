using System;

namespace GeoTiffReaderTest
{
  public class Point2d
  {
    public static Point2d Create( double x = 0.0, double y = 0.0 )
    {
      return new Point2d( x, y );
    }

    protected Point2d( double x, double y )
    {
      X = x;
      Y = y;
    }

    public double X { get; set; }
    public double Y { get; set; }

    public void Set( Point2d other )
    {
      Set( other.X, other.Y );
    }

    public void Set( double x = 0, double y = 0 )
    {
      X = x;
      Y = y;
    }

    public override string ToString()
    {
      return $"[{X}, {Y}]";
    }

    public string AsWkt()
    {
      return $"wkt;\npoint({X} {Y})";
    }
    public static Point2d operator +( Point2d a, Point2d b )
    {
      return Create( a.X + b.X, a.Y + b.Y );
    }

    public static Point2d operator -( Point2d a, Point2d b )
    {
      return Create( a.X - b.X, a.Y - b.Y );
    }

    public static bool operator ==( Point2d a, Point2d b )
    {
      return Utils.Equal( a.X, b.X, Utils.Epsilon ) && Utils.Equal( a.Y, b.Y, Utils.Epsilon );
    }

    public static bool operator !=( Point2d a, Point2d b )
    {
      return !(a==b);
    }

    public override bool Equals( object o )
    {
      if ( o == null || !( o is Point2d ) )
      {
        return false;
      }

      var other = (Point2d)o;
      return other == this;
    }

    public override int GetHashCode()
    {
      return Tuple.Create( X, Y ).GetHashCode();
    }
  }
}
