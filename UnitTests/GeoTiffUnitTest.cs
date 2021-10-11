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
    public void TestSingleFileCreation()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage  );
      
      //geoTiff.Write( "e:\\testiViaSingleFile.tif" );

      Assert.AreEqual( Point2d.Create( -114, 37 ), geoTiff.Extent.TopRight );
      Assert.AreEqual( Point2d.Create( -115, 37 ), geoTiff.Extent.TopLeft  );
      Assert.AreEqual( Point2d.Create( -115, 36 ), geoTiff.Extent.BottomLeft );
      Assert.AreEqual( Point2d.Create( -114, 36 ), geoTiff.Extent.BottomRight );


      var width = 3600 * 1;
      var height = 3600 * 1;
      Assert.AreEqual( Point2i.Create( width, height ), geoTiff.Size );
      Assert.IsTrue( geoTiff.NorthUp );
      Assert.AreEqual( Point2d.Create( 1.0 / width, 1.0 / height ), geoTiff.Resolution );

      Assert.AreEqual( 804.0f, geoTiff.Pixel( Point2i.Create( 0, 0 ) ) );
      Assert.AreEqual( 665.0f, geoTiff.Pixel( Point2i.Create( 3599, 0 ) ) );
      Assert.AreEqual( 795.0f, geoTiff.Pixel( Point2i.Create( 0, 3599 ) ) );
      Assert.AreEqual( 915.0f, geoTiff.Pixel( Point2i.Create( 3599, 3599 ) ) );

      Assert.AreEqual( Point2d.Create( 1.0, 1.0 ), geoTiff.Extent.Size );
      Assert.AreEqual( Point2d.Create( 88903.2896148812, 111319.490793274 ), geoTiff.Extent.SizeInMeters );
    }

    [TestMethod]
    public void Test2x1TileCreation()
    {
      var tileCount = Point2i.Create( 2, 1 );
      List<string> tileFileList = new List<string>();
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N036W115_DSM.tif" );
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N036W114_DSM.tif" );

      foreach ( var tile in tileFileList )
      {
        Assert.IsTrue( File.Exists( tile ) );
      }

      GeoTiff geoTiff = new GeoTiff( tileFileList, tileCount );
      //geoTiff.Write( "e:\\ViaTileFileList_tiles2x1.tif" );

      Assert.AreEqual( Point2d.Create( -115, 36 ), geoTiff.Extent.BottomLeft );
      Assert.AreEqual( Point2d.Create( -113, 37 ), geoTiff.Extent.TopRight );

      Assert.AreEqual( Point2d.Create( -115, 37 ), geoTiff.Extent.TopLeft );
      Assert.AreEqual( Point2d.Create( -113, 36 ), geoTiff.Extent.BottomRight );

      var width = 3600 * 2;
      var height = 3600 * 1;
      Assert.AreEqual( Point2i.Create( width, height ), geoTiff.Size );
      Assert.IsTrue( geoTiff.NorthUp );
      Assert.AreEqual( Point2d.Create( 2.0 / width, 1.0 / height ), geoTiff.Resolution );

      Assert.AreEqual( 804.0f, geoTiff.Pixel( Point2i.Create( 0, 0 ) ) );
      Assert.AreEqual( 665.0f, geoTiff.Pixel( Point2i.Create( 3599, 0 ) ) );
      Assert.AreEqual( 915.0f, geoTiff.Pixel( Point2i.Create( 3599, 3599 ) ) );

      Assert.AreEqual( 669.0f, geoTiff.Pixel( Point2i.Create( 3600 + 1, 0 ) ) );
      Assert.AreEqual( 668.0f, geoTiff.Pixel( Point2i.Create( 3600 + 2, 0 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 3, 0 ) ) );
      Assert.AreEqual( 669.0f, geoTiff.Pixel( Point2i.Create( 3600 + 4, 0 ) ) );

      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 0, 3 ) ) );
      Assert.AreEqual( 667.0f, geoTiff.Pixel( Point2i.Create( 3600 + 1, 3 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 6, 3 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 7, 3 ) ) );
      Assert.AreEqual( 668.0f, geoTiff.Pixel( Point2i.Create( 3600 + 8, 3 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 6, 4 ) ) );
      Assert.AreEqual( 668.0f, geoTiff.Pixel( Point2i.Create( 3600 + 6, 5 ) ) );

      var samplePositions = new List<Tuple<Point2d, float>>() 
      {
        Tuple.Create( Point2d.Create( -113.2257057, 36.3609809 ), 2008.0f ) 
        , Tuple.Create( Point2d.Create( -113.343170, 36.051512 ), 788.0f )
        , Tuple.Create( Point2d.Create( -114.114267, 36.605136 ), 2207.0f )
        , Tuple.Create( Point2d.Create( -114.7056917, 36.2298569 ), 1023.0f )
        , Tuple.Create( Point2d.Create( -114.9999999788, 36.0000000077 ), 795.0f )
        , Tuple.Create( Point2d.Create( -114.9999788, 36.9999806 ), 804.0f )
        , Tuple.Create( Point2d.Create( -113.00001929, 36.99998337 ), 1539.0f )
        , Tuple.Create( Point2d.Create( -113.0000295, 36.0000292 ), 1949.0f )
      };

      foreach ( var samplePos in samplePositions )
      {
        geoTiff.GeoToPixel( samplePos.Item1, out Point2i samplePixel );
        Assert.AreEqual( samplePos.Item2, geoTiff.Pixel( samplePixel ) );
      }

      Assert.AreEqual( Point2d.Create( 2.0, 1.0 ), geoTiff.Extent.Size );
      //Assert.AreEqual( Point2d.Create( 88903.2896148812 * 2.0, 111319.490793274 ), geoTiff.Extent.SizeInMeters );
    }

    [TestMethod]
    public void Test2x2TileCreation()
    {
      var tileCount = Point2i.Create( 2, 2 );
      List<string> tileFileList = new List<string>();
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N035W114_DSM.tif" );
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N035W115_DSM.tif" );
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N036W114_DSM.tif" );
      tileFileList.Add( @"..\..\testData\ALPSMLC30_N036W115_DSM.tif" );

      foreach ( var tile in tileFileList )
      {
        Assert.IsTrue( File.Exists( tile ) );
      }

      GeoTiff geoTiff = new GeoTiff( tileFileList, tileCount );
      //geoTiff.Write( "e:\\ViaTileFileList_tiles2x2.tif" );

      Assert.AreEqual( Point2d.Create( -115, 35 ), geoTiff.Extent.BottomLeft );
      Assert.AreEqual( Point2d.Create( -113, 37 ), geoTiff.Extent.TopRight );

      Assert.AreEqual( Point2d.Create( -115, 37 ), geoTiff.Extent.TopLeft );
      Assert.AreEqual( Point2d.Create( -113, 35 ), geoTiff.Extent.BottomRight );

      var width = 3600 * 2;
      var height = 3600 * 2;
      Assert.AreEqual( Point2i.Create( width, height ), geoTiff.Size );
      Assert.IsTrue( geoTiff.NorthUp );
      Assert.AreEqual( Point2d.Create( 2.0 / width, 2.0 / height ), geoTiff.Resolution );

      Assert.AreEqual( 804.0f, geoTiff.Pixel( Point2i.Create( 0, 0 ) ) );
      Assert.AreEqual( 665.0f, geoTiff.Pixel( Point2i.Create( 3599, 0 ) ) );
      Assert.AreEqual( 915.0f, geoTiff.Pixel( Point2i.Create( 3599, 3599 ) ) );

      Assert.AreEqual( 669.0f, geoTiff.Pixel( Point2i.Create( 3600 + 1, 0 ) ) );
      Assert.AreEqual( 668.0f, geoTiff.Pixel( Point2i.Create( 3600 + 2, 0 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 3, 0 ) ) );
      Assert.AreEqual( 669.0f, geoTiff.Pixel( Point2i.Create( 3600 + 4, 0 ) ) );

      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 0, 3 ) ) );
      Assert.AreEqual( 667.0f, geoTiff.Pixel( Point2i.Create( 3600 + 1, 3 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 6, 3 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 7, 3 ) ) );
      Assert.AreEqual( 668.0f, geoTiff.Pixel( Point2i.Create( 3600 + 8, 3 ) ) );
      Assert.AreEqual( 670.0f, geoTiff.Pixel( Point2i.Create( 3600 + 6, 4 ) ) );
      Assert.AreEqual( 668.0f, geoTiff.Pixel( Point2i.Create( 3600 + 6, 5 ) ) );

      var samplePositions = new List<Tuple<Point2d, float>>()
      {
        Tuple.Create( Point2d.Create( -113.2257057, 36.3609809 ), 2008.0f )
        , Tuple.Create( Point2d.Create( -113.343170, 36.051512 ), 788.0f )
        , Tuple.Create( Point2d.Create( -114.114267, 36.605136 ), 2207.0f )
        , Tuple.Create( Point2d.Create( -114.7056917, 36.2298569 ), 1023.0f )
        , Tuple.Create( Point2d.Create( -114.9999999788, 36.0000000077 ), 795.0f )
        , Tuple.Create( Point2d.Create( -114.9999788, 36.9999806 ), 804.0f )
        , Tuple.Create( Point2d.Create( -113.00001929, 36.99998337 ), 1539.0f )
        , Tuple.Create( Point2d.Create( -113.0000295, 36.0000292 ), 1949.0f )
        , Tuple.Create( Point2d.Create( -114.592071,36.570949 ), 739.0f )
        , Tuple.Create( Point2d.Create( -113.4440367,36.4628770 ), 1818.0f )
        , Tuple.Create( Point2d.Create( -114.635142,35.228430 ), 503.0f )
        , Tuple.Create( Point2d.Create( -113.2454424,35.1032157 ), 1536.0f )
      };

      foreach ( var samplePos in samplePositions )
      {
        geoTiff.GeoToPixel( samplePos.Item1, out Point2i samplePixel );
        Assert.AreEqual( samplePos.Item2, geoTiff.Pixel( samplePixel ) );
      }

      Assert.AreEqual( Point2d.Create( 2.0, 2.0 ), geoTiff.Extent.Size );
      //Assert.AreEqual( Point2d.Create( 88903.2896148812 * 2.0, 111319.490793274 ), geoTiff.Extent.SizeInMeters );
    }

    [TestMethod]
    public void TestGeoToPixel()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage );

      {
        geoTiff.GeoToPixel( geoTiff.Extent.TopLeft + Point2d.Create( geoTiff.Resolution.X * 0.5, -geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( Point2i.Create( 0, 0 ), pixel );
      }
      {
        geoTiff.GeoToPixel( geoTiff.Extent.TopRight + Point2d.Create( -geoTiff.Resolution.X * 0.5, -geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( Point2i.Create( 3599, 0 ), pixel );
      }
      {
        geoTiff.GeoToPixel( geoTiff.Extent.BottomRight + Point2d.Create( -geoTiff.Resolution.X * 0.5, +geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( Point2i.Create( 3599, 3599 ), pixel );
      }
      {
        geoTiff.GeoToPixel( geoTiff.Extent.BottomLeft + Point2d.Create( +geoTiff.Resolution.X * 0.5, +geoTiff.Resolution.Y * 0.5 ), out Point2i pixel );
        Assert.AreEqual( Point2i.Create( 0, 3599 ), pixel );
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

      Assert.AreEqual( 791.0f, geoTiff.Sample( samplePosition ) );
      Assert.AreEqual( 782.0f, geoTiff.Sample( samplePointRightNeighbour ) );
      Assert.AreEqual( 782.0f, geoTiff.Sample( samplePointTopRightNeighbour ) );
      Assert.AreEqual( 797.0f, geoTiff.Sample( samplePointLeftNeighbour ) );
      Assert.AreEqual( 785.0f, geoTiff.Sample( samplePointBottomNeighbour ) );
      Assert.AreEqual( 785.0f, geoTiff.Sample( samplePointTopNeighbour ) );
    }

    [TestMethod]
    public void TestMinMax()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage );

      var minRef = float.MaxValue;
      var maxRef = float.MinValue;
      var minPixelPosRef = Point2i.Create();
      var maxPixelPosRef = Point2i.Create();
      {
        for ( var y = 0; y < geoTiff.Size.Y; ++y )
        {
          for ( var x = 0; x < geoTiff.Size.X; ++x )
          {
            var h = geoTiff.Pixel( Point2i.Create( x, y ) );
            if ( h < minRef )
            {
              minRef = h;
              minPixelPosRef.X = x;
              minPixelPosRef.Y = y;
            }
            if ( h > maxRef )
            {
              maxRef = h;
              maxPixelPosRef.X = x;
              maxPixelPosRef.Y = y;
            }
          }
        }

        {
          geoTiff.GetMinMax( out float min, out float max );
          Assert.AreEqual( minRef, min );
          Assert.AreEqual( maxRef, max );
        }
        {
          geoTiff.GetMinMax( out float min, out float max, out Point2i minPixelPos, out Point2i maxPixelPos );
          Assert.AreEqual( minPixelPos, minPixelPosRef );
          Assert.AreEqual( maxPixelPos, maxPixelPosRef );

          geoTiff.PixelToGeo( minPixelPos, out Point2d geoMinPixel );
          geoTiff.PixelToGeo( maxPixelPos, out Point2d geoMaxPixel );
          Assert.AreEqual( minRef, min, geoMinPixel.AsWkt() );
          Assert.AreEqual( maxRef, max, geoMaxPixel.AsWkt() );

          Assert.AreEqual( minPixelPosRef, minPixelPos );
          Assert.AreEqual( maxPixelPosRef, maxPixelPos );
        }
      }
    }

    [TestMethod]
    public void TestNormalize()
    {
      var testImage = @"..\..\testData\ALPSMLC30_N036W115_DSM.tif";

      GeoTiff geoTiff = new GeoTiff( testImage );
      {
        geoTiff.GetMinMax( out float min, out float max );

        var originalHeight = geoTiff.Pixel( Point2i.Create( 0, 0 ) );
        geoTiff.Normalize();
        var originHeightNorm = geoTiff.Pixel( Point2i.Create( 0, 0 ) );

        var testValue = min + originHeightNorm * ( max - min );
        Assert.AreEqual( originalHeight, testValue );

        for ( var px = Point2i.Create(); px.Y < geoTiff.Size.Y; ++px.Y )
        {
          for ( px.X = 0; px.X < geoTiff.Size.X; ++px.X )
          {
            var h = geoTiff.Pixel( px );
            Assert.IsTrue( 0.0 <= h && 1.0 >= h, $"{px} {h}" );
          }
        }
      }

      //geoTiff.Write( @"e:\notmalized.tif" );
      {
        geoTiff.GetMinMax( out float min, out float max, out Point2i minPos, out Point2i maxPos );
        geoTiff.PixelToGeo( minPos, out Point2d minGeo );
        geoTiff.PixelToGeo( maxPos, out Point2d maxGeo );
        //Utils.CreateFile( @"e:\min.csv", minGeo.AsWkt() );
        //Utils.CreateFile( @"e:\max.csv", maxGeo.AsWkt() );
      }
    }
  }
}
