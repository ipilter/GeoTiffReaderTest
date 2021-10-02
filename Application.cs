using System;
using System.Collections.Generic;
using System.IO;

namespace GeoTiffReaderTest
{
  public class Application
  {
    string mGeoTiffPath;
    Point2d samplePosition;
    string mWktPath;

    public Application( string[] args )
    {
      try
      {
        GdalConfiguration.ConfigureGdal();
        ParseArgs( args );
      }
      catch (Exception e)
      {
        throw new Exception( "Application failed: ", e );
      }
    }

    public void run()
    {
      var geoTiff = new GeoTiff( mGeoTiffPath );

      Utils.CreateWkt( mWktPath + @"\extent.csv", geoTiff.Extent.AsWkt() );

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
      Utils.CreateWkt( mWktPath + @"\minGeo.csv", minGeo.AsWkt() );
      Utils.CreateWkt( mWktPath + @"\maxGeo.csv", maxGeo.AsWkt() );

      // max -> 1.0
      // min -> 0.0
      var dh = max - min;
      var oneMeter = 1.0 / dh;
      var sizeInMeters = geoTiff.Extent.SizeInMeters;
      var gridWidth = sizeInMeters.X *= oneMeter;
      var gridLength = sizeInMeters.Y *= oneMeter;
      Console.WriteLine( $"Grid size with height between 0.0 and 1.0 is [{gridWidth}, {gridLength}]" );

      // Create a grid with the dims above (flat, displacement will be applied runtime) : 0,0 -> [gridWidth, gridLength]
      var gridBottomLeft = geoTiff.Extent.BottomLeft;
      var gridTopRight = geoTiff.Extent.BottomLeft + Point2d.Create( 0.2, 0.2 );
      {
        geoTiff.GeoToPixel( gridTopRight, out Point2i pixelTopRight );
        geoTiff.GeoToPixel( gridBottomLeft, out Point2i pixelgridBottomLeft );

        // create geometry
        var vertices = new List<double>();
        var normals = new List<double>();
        var faces = new List<uint>();

        //var originX = 0.0;
        //var originY = 0.0;

        // 

        // write geometry as Obj file to disk
      }
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
