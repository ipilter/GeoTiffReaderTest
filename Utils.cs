using System;
using System.IO;

// @equator:
// 1°          = 111 km ( or 60 nautical miles )
// 0.1°        = 11.1 km
// 0.01°       = 1.11 km ( 2 decimals, km accuracy )
// 0.001°      = 111 m
// 0.0001°     = 11.1 m
// 0.00001°    = 1.11 m
// 0.000001°   = 0.11 m ( 7 decimals, cm accuracy )
// 0.0000001°  = 0.011 m
// 0.00000001° = 0.0011 m ( 9 decimals, mm accuracy )


namespace GeoTiffReaderTest
{
  public class Utils
  {
    public static double Epsilon { get { return 0.00000001; } private set { } }

    public static bool Equal( double a, double b, double e )
    {
      return Math.Abs( a - b ) < e;
    }

    public static double ToRad( double deg )
    {
      return deg * Math.PI / 180.0;
    }

    public static double Distance( Point2d a, Point2d b )
    {
      var earthRadius = 6378.137; // km
      var dLat = ToRad( b.Y ) - ToRad( a.Y );
      var dLon = ToRad( b.X ) - ToRad( a.X );
      var t = Math.Sin( dLat / 2.0 ) * Math.Sin( dLat / 2.0 ) + Math.Cos( ToRad( a.Y ) ) * Math.Cos( ToRad( b.Y ) ) * Math.Sin( dLon / 2.0 ) * Math.Sin( dLon / 2.0 );
      var c = 2.0 * Math.Atan2( Math.Sqrt( t ), Math.Sqrt( 1.0 - t ) );
      return earthRadius * c * 1000.0;
    }

    public static void CreateFile( string filePath, string content )
    {
      try
      {
        using ( StreamWriter stream = new StreamWriter( filePath ) )
        {
          stream.WriteLine( content );
        }
      }
      catch ( Exception e )
      {
        Console.WriteLine( $"Warning: cannot create file {filePath}: {e.Message}" );
      }
    }

    public static double RangeMap( double a0, double a1, double b0, double b1, double value )
    {
      double a_norm = a1 - a0;
      if ( a_norm == 0.0 )
      {
        return 0.0;
      }
      return ( b0 + ( value - a0 ) * ( b1 - b0 ) ) / a_norm;
    }

    public static void GetMinMax( float[] data, Point2i dataDim, out float min, out float max, out Point2i minPixelPos, out Point2i maxPixelPos )
    {
      min = float.MaxValue;
      max = float.MinValue;
      minPixelPos = Point2i.Create();
      maxPixelPos = Point2i.Create();

      for ( int idx = 0; idx < dataDim.X * dataDim.Y; ++idx )
      {
        var h = data[idx];

        var y = (int)( idx / (double)dataDim.X ); // iTODO validate
        var x = (int)( idx % (double)dataDim.X );

        if ( h < min )
        {
          min = h;
          minPixelPos.Set( x, y );
        }
        if ( h > max )
        {
          max = h;
          maxPixelPos.Set( x, y );
        }
      }
    }
  }
}
