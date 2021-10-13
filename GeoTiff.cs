using System;
using System.Collections.Generic;
using OSGeo.GDAL;

namespace GeoTiffReaderTest
{
  public class GeoTiff
  {
    Point2i mSize;
    bool mNorthUp;
    Extent mExtent;
    Point2d mResolution;

    private double[] mGeoTransform = new double[6];
    private double[] mInvGeoTransform = new double[6];
    private float[] mPixels = null;

    public enum PixelPosition
    {
      BottomLeft = 0
      , BottomRight = 1
      , Center = 2
      , TopLeft = 3
      , TopRight = 4
    }

    public GeoTiff( string path )
    {
      Create( new List<string>() { path }, Point2i.Create( 1, 1 ) );
    }

    // all tile images must have the same resolution, width and length
    public GeoTiff( List<string> tileImagePaths, Point2i tileCount )
    {
      Create( tileImagePaths, tileCount );
    }

    public GeoTiff( GeoTiff other, Extent subRegion )
    {
      Create( other, subRegion );
    }

    public void GetMinMax( out float min, out float max )
    {
      GetMinMax( out min, out max, out _, out _ );
    }

    public void GetMinMax( out float min, out float max, out Point2i minPixelPos, out Point2i maxPixelPos )
    {
      Utils.GetMinMax( mPixels, Size, out min, out max, out minPixelPos, out maxPixelPos );
    }

    public void Normalize()
    {
      GetMinMax( out float min, out float max );
      for ( var pixelPos = Point2i.Create(); pixelPos.Y < Size.Y; ++pixelPos.Y )
      {
        for ( pixelPos.X = 0; pixelPos.X < Size.X; ++pixelPos.X )
        {
          var height = Pixel( pixelPos );
          var heightNormalized = (float)Utils.RangeMap( min, max, 0.0, 1.0, height );
          mPixels[pixelPos.X + pixelPos.Y * Size.X ] = heightNormalized;
        }
      }
    }

    public void Write( string path )
    {
      // reverse Y axis back to GDal form
      var gdalM = new double[6] { mGeoTransform[0], mGeoTransform[1]
                                  , mGeoTransform[2], mGeoTransform[3] + mGeoTransform[5] * Size.Y 
                                  , mGeoTransform[4], -mGeoTransform[5] };
      Write( path, mPixels, mSize.X, mSize.Y, gdalM );
    }

    public static void Write( string path, float[] pixels, int w, int h, double[] m )
    {
      var driver = Gdal.GetDriverByName( "GTiff" );
      var dataset = driver.Create( path, w, h, 1, DataType.GDT_Float32, null );
      dataset.SetGeoTransform( m );
      dataset.WriteRaster( 0, 0, w, h, pixels, w, h, 1, null, 0, 0, 0 );
      dataset.FlushCache();
    }

    public float Sample( Point2d geo )
    {
      GeoToPixel( geo, out Point2i pixel );
      return Pixel( pixel );
    }

    public float Pixel( Point2i pixel )
    {
      return mPixels[pixel.X + pixel.Y * mSize.X];
    }

    public void PixelToGeo( Point2i pixel, out Point2d geo, PixelPosition pixelPosition = PixelPosition.Center )
    {
      var x = mGeoTransform[0] + pixel.X * Resolution.X + pixel.Y * mGeoTransform[2];
      var y = mGeoTransform[3] + pixel.X * mGeoTransform[4] - pixel.Y * Resolution.Y;
      geo = Point2d.Create( x, y + ( Size.Y - 1 ) * Resolution.Y ); // invert y

      // position the geo position inside the pixel
      switch ( pixelPosition )
      {
        case PixelPosition.Center:
        {
          geo.X += Resolution.X * 0.5;
          geo.Y += Resolution.Y * 0.5;
          break;
        }
        case PixelPosition.TopRight:
        {
          geo.X += Resolution.X;
          geo.Y += Resolution.Y;
          break;
        }
        case PixelPosition.BottomRight:
        {
          geo.X += Resolution.X;
          break;
        }
        case PixelPosition.TopLeft:
        {
          geo.Y += Resolution.Y;
          break;
        }
        default:
          // PixelPosition.BottomLeft
          break;
      }
    }

