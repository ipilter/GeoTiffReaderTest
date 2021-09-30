using System;
using OSGeo.GDAL;

namespace GeoTiffReaderTest
{
  class GeoTiff
  {
    Point2i mSize;
    bool mInverseY;
    Extent mExtent;
    Point2d mResolution;

    private double[] mGeoTransform = new double[6];
    private double[] mInvGeoTransform = new double[6];
    private double[] mPixels = null;

    public enum PixelPosition
    {
      TopLeft = 0
      , Center = 1
      , BottomRight = 2
    }

    public GeoTiff( string path )
    {
      var dataset = Gdal.Open( path, Access.GA_ReadOnly );
      Size = Point2i.Create( dataset.RasterXSize, dataset.RasterYSize );

      dataset.GetGeoTransform( mGeoTransform );
      Gdal.InvGeoTransform( mGeoTransform, mInvGeoTransform );

      InverseY = mGeoTransform[5] < 0;  // TODO follow this rabbit
      //mGeoTransform[5] = Math.Abs( mGeoTransform[5] );
      Resolution = Point2d.Create( mGeoTransform[1], Math.Abs( mGeoTransform[5] ) );
      //Resolution = Point2d.Create( mGeoTransform[1], mGeoTransform[5] );

      mPixels = new double[mSize.X * mSize.Y];
      dataset.GetRasterBand( 1 ).ReadRaster( 0, 0, mSize.X, mSize.Y, mPixels, mSize.X, mSize.Y, 0, 0 );

      Extent = new Extent( Point2d.Create( mGeoTransform[0], mGeoTransform[3] )
                           , Point2d.Create( mGeoTransform[0] + ( dataset.RasterXSize * mGeoTransform[1] )
                                             , mGeoTransform[3] + ( dataset.RasterYSize * mGeoTransform[5] ) ) );
    }

    public double Sample( Point2d geo )
    {
      GeoToPixel( geo, out Point2i pixel );
      return Pixel( pixel );
    }

    public double Pixel( Point2i pixel )
    {
      return mPixels[pixel.X + pixel.Y * mSize.X];
    }

    public void PixelToGeo( Point2i pixel, out Point2d geo, PixelPosition pixelPosition = PixelPosition.TopLeft )
    {
      geo = Point2d.Create( mGeoTransform[0] + pixel.X * mGeoTransform[1] + pixel.Y * mGeoTransform[2],
                            mGeoTransform[3] + pixel.X * mGeoTransform[4] + pixel.Y * mGeoTransform[5] );

      // position the geo position inside the pixel
      switch ( pixelPosition )
      {
        case PixelPosition.TopLeft:
        {
          break;
        }
        case PixelPosition.Center:
        {
          geo.X += mGeoTransform[1] * 0.5;
          geo.Y += mGeoTransform[5] * 0.5;
          break;
        }
        case PixelPosition.BottomRight:
        {
          geo.X += mGeoTransform[1];
          geo.Y += mGeoTransform[5];
          break;
        }
        default:
          // top left
          break;
      }
    }

    public void GeoToPixel( Point2d geo, out Point2i pixel )
    {
      pixel = Point2i.Create( (int)Math.Floor( mInvGeoTransform[0] + geo.X * mInvGeoTransform[1] + geo.Y * mInvGeoTransform[2] )
                              , (int)Math.Floor( mInvGeoTransform[3] + geo.X * mInvGeoTransform[4] + geo.Y * mInvGeoTransform[5] ) );
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

    public bool InverseY
    {
      get { return mInverseY; }
      private set { mInverseY = value; }
    }
  }
}
