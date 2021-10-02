using System;
using System.IO;

namespace GeoTiffReaderTest
{
  public class Utils
  {
    public static bool Equals( double a, double b, double eps = 0.0000001 )
    {
      return Math.Abs( a - b ) < eps;
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

    public static void CreateWkt( string filePath, string content )
    {
      try
      {
        using ( StreamWriter wktStream = new StreamWriter( filePath ) )
        {
          wktStream.WriteLine( content );
        }
      }
      catch ( Exception e )
      {
        Console.WriteLine( $"Warning: cannot crete {filePath}: {e.Message}" );
      }
    }
  }
}