    public void GeoToPixel( Point2d geo, out Point2i pixel )
    {
      var fx = mInvGeoTransform[0] + geo.X * mInvGeoTransform[1] + geo.Y * mInvGeoTransform[2];
      var fy = mInvGeoTransform[3] + geo.X * mInvGeoTransform[4] + geo.Y * mInvGeoTransform[5];
      var x = (int)Math.Floor( fx );
      var y = (int)Math.Floor( fy );
      pixel = Point2i.Create( x, ( Size.Y - 1 ) - y ); // invert y
    }

    public Point2i Size
    {
      get { return mSize; }
      private set { mSize = value; }
    }

    public Extent Extent
    {
      get { return mExtent; }
      private set { mExtent = value; }
    }

    public Point2d Resolution
    {
      get { return mResolution; }
      private set { mResolution = value; }
    }

    public bool NorthUp
    {
      get { return mNorthUp; }
      private set { mNorthUp = value; }
    }

    void Create( List<string> tileImagePaths, Point2i tileCount )
    {
      if ( tileImagePaths.Count != tileCount.X * tileCount.Y )
      {
        throw new ArgumentException( "invalid tile count, cannot create GeoTiff" );
      }

      // collect input datasets and find the top left tile
      var tileDatasets = new List<Dataset>();
      var topLeftTileIndex = -1;
      {
        var tileIdx = Point2i.Create();
        var topLeftTileGeo = Point2d.Create( 1000.0, -1000.0 );
        for ( tileIdx.Y = 0; tileIdx.Y < tileCount.Y; ++tileIdx.Y )
        {
          for ( tileIdx.X = 0; tileIdx.X < tileCount.X; ++tileIdx.X )
          {
            var tileFilePath = tileImagePaths[tileIdx.X + tileIdx.Y * tileCount.X];
            var tileDataset = Gdal.Open( tileFilePath, Access.GA_ReadOnly );
            tileDatasets.Add( tileDataset );

            double[] tileGeoTransform = new double[6];
            tileDataset.GetGeoTransform( tileGeoTransform );
            if ( tileGeoTransform[0] < topLeftTileGeo.X || ( Utils.Equal( tileGeoTransform[0], topLeftTileGeo.X, Utils.Epsilon ) && tileGeoTransform[3] > topLeftTileGeo.Y ) )
            {
              topLeftTileGeo.X = tileGeoTransform[0];
              topLeftTileGeo.Y = tileGeoTransform[3];
              topLeftTileIndex = tileIdx.X + tileIdx.Y * tileCount.X;
            }
          }
        }
      }
      if ( topLeftTileIndex < 0 || topLeftTileIndex >= tileImagePaths.Count )
      {
        throw new IndexOutOfRangeException( "top left tile index is invalid" );
      }

      // calculate merged image size
      {
        mSize = Point2i.Create();
        for ( var idx = 0; idx < tileCount.X; ++idx )
        {
          var tileDataset = tileDatasets[idx];
          mSize.X += tileDataset.RasterXSize;
        }
        for ( var idx = 0; idx < tileCount.Y; ++idx )
        {
          var tileDataset = tileDatasets[idx * tileCount.X];
          mSize.Y += tileDataset.RasterYSize;
        }
      }

      // first image is the origin, resolution is the same for all tiles
      {
        double[] tileGeoTransform = new double[6];
        tileDatasets[topLeftTileIndex].GetGeoTransform( tileGeoTransform );
        mGeoTransform[0] = tileGeoTransform[0];
        mGeoTransform[1] = tileGeoTransform[1];
        mGeoTransform[3] = tileGeoTransform[3];
        mGeoTransform[5] = tileGeoTransform[5];
        NorthUp = mGeoTransform[5] < 0;
      }

      // change origin from TopLeft to BottomLeft
      {
        mGeoTransform[3] = mGeoTransform[3] + mGeoTransform[5] * mSize.Y;
        mGeoTransform[5] = Math.Abs( mGeoTransform[5] );
      }
      Gdal.InvGeoTransform( mGeoTransform, mInvGeoTransform );

      Resolution = Point2d.Create( mGeoTransform[1], mGeoTransform[5] );
      Extent = Extent.Create( Point2d.Create( mGeoTransform[0], mGeoTransform[3] )
                              , Point2d.Create( mGeoTransform[0] + ( mSize.X * Resolution.X )
                                                , mGeoTransform[3] + ( mSize.Y * Resolution.Y ) ) );

      mPixels = new float[mSize.X * mSize.Y];

      var tileWidth = tileDatasets[0].RasterXSize;
      var tileHeight = tileDatasets[0].RasterYSize;
      var tilePixels = new float[tileWidth * tileHeight];
      foreach ( var tileDataset in tileDatasets )
      {
        tileDataset.GetRasterBand( 1 ).ReadRaster( 0, 0, tileDataset.RasterXSize, tileDataset.RasterYSize
                                                    , tilePixels
                                                    , tileWidth, tileHeight
                                                    , 0, 0 );
        double[] tileGeoTransform = new double[6];
        tileDataset.GetGeoTransform( tileGeoTransform );

        var geoOrigin = Point2d.Create( tileGeoTransform[0], tileGeoTransform[3] );
        geoOrigin = geoOrigin + Point2d.Create( tileGeoTransform[1] / 2.0, tileGeoTransform[5] / 2.0 ); // iTODO verify move to the nearest pixel center
        GeoToPixel( geoOrigin, out Point2i pixelOrigin );

        for ( int y = 0; y < tileDataset.RasterYSize; ++y )
        {
          for ( int x = 0; x < tileDataset.RasterXSize; ++x )
          {
            var pixelIdx = ( pixelOrigin.X + x ) + ( pixelOrigin.Y + y ) * mSize.X;
            var tilePixelIdx = x + y * tileDataset.RasterXSize;
            mPixels[pixelIdx] = tilePixels[tilePixelIdx];
          }
        }
      }
    }

