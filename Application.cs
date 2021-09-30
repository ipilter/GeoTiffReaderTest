using System;
using System.Collections.Generic;
using System.IO;
using GlmNet;

namespace GeoTiffReaderTest
{
  class Application
  {
    string mGeoTiffPath;
    Point2d samplePosition;
    string mWktPath;

    public Application( string[] args )
    {
      try
      {
        ParseArgs( args );

        GdalConfiguration.ConfigureGdal();
      }
      catch (Exception e)
      {
        throw new Exception( "Application failed: ", e );
      }
    }

    public void run()
    {
      var geoTiff = new GeoTiff( mGeoTiffPath );

      Console.WriteLine( $"Size: {geoTiff.Size}" );
      Console.WriteLine( $"Extent: {geoTiff.Extent}" );
      Console.WriteLine( $"Resolution: {geoTiff.Resolution}" );
      Console.WriteLine( $"Extent size = {geoTiff.Extent.Size}deg, {geoTiff.Extent.SizeInMeters}m" );
      Console.WriteLine( $"Inverted Y: {( geoTiff.InverseY ? "yes" : "no" )}" );

      CreateWkt( "extent", geoTiff.Extent.AsWkt() );
      CreateWkt( "samplePoint", samplePosition.AsWkt() );

      var right = Point2d.Create( samplePosition.X + geoTiff.Resolution.X, samplePosition.Y );
      var topRight = Point2d.Create( samplePosition.X + geoTiff.Resolution.X, samplePosition.Y + geoTiff.Resolution.Y );
      var left = Point2d.Create( samplePosition.X - geoTiff.Resolution.X, samplePosition.Y );
      var bottom = Point2d.Create( samplePosition.X, samplePosition.Y - geoTiff.Resolution.Y );
      var top = Point2d.Create( samplePosition.X, samplePosition.Y + geoTiff.Resolution.Y );

      CreateWkt( "samplePointRightNeighbour", right.AsWkt() );
      CreateWkt( "samplePointTopRightNeighbour", topRight.AsWkt() );
      CreateWkt( "samplePointLeftNeighbour", left.AsWkt() );
      CreateWkt( "samplePointBottomNeighbour", bottom.AsWkt() );
      CreateWkt( "samplePointTopNeighbour", top.AsWkt() );

      Sample( geoTiff, samplePosition );
      Sample( geoTiff, right );
      Sample( geoTiff, topRight );
      Sample( geoTiff, left );
      Sample( geoTiff, bottom );
      Sample( geoTiff, top );

      CreateWkt( "TopLeft", geoTiff.Extent.TopLeft.AsWkt() );
      CreateWkt( "TopRight", geoTiff.Extent.TopRight.AsWkt() );
      CreateWkt( "BottomLeft", geoTiff.Extent.BottomLeft.AsWkt() );

      var min = double.MaxValue;
      var max = double.MinValue;
      Point2i minPixel = Point2i.Create();
      Point2i maxPixel = Point2i.Create();
      {
        var pixel = Point2i.Create();
        for ( pixel.Y = 0; pixel.Y < geoTiff.Size.Y; ++pixel.Y )
        {
          for ( pixel.X = 0; pixel.X < geoTiff.Size.X; ++pixel.X )
          {
            var height = geoTiff.Pixel( pixel );
            if ( height > max )
            {
              max = height;
              maxPixel.Clone( pixel );
            }
            if ( height < min )
            {
              min = height;
              minPixel.Clone( pixel );
            }
          }
        }
      }
      Console.WriteLine($"Minimum height is {min}m, maximum is {max}m");
      geoTiff.PixelToGeo( minPixel, out Point2d minGeo, GeoTiff.PixelPosition.Center );
      geoTiff.PixelToGeo( maxPixel, out Point2d maxGeo, GeoTiff.PixelPosition.Center );
      CreateWkt( "minGeo", minGeo.AsWkt() );
      CreateWkt( "maxGeo", maxGeo.AsWkt() );

      // max -> 1.0
      // min -> 0.0
      var dh = max - min;
      var oneMeter = 1.0 / dh;
      var sizeInMeters = geoTiff.Extent.SizeInMeters;
      var gridWidth = sizeInMeters.X *= oneMeter;
      var gridLength = sizeInMeters.Y *= oneMeter;
      Console.WriteLine( $"Grid size with height between 0.0 and 1.0 is [{gridWidth}, {gridLength}]" );

      // Create a grid with the dims above (flat, displacement will be applied runtime) : 0,0 -> [gridWidth, gridLength]
      var gridTopLeft = geoTiff.Extent.TopLeft;
      var gridBottomRight = geoTiff.Extent.TopLeft + Point2d.Create( 0.2, -0.2 );
      {
        geoTiff.GeoToPixel( gridTopLeft, out Point2i pixelTopLeft );
        geoTiff.GeoToPixel( gridBottomRight, out Point2i pixelBottomRight );

        // create geometry
        var vertices = new List<double>();
        var normals = new List<double>();
        var faces = new List<uint>();

        var originX = 0.0;
        var originY = 0.0;

        // 

        // write geometry as Obj file to disk
      }
    }

    void Sample( GeoTiff geoTiff, Point2d samplePosition )
    {
      if ( geoTiff.Extent.Contains( samplePosition ) )
      {
        Console.WriteLine( $"Height at {samplePosition} is {geoTiff.Sample( samplePosition )}" );
      }
      else
      {
        Console.WriteLine( $"Geo location {samplePosition} out of geotiff's bounds" );
      }
    }

    void CreateWkt( string fileName, string content )
    {
      try
      {
        using ( StreamWriter wktStream = new StreamWriter( mWktPath + @"\" + fileName + ".csv" ) )
        {
          wktStream.WriteLine( content );
        }
      }
      catch ( Exception e )
      {
        Console.WriteLine( $"Warning: cannot crete {fileName}: {e.Message}" );
      }
    }

    void ParseArgs( string[] args )
    {
      if ( args.Length < 3 )
      {
        throw new Exception( @"Invalid arguments. Usage: tiffPath geoX geoY [wktPath=""d:\""]" );
      }

      // mandatories
      mGeoTiffPath = args[0];
      samplePosition = Point2d.Create( double.Parse( args[1] ), double.Parse( args[2] ) );

      // optionals
      if ( args.Length > 3 )
      {
        mWktPath = $"{args[3]}";
      }
      else
      {
        mWktPath = Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory );
      }
    }
  }
}
