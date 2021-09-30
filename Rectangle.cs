namespace GeoTiffReaderTest
{
  class Rectangle
  {
    public Rectangle( Point2d tl, Point2d br )
    {
      TopLeft = tl;
      BottomRight = br;
    }

    public Point2d TopLeft { get; set; }
    public Point2d BottomRight { get; set; }
    public Point2d TopRight { get { return Point2d.Create( BottomRight.X, TopLeft.Y ); } private set { } }
    public Point2d BottomLeft { get { return Point2d.Create( TopLeft.X, BottomRight.Y ); } private set { } }

    public Point2d Size
    {
      get
      {
        return Point2d.Create( BottomRight.X - TopLeft.X, TopLeft.Y - BottomRight.Y );
      }
      private set { }
    }

    public override string ToString() 
    {
      return $"[{TopLeft}, {BottomRight}]";
    }
    public string AsWkt()
    {
      return $"wkt;\nlinestring({TopLeft.X} {TopLeft.Y}, {BottomRight.X} {TopLeft.Y}, {BottomRight.X} {BottomRight.Y}, {TopLeft.X} {BottomRight.Y}, {TopLeft.X} {TopLeft.Y})";
    }
  }
}