    void Create( GeoTiff other, Extent subRegion )
    {
      // get region in pixel space ( as the region's corners are at the pixels outter boundaries, move the geo positions half pixel invards
      other.GeoToPixel( subRegion.TopLeft + Point2d.Create( other.Resolution.X * 0.5, -other.Resolution.Y * 0.5 ), out Point2i pixelTopLeft );
      other.GeoToPixel( subRegion.BottomRight + Point2d.Create( -other.Resolution.X * 0.5, other.Resolution.Y * 0.5 ), out Point2i pixelBottomRight );

      mSize = Point2i.Create( pixelBottomRight.X - pixelTopLeft.X, pixelBottomRight.Y - pixelTopLeft.Y );
      mPixels = new float[mSize.X * mSize.Y];
      mExtent = subRegion;

      mGeoTransform[0] = mExtent.TopLeft.X;
      mGeoTransform[3] = mExtent.TopLeft.Y;
      mGeoTransform[1] = mExtent.Size.X / (double)mSize.X;
      mGeoTransform[5] = -mExtent.Size.Y / (double)mSize.Y;

      // change origin from TopLeft to BottomLeft
      {
        mGeoTransform[3] = mGeoTransform[3] + mGeoTransform[5] * mSize.Y;
        mGeoTransform[5] = Math.Abs( mGeoTransform[5] );
      }

      Gdal.InvGeoTransform( mGeoTransform, mInvGeoTransform );
      Resolution = Point2d.Create( mGeoTransform[1], mGeoTransform[5] );

      for ( var pixel = Point2i.Create(); pixel.Y < mSize.Y; ++pixel.Y )
      {
        for ( pixel.X = 0; pixel.X < mSize.X; ++pixel.X )
        {
          var h = other.Pixel( pixelTopLeft + pixel );
          mPixels[pixel.X + pixel.Y * mSize.X] = h;
        }
      }
    }
  }
}
