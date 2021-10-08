namespace GeoTiffReaderTest
{
  public class Rectangle
  {
    public static Rectangle Create( Point2d bl, Point2d tr )
    {
      return new Rectangle( bl, tr );
    }

    protected Rectangle( Point2d bl, Point2d tr )
    {
      BottomLeft = bl;
      TopRight = tr;
    }

    public Point2d BottomLeft { get; set; }
    public Point2d TopRight { get; set; }
    public Point2d TopLeft { get { return Point2d.Create( BottomLeft.X, TopRight.Y ); } private set { } }
    public Point2d BottomRight { get { return Point2d.Create( TopRight.X, BottomLeft.Y ); } private set { } }

    public Point2d Size
    {
      get
      {
        return Point2d.Create( TopRight.X - BottomLeft.X, TopRight.Y - BottomLeft.Y );
      }
      private set { }
    }

    public override string ToString() 
    {
      return $"[{BottomLeft}, {TopRight}]";
    }
    public string AsWkt()
    {
      return $"wkt;\nlinestring({BottomLeft.X} {BottomLeft.Y}, {TopRight.X} {BottomLeft.Y}, {TopRight.X} {TopRight.Y}, {BottomLeft.X} {TopRight.Y}, {BottomLeft.X} {BottomLeft.Y})";
    }
  }
}
