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
      var dataset = Gdal.Open( path, Access.GA_ReadOnly );
      Size = Point2i.Create( dataset.RasterXSize, dataset.RasterYSize );

      dataset.GetGeoTransform( mGeoTransform );
      NorthUp = mGeoTransform[5] < 0;

      // change origin from TopLeft to BottomLeft
      {
        mGeoTransform[3] = mGeoTransform[3] + mGeoTransform[5] * Size.Y;
        mGeoTransform[5] = Math.Abs( mGeoTransform[5] );
      }
      Gdal.InvGeoTransform( mGeoTransform, mInvGeoTransform );

      Resolution = Point2d.Create( mGeoTransform[1], mGeoTransform[5] );
      Extent = Extent.Create( Point2d.Create( mGeoTransform[0], mGeoTransform[3] )
                              , Point2d.Create( mGeoTransform[0] + ( mSize.X * Resolution.X )
                                             , mGeoTransform[3] + ( mSize.Y * Resolution.Y ) ) );

      mPixels = new float[mSize.X * mSize.Y];
      dataset.GetRasterBand( 1 ).ReadRaster( 0, 0, mSize.X, mSize.Y, mPixels, mSize.X, mSize.Y, 0, 0 );
    }

    public GeoTiff( List<string> tileImagePaths, Point2i tileCount )
    {
      // iTODO: expectations!
      //  - first image is the origin
      //  - all tile images has same resolution, width and lenght
      if ( tileImagePaths.Count != tileCount.X * tileCount.Y )
      {
        throw new ArgumentException( "invalid tile count, cannot create GeoTiff" );
      }

      var tileDatasets = new List<Dataset>();

      Size = Point2i.Create();
      var tileIdx = Point2i.Create();
      for ( ; tileIdx.Y < tileCount.Y; ++tileIdx.Y )
      {
        for ( ; tileIdx.X < tileCount.X; ++tileIdx.X )
        {
          var tileFilePath = tileImagePaths[tileIdx.X + tileIdx.Y * tileCount.Y];
          var tileDataset = Gdal.Open( tileFilePath, Access.GA_ReadOnly );
          tileDatasets.Add( tileDataset );
          Size.X += tileDatasets[tileIdx.X].RasterXSize;

        }
        Size.Y += tileDatasets[0 + tileIdx.Y * tileCount.Y].RasterYSize;
      }

      // first image is the origin, resolution is the same for all tiles
      double[] tileGeoTransform = new double[6];
      tileDatasets[0].GetGeoTransform( tileGeoTransform );
      mGeoTransform[0] = tileGeoTransform[0];
      mGeoTransform[1] = tileGeoTransform[1];
      mGeoTransform[3] = tileGeoTransform[3];
      mGeoTransform[5] = tileGeoTransform[5];
      NorthUp = mGeoTransform[5] < 0;

      // change origin from TopLeft to BottomLeft
      {
        mGeoTransform[3] = mGeoTransform[3] + mGeoTransform[5] * Size.Y;
        mGeoTransform[5] = Math.Abs( mGeoTransform[5] );
      }
      Gdal.InvGeoTransform( mGeoTransform, mInvGeoTransform );

      Resolution = Point2d.Create( mGeoTransform[1], mGeoTransform[5] );
      Extent = Extent.Create( Point2d.Create( mGeoTransform[0], mGeoTransform[3] )
                              , Point2d.Create( mGeoTransform[0] + ( Size.X * Resolution.X )
                                                , mGeoTransform[3] + ( Size.Y * Resolution.Y ) ) );

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

    public void Normalize()
    {
      // iTODO use min max to scale to 0.0, 1.0
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
      var fx = Math.Floor( mInvGeoTransform[0] + geo.X * mInvGeoTransform[1] + geo.Y * mInvGeoTransform[2] );
      var fy = Math.Floor( mInvGeoTransform[3] + geo.X * mInvGeoTransform[4] + geo.Y * mInvGeoTransform[5] );
      var x = (int)fx;
      var y = (int)fy;
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
  }
}
