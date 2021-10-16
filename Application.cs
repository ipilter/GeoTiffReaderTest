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
      ParseInputTileDataFile( out List<string> tileImagePaths, out Point2i tileDim );
      Console.WriteLine( $"Loading input geotiff..." );
      var geoTiff = new GeoTiff( tileImagePaths, tileDim );
      var subGeoTiff = new GeoTiff( geoTiff, mRegion );
      if ( subGeoTiff == null )
      {
        throw new ApplicationException( "cannot create sub image from input geotiff" );
      }
      CreateMesh( subGeoTiff );
    }

    void ParseInputTileDataFile( out List<string> tileImagePaths, out Point2i tileDim )
    {
      tileDim = Point2i.Create();
      tileImagePaths = new List<string>();
      mRegion = Extent.Create( Point2d.Create(), Point2d.Create() );

      using ( var e = new StreamReader( mInputDataFilePath )  )
      {
        string line;
        uint lineIdx = 0;
        while ( (line = e.ReadLine() ) != null )
        {
          line = line.Trim();
          if ( line.Length == 0 || line[0] == '#' )
          {
            continue;
          }
          if ( line[0] == '"' )
          {
            line = line.Remove( 0, 1 ); 
          }
          if ( line[line.Length - 1] == '"' )
          {
            line = line.Remove( line.Length - 1, 1 );
          }

          if ( lineIdx == 0 )
          {
            var dimensions = Regex.Matches( line, @"\d+" ).Cast<Match>().Select( m => m.Value ).ToArray();
            if ( dimensions.Length != 2 )
            {
              throw new ArgumentException( "invalid tile dimensions" );
            }
            tileDim.Set( int.Parse( dimensions[0] ), int.Parse( dimensions[1] ) );
          }
          else if( lineIdx == 1 )
          {
            var lbrt = Regex.Matches( line, @"-?\d+.\d+" ).Cast<Match>().Select( m => m.Value ).ToArray();
            if ( lbrt.Length != 4 )
            {
              throw new ArgumentException( "invalid region values" );
            }
            mRegion.BottomLeft.Set( double.Parse( lbrt[0] ), double.Parse( lbrt[1] ) );
            mRegion.TopRight.Set( double.Parse( lbrt[2] ), double.Parse( lbrt[3] ) );
          }
          else
          {
            tileImagePaths.Add( line );
          }

          ++lineIdx;
        }
      }
    }

    void CreateMesh( GeoTiff geoTiff )
    {
      Console.WriteLine( $"Creating Mesh..." );
      Utils.CreateFile( mOutputPath + @"\extent.csv", geoTiff.Extent.AsWkt() );

      geoTiff.GetMinMax( out float subMin, out float subMax, out Point2i subMinPixel, out Point2i subMaxPixel );
      geoTiff.PixelToGeo( subMinPixel, out Point2d subMinGeo, GeoTiff.PixelPosition.Center );
      geoTiff.PixelToGeo( subMaxPixel, out Point2d subMaxGeo, GeoTiff.PixelPosition.Center );

      Console.WriteLine( $"Minimum height is {subMin}m, maximum is {subMax}m" );
      Utils.CreateFile( mOutputPath + @"\subMinGeo.csv", subMinGeo.AsWkt() );
      Utils.CreateFile( mOutputPath + @"\subMaxGeo.csv", subMaxGeo.AsWkt() );


      // create normalized height data from the input image (height between 0.0m and 1.0m)
      geoTiff.GetMinMax( out float min, out float max );
      geoTiff.Normalize();
      geoTiff.Write( $"{mOutputPath}heightfield.tif" );

      Console.WriteLine($"{geoTiff.Extent.SizeInMeters}");

      // Create mesh from the region
      {
        // get region in pixel space
        geoTiff.GeoToPixel( mRegion.BottomLeft, out Point2i pixelBottomLeft );
        geoTiff.GeoToPixel( mRegion.TopLeft, out Point2i pixelTopLeft );
        geoTiff.GeoToPixel( mRegion.TopRight, out Point2i pixelTopRight );
        // expand to pixel's outter side
        geoTiff.PixelToGeo( pixelBottomLeft, out Point2d geoBottomLeft, GeoTiff.PixelPosition.BottomLeft );
        geoTiff.PixelToGeo( pixelTopLeft, out Point2d geoTopLeft, GeoTiff.PixelPosition.TopLeft );
        geoTiff.PixelToGeo( pixelTopRight, out Point2d geoTopRight, GeoTiff.PixelPosition.TopRight );

        var dh = max - min;
        var meterScaled = 1.0 / dh;
        var dLefTopRight = Utils.Distance( geoTopLeft, geoTopRight );
        var dTopBottom = Utils.Distance( geoTopLeft, geoBottomLeft );
        Console.WriteLine( $"dLefTopRight is {dLefTopRight}m, dTopBottom is {dTopBottom}m" );

        var dLefTopRightScaled = dLefTopRight * meterScaled;
        var dTopBottomScaled = dTopBottom * meterScaled;

        var vertices = new List<double>();
        var uvs = new List<double>();
        var faces = new List<int>();
        var vertexIndexTable = new Dictionary<Point2d, int>();

        var meshOrigin = Point2d.Create();
        var uvOrigin = Point2d.Create();

        var dX = dLefTopRightScaled / mMeshSubdivision;
        var dY = dTopBottomScaled / mMeshSubdivision;
        var du = 1.0 / mMeshSubdivision;
        var dv = 1.0 / mMeshSubdivision;
        for ( int y = 0; y < mMeshSubdivision; ++y )
        {
          for ( int x = 0; x < mMeshSubdivision; ++x )
          {
            AddFace( vertices, uvs, faces, vertexIndexTable
                     , meshOrigin.X + x * dX, meshOrigin.Y + y * dY, 0.0, uvOrigin.X + x * du, uvOrigin.Y + y * dv
                     , meshOrigin.X + ( x + 1 ) * dX, meshOrigin.Y + y * dY, 0.0, uvOrigin.X + ( x + 1 ) * du, uvOrigin.Y + y * dv
                     , meshOrigin.X + ( x + 1 ) * dX, meshOrigin.Y + ( y + 1 ) * dY, 0.0, uvOrigin.X + ( x + 1 ) * du, uvOrigin.Y + ( y + 1 ) * dv
                     , meshOrigin.X + x * dX, meshOrigin.Y + ( y + 1 ) * dY, 0.0, uvOrigin.X + x * du, uvOrigin.Y + ( y + 1 ) * dv );
          }
        }

        // Wrtie out mesh obj
        double scale = 1.0;
        using ( StreamWriter wktStream = new StreamWriter( $"{mOutputPath}\\mesh.obj" ) )
        {
          for ( int i = 0; i < vertices.Count; i += 3 )
          {
            wktStream.WriteLine( $"v {vertices[i] * scale} {vertices[i + 1] * scale} {vertices[i + 2] * scale}" );
          }

          for ( int i = 0; i < uvs.Count; i += 2 )
          {
            wktStream.WriteLine( $"vt {uvs[i]} {uvs[i + 1]}" );
          }

          wktStream.WriteLine( $"vn 0.0 0.0 1.0" );

          for ( int i = 0; i < faces.Count; i += 8 ) // 4 * 2: 4 vertex 2 ints per vertex
          {
            wktStream.WriteLine( $"f {faces[i]}/{faces[i + 1]}/1 {faces[i + 2]}/{faces[i + 3]}/1 {faces[i + 4]}/{faces[i + 5]}/1 {faces[i + 6]}/{faces[i + 7]}/1" );
          }
        }
      }
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
      if ( args.Length < 1 )
      {
        throw new ArgumentException( @"Invalid arguments. Usage: datFilePath [outputPath=""Desktop"" meshSubdivision=16]" );
      }

      // mandatories
      mInputDataFilePath = args[0];
      if ( !File.Exists( mInputDataFilePath ) )
      {
        throw new ArgumentException( @"cannot find input data file" );
      }

      // optionals
      ParseOptional( args, 1, ref mOutputPath, Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ) );
      ParseOptional( args, 2, ref mMeshSubdivision, 32 );
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
