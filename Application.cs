using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeoTiffReaderTest
{
  public class Application
  {
    string mInputDataFilePath;
    Extent mRegion;
    string mOutputPath;
    int mMeshSubdivision;

    public Application( string[] args )
    {
      try
      {
        GdalConfiguration.ConfigureGdal();
        ParseArgs( args );
      }
      catch ( Exception e )
      {
        throw new Exception( "Application failed: ", e );
      }
    }

    public void run()
    {
      GetTileImages( out List<string> tileImagePaths, out Point2i tileDim );

      var geoTiff = GeoTiffFactory.CreateFromTiles( tileImagePaths, tileDim );
      CreateMesh( geoTiff );
    }

    void GetTileImages( out List<string> tileImagePaths, out Point2i tileDim )
    {
      tileDim = Point2i.Create();
      tileImagePaths = new List<string>();
      using ( var e = new StreamReader( mInputDataFilePath )  )
      {
        string line;
        bool firstLine = true;
        while ( (line = e.ReadLine() ) != null )
        {
          line = line.Trim();
          if ( line.Length == 0 || line[0] == '#' )
            continue;
          if ( line[0] == '"' )
            line = line.Remove( 0, 1 );
          if ( line[line.Length - 1] == '"' )
            line = line.Remove( line.Length - 1, 1 );

          if ( firstLine )
          {
            firstLine = false;
            var dimensions = Regex.Matches( line, @"\d+" ).Cast<Match>().Select( m => m.Value ).ToArray();
            if ( dimensions.Length != 2 )
            {
              throw new ArgumentException( "invalid tile dimensions" );
            }
            tileDim.Set( int.Parse( dimensions[0] ), int.Parse( dimensions[1] ) );
          }
          else
          {
            tileImagePaths.Add( line );
          }
        }
      }
    }

    void CreateMesh( GeoTiff geoTiff )
    {
      Utils.CreateFile( mOutputPath + @"\extent.csv", geoTiff.Extent.AsWkt() );

      geoTiff.GetMinMax( out float min, out float max, out Point2i minPixel, out Point2i maxPixel );
      geoTiff.PixelToGeo( minPixel, out Point2d minGeo, GeoTiff.PixelPosition.Center );
      geoTiff.PixelToGeo( maxPixel, out Point2d maxGeo, GeoTiff.PixelPosition.Center );

      Console.WriteLine( $"Minimum height is {min}m, maximum is {max}m" );
      Utils.CreateFile( mOutputPath + @"\minGeo.csv", minGeo.AsWkt() );
      Utils.CreateFile( mOutputPath + @"\maxGeo.csv", maxGeo.AsWkt() );

      // create normalized height data from the input image (height between 0.0m and 1.0m)
      // iTODO region only
      geoTiff.Normalize();
      geoTiff.Write( $"{mOutputPath}heightfield.tif" );

      Console.WriteLine($"{geoTiff.Extent.SizeInMeters}");

      //// Create mesh from the region
      //{
      //  Utils.CreateFile( mOutputPath + @"\region.csv", mRegion.AsWkt() );
      //  Utils.CreateFile( mOutputPath + @"\regionbl.csv", mRegion.BottomLeft.AsWkt() );
      //  Utils.CreateFile( mOutputPath + @"\regiontr.csv", mRegion.TopRight.AsWkt() );

      //  // get region in pixel space
      //  geoTiff.GeoToPixel( mRegion.BottomLeft, out Point2i pixelBottomLeft );
      //  geoTiff.GeoToPixel( mRegion.TopLeft, out Point2i pixelTopLeft );
      //  geoTiff.GeoToPixel( mRegion.TopRight, out Point2i pixelTopRight );

      //  // expand to pixel's outter side
      //  geoTiff.PixelToGeo( pixelBottomLeft, out Point2d geoBottomLeft, GeoTiff.PixelPosition.BottomLeft );
      //  geoTiff.PixelToGeo( pixelTopLeft, out Point2d geoTopLeft, GeoTiff.PixelPosition.TopLeft );
      //  geoTiff.PixelToGeo( pixelTopRight, out Point2d geoTopRight, GeoTiff.PixelPosition.TopRight );

      //  Utils.CreateFile( mOutputPath + @"\regionBottomLeft.csv", geoBottomLeft.AsWkt() );
      //  Utils.CreateFile( mOutputPath + @"\regionTopLeft.csv", geoTopLeft.AsWkt() );
      //  Utils.CreateFile( mOutputPath + @"\regionTopRight.csv", geoTopRight.AsWkt() );

      //  var dh = max - min;
      //  var meterScaled = 1.0 / dh;
      //  var dLefTopRight = Utils.Distance( geoTopLeft, geoTopRight );
      //  var dTopBottom = Utils.Distance( geoTopLeft, geoBottomLeft );
      //  Console.WriteLine( $"dLefTopRight is {dLefTopRight}m, dTopBottom is {dTopBottom}m" );

      //  var dLefTopRightScaled = dLefTopRight * meterScaled;
      //  var dTopBottomScaled = dTopBottom * meterScaled;

      //  var vertices = new List<double>();
      //  var uvs = new List<double>();
      //  var faces = new List<int>();
      //  var vertexIndexTable = new Dictionary<Point2d, int>();

      //  var meshOrigin = Point2d.Create();
      //  var uvOrigin = Point2d.Create();

      //  var dX = dLefTopRightScaled / mMeshSubdivision;
      //  var dY = dTopBottomScaled / mMeshSubdivision;
      //  var du = 1.0 / mMeshSubdivision;
      //  var dv = 1.0 / mMeshSubdivision;
      //  for ( int y = 0; y < mMeshSubdivision; ++y )
      //  {
      //    for ( int x = 0; x < mMeshSubdivision; ++x )
      //    {
      //      AddFace( vertices, uvs, faces, vertexIndexTable
      //               , meshOrigin.X + x * dX, meshOrigin.Y + y * dY, 0.0, uvOrigin.X + x * du, uvOrigin.Y + y * dv
      //               , meshOrigin.X + ( x + 1 ) * dX, meshOrigin.Y + y * dY, 0.0, uvOrigin.X + ( x + 1 ) * du, uvOrigin.Y + y * dv
      //               , meshOrigin.X + ( x + 1 ) * dX, meshOrigin.Y + ( y + 1 ) * dY, 0.0, uvOrigin.X + ( x + 1 ) * du, uvOrigin.Y + ( y + 1 ) * dv
      //               , meshOrigin.X + x * dX, meshOrigin.Y + ( y + 1 ) * dY, 0.0, uvOrigin.X + x * du, uvOrigin.Y + ( y + 1 ) * dv );
      //    }
      //  }

      //  // Wrtie out mesh obj
      //  double scale = 1.0;
      //  using ( StreamWriter wktStream = new StreamWriter( $"{mOutputPath}\\mesh.obj" ) )
      //  {
      //    for ( int i = 0; i < vertices.Count; i += 3 )
      //    {
      //      wktStream.WriteLine( $"v {vertices[i] * scale} {vertices[i + 1] * scale} {vertices[i + 2] * scale}" );
      //    }

      //    for ( int i = 0; i < uvs.Count; i += 2 )
      //    {
      //      wktStream.WriteLine( $"vt {uvs[i]} {uvs[i + 1]}" );
      //    }

      //    wktStream.WriteLine( $"vn 0.0 0.0 1.0" );

      //    for ( int i = 0; i < faces.Count; i += 8 ) // 4 * 2: 4 vertex 2 ints per vertex
      //    {
      //      wktStream.WriteLine( $"f {faces[i]}/{faces[i + 1]}/1 {faces[i + 2]}/{faces[i + 3]}/1 {faces[i + 4]}/{faces[i + 5]}/1 {faces[i + 6]}/{faces[i + 7]}/1" );
      //    }
      //  }
      //}
    }

    void AddVertex( List<double> vertices, List<double> uvs, Dictionary<Point2d, int> vertexIndexTable
                    , double x, double y, double z, double u, double v, out int vertexIdx, out int uvIdx )
    {
      var p = Point2d.Create( x, y );
      if ( !vertexIndexTable.ContainsKey( p ) )
      {
        vertices.Add( x );
        vertices.Add( y );
        vertices.Add( z );
        uvs.Add( u );
        uvs.Add( v );
        vertexIdx = vertices.Count / 3;
        uvIdx = uvs.Count / 2;
        vertexIndexTable.Add( p, vertexIdx );
      }
      else
      {
        vertexIdx = uvIdx = vertexIndexTable[p];
      }
    }

    void AddFace( List<double> vertices, List<double> uvs, List<int> faces, Dictionary<Point2d, int> vertexIndexTable
                  , double x0, double y0, double z0, double u0, double v0
                  , double x1, double y1, double z1, double u1, double v1
                  , double x2, double y2, double z2, double u2, double v2
                  , double x3, double y3, double z3, double u3, double v3 )
    {
      AddVertex( vertices, uvs, vertexIndexTable, x0, y0, z0, u0, v0, out int f0, out int uv0 );
      faces.Add( f0 );
      faces.Add( uv0 );

      AddVertex( vertices, uvs, vertexIndexTable, x1, y1, z1, u1, v1, out int f1, out int uv1 );
      faces.Add( f1 );
      faces.Add( uv1 );

      AddVertex( vertices, uvs, vertexIndexTable, x2, y2, z2, u2, v2, out int f2, out int uv2 );
      faces.Add( f2 );
      faces.Add( uv2 );

      AddVertex( vertices, uvs, vertexIndexTable, x3, y3, z3, u3, v3, out int f3, out int uv3 );
      faces.Add( f3 );
      faces.Add( uv3 );
    }

    void Sample( GeoTiff geoTiff, Point2d samplePosition, string msg )
    {
      if ( geoTiff.Extent.Contains( samplePosition ) )
      {
        Console.WriteLine( $"Height at {samplePosition} is {geoTiff.Sample( samplePosition )} {{{msg}}}" );
      }
      else
      {
        Console.WriteLine( $"Geo location {samplePosition} out of geotiff's bounds {{{msg}}}" );
      }
    }

    void ParseArgs( string[] args )
    {
      if ( args.Length < 5 )
      {
        throw new ArgumentException( @"Invalid arguments. Usage: datFilePath right bottom left top [outputPath=""Desktop"" meshSubdivision=16]" );
      }

      // mandatories
      mInputDataFilePath = args[0];
      if ( !File.Exists( mInputDataFilePath ) )
      {
        throw new ArgumentException( @"cannot find input data file" );
      }

      mRegion = Extent.Create( Point2d.Create( double.Parse( args[1] ), double.Parse( args[2] ) )
                               , Point2d.Create( double.Parse( args[3] ), double.Parse( args[4] ) ) );

      // optionals
      ParseOptional( args, 5, ref mOutputPath, Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ) );
      ParseOptional( args, 6, ref mMeshSubdivision, 32 );
    }

    void ParseOptional( string[] args, int idx, ref string value, string defaultValue )
    {
      if ( args.Length > idx )
      {
        value = args[idx];
      }
      else
      {
        value = defaultValue;
      }
    }
    void ParseOptional( string[] args, int idx, ref int value, int defaultValue )
    {
      if ( args.Length > idx )
      {
        value = int.Parse( args[idx] );
      }
      else
      {
        value = defaultValue;
      }
    }
  }
}
