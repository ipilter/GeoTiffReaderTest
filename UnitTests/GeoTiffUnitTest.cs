using System;
using System.Collections.Generic;
using System.IO;
using GeoTiffReaderTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
  [TestClass]
  public class GeoTiffUnitTest
  {
    [ClassInitialize]
    public static void TestFixtureSetup( TestContext context )
    {
      GdalConfiguration.ConfigureGdal();
    }

    [TestMethod]
    public void TestCreationViaSingleFile()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage  );

      Assert.AreEqual( geoTiff.Extent.TopRight, Point2d.Create( -114, 37 ) );
      Assert.AreEqual( geoTiff.Extent.TopLeft, Point2d.Create( -115, 37 ) );
      Assert.AreEqual( geoTiff.Extent.BottomLeft, Point2d.Create( -115, 36 ) );
      Assert.AreEqual( geoTiff.Extent.BottomRight, Point2d.Create( -114, 36 ) );

      Assert.AreEqual( geoTiff.Size, Point2i.Create( 3600, 3600 ) );
      Assert.IsTrue( geoTiff.NorthUp );
      Assert.AreEqual( geoTiff.Resolution, Point2d.Create( 1.0 / 3600, 1.0 / 3600 ) );

      Assert.AreEqual( geoTiff.Pixel( Point2i.Create( 0, 0 ) ), 804 );
      Assert.AreEqual( geoTiff.Pixel( Point2i.Create( 3599, 0 ) ), 665 );
      Assert.AreEqual( geoTiff.Pixel( Point2i.Create( 0, 3599 ) ), 795 );
      Assert.AreEqual( geoTiff.Pixel( Point2i.Create( 3599, 3599 ) ), 915 );

      Assert.AreEqual( geoTiff.Extent.Size, Point2d.Create( 1.0, 1.0 ) );
      Assert.AreEqual( geoTiff.Extent.SizeInMeters, Point2d.Create( 88903.2896148812, 111319.490793274 ) );
    }

    [TestMethod]
    public void TestCreationViaTileFileList()
    {
      var tileCount = Point2i.Create( 2, 1 );
      List<string> tileFileList = new List<string>();
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N036W115_DSM.tif" );
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N036W114_DSM.tif" );

      foreach(var tile in tileFileList )
      {
        Assert.IsTrue( File.Exists( tile ) );
      }

      GeoTiff geoTiff = new GeoTiff( tileFileList, tileCount );

      Assert.AreEqual( geoTiff.Extent.BottomLeft, Point2d.Create( -115, 36 ) );
      Assert.AreEqual( geoTiff.Extent.TopRight, Point2d.Create( -113, 37 ) );

      Assert.AreEqual( geoTiff.Extent.TopLeft, Point2d.Create( -115, 37 ) );
      Assert.AreEqual( geoTiff.Extent.BottomRight, Point2d.Create( -113, 36 ) );
      
      var width = 3600 * 2;
      var height = 3600;
      Assert.AreEqual( geoTiff.Size, Point2i.Create( width, height ) );
      Assert.IsTrue( geoTiff.NorthUp );
      Assert.AreEqual( geoTiff.Resolution, Point2d.Create( 2.0 / width, 1.0 / height ) );

      Assert.AreEqual( 804, geoTiff.Pixel( Point2i.Create( 0, 0 ) ) );
      Assert.AreEqual( 665, geoTiff.Pixel( Point2i.Create( 3599, 0 ) ) );
      Assert.AreEqual( 795, geoTiff.Pixel( Point2i.Create( 0, 3599 ) ) );
      Assert.AreEqual( 915, geoTiff.Pixel( Point2i.Create( 3599, 3599 ) ) );

      Assert.AreEqual( geoTiff.Extent.Size, Point2d.Create( 2.0, 1.0 ) );
      //Assert.AreEqual( geoTiff.Extent.SizeInMeters, Point2d.Create( 88903.2896148812 * 2.0, 111319.490793274 ) );
    }

    [TestMethod]
    public void TestGeoToPixel()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage );

      {
        geoTiff.GeoToPixel( geoTiff.Extent.TopLeft + Point2d.Create( geoTiff.Resolution.X * 0.5, -geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( pixel, Point2i.Create( 0, 0 ) );
      }
      {
        geoTiff.GeoToPixel( geoTiff.Extent.TopRight + Point2d.Create( -geoTiff.Resolution.X * 0.5, -geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( pixel, Point2i.Create( 3599, 0 ) );
      }
      {
        geoTiff.GeoToPixel( geoTiff.Extent.BottomRight + Point2d.Create( -geoTiff.Resolution.X * 0.5, +geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( pixel, Point2i.Create( 3599, 3599 ) );
      }
      {
        geoTiff.GeoToPixel( geoTiff.Extent.BottomLeft + Point2d.Create( +geoTiff.Resolution.X * 0.5, +geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( pixel, Point2i.Create( 0, 3599 ) );
      }
    }

    [TestMethod]
    public void TestPixelToGeo()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage );

      var testPixels = new List<Point2i>() {
        Point2i.Create( 0, 0 )
        , Point2i.Create( 1, 0 )
        , Point2i.Create( 0, 1 )
        , Point2i.Create( 1, 1 )
        , Point2i.Create( 5, 3 )
      };
      var subPixelPositions = new List<Tuple<GeoTiff.PixelPosition, Point2d>>() {
        Tuple.Create( GeoTiff.PixelPosition.Center, Point2d.Create( 0.5, 0.5 ) )
        , Tuple.Create( GeoTiff.PixelPosition.BottomLeft, Point2d.Create( 0.0, 1.0 ) )
        , Tuple.Create( GeoTiff.PixelPosition.BottomRight, Point2d.Create( 1.0, 1.0 ) )
        , Tuple.Create( GeoTiff.PixelPosition.TopRight, Point2d.Create( 1.0, 0.0 ) )
        , Tuple.Create( GeoTiff.PixelPosition.TopLeft, Point2d.Create( 0.0, 0.0 ) )
      };

      foreach ( var subPixelPosition in subPixelPositions )
      {
        var positionInPixel = subPixelPosition.Item1;
        var subPixelPos = subPixelPosition.Item2;
        foreach ( var pixel in testPixels )
        {
          geoTiff.PixelToGeo( pixel, out Point2d geo, positionInPixel );
          Assert.AreEqual( geoTiff.Extent.TopLeft + Point2d.Create( geoTiff.Resolution.X * ( pixel.X + subPixelPos.X ), -geoTiff.Resolution.Y * ( pixel.Y + subPixelPos.Y ) )
            , geo
            , $" with pixel: {pixel} and sub pixel position: {positionInPixel}" );
        }
      }
    }

    [TestMethod]
    public void TestSample()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage );

      var samplePosition = geoTiff.Extent.BottomLeft + Point2d.Create( geoTiff.Resolution.X * ( 0.5 * 3.0 ), geoTiff.Resolution.Y * ( 0.5 * 3.0 ) );
      var samplePointRightNeighbour = Point2d.Create( samplePosition.X + geoTiff.Resolution.X, samplePosition.Y );
      var samplePointTopRightNeighbour = Point2d.Create( samplePosition.X + geoTiff.Resolution.X, samplePosition.Y + geoTiff.Resolution.Y );
      var samplePointLeftNeighbour = Point2d.Create( samplePosition.X - geoTiff.Resolution.X, samplePosition.Y );
      var samplePointBottomNeighbour = Point2d.Create( samplePosition.X, samplePosition.Y - geoTiff.Resolution.Y );
      var samplePointTopNeighbour = Point2d.Create( samplePosition.X, samplePosition.Y + geoTiff.Resolution.Y );

      Assert.IsTrue( geoTiff.Extent.Contains( samplePosition ) );
      Assert.IsTrue( geoTiff.Extent.Contains( samplePointRightNeighbour ) );
      Assert.IsTrue( geoTiff.Extent.Contains( samplePointTopRightNeighbour ) );
      Assert.IsTrue( geoTiff.Extent.Contains( samplePointLeftNeighbour ) );
      Assert.IsTrue( geoTiff.Extent.Contains( samplePointBottomNeighbour ) );
      Assert.IsTrue( geoTiff.Extent.Contains( samplePointTopNeighbour ) );

      Assert.AreEqual( 791.0, geoTiff.Sample( samplePosition ) );
      Assert.AreEqual( 782.0, geoTiff.Sample( samplePointRightNeighbour ) );
      Assert.AreEqual( 782.0, geoTiff.Sample( samplePointTopRightNeighbour ) );
      Assert.AreEqual( 797.0, geoTiff.Sample( samplePointLeftNeighbour ) );
      Assert.AreEqual( 785.0, geoTiff.Sample( samplePointBottomNeighbour ) );
      Assert.AreEqual( 785.0, geoTiff.Sample( samplePointTopNeighbour ) );
    }
  }
}
