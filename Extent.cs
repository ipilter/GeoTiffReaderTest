namespace GeoTiffReaderTest
{
  class Extent : Rectangle
  {
    public Extent( Point2d tl, Point2d br ) // [lat, lon], [lat, lon]
      : base(tl, br)
    { }

    public bool Contains( Point2d geo )
    {
      return geo.X >= TopLeft.X && geo.X <= BottomRight.X &&
             geo.Y <= TopLeft.Y && geo.Y >= BottomRight.Y;
    }

    public Point2d SizeInMeters
    { 
      get
      {
        return Point2d.Create( Utils.Distance(TopLeft, TopRight)
                               , Utils.Distance(TopLeft, BottomLeft) );
      }
      private set { }
    }
  }
}
