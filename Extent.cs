namespace GeoTiffReaderTest
{
  public class Extent : Rectangle
  {
    public static new Extent Create( Point2d bl, Point2d tr ) // [lat, lon], [lat, lon]
    {
      return new Extent( bl, tr );
    }

    public static Extent Create( Extent other )
    {
      return new Extent( other.BottomLeft, other.TopRight );
    }

    protected Extent( Point2d bl, Point2d tr )
      : base( bl, tr )
    { }

    public bool Contains( Point2d geo )
    {
      return geo.X >= BottomLeft.X && geo.X < TopRight.X &&
             geo.Y >= BottomLeft.Y && geo.Y < TopRight.Y;
    }

    public Point2d SizeInMeters
    {
      get
      {
        return Point2d.Create( Utils.Distance( TopLeft, TopRight )
                               , Utils.Distance( TopLeft, BottomLeft ) );
      }
      private set { }
    }
  }
}
