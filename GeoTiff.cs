using System;
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
    private double[] mPixels = null;

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
      NorthUp = mGeoTransform[5] < 0;  // TODO follow this rabbit

      // change origin
      {
        mGeoTransform[3] = mGeoTransform[3] + mGeoTransform[5] * Size.Y;
        mGeoTransform[5] = -mGeoTransform[5];
      }
      Gdal.InvGeoTransform( mGeoTransform, mInvGeoTransform );

      Resolution = Point2d.Create( mGeoTransform[1], mGeoTransform[5] );
      Extent = new Extent( Point2d.Create( mGeoTransform[0], mGeoTransform[3] )
                           , Point2d.Create( mGeoTransform[0] + ( dataset.RasterXSize * Resolution.X )
                                             , mGeoTransform[3] + ( dataset.RasterYSize * Resolution.Y ) ) );

      mPixels = new double[mSize.X * mSize.Y];
      dataset.GetRasterBand( 1 ).ReadRaster( 0, 0, mSize.X, mSize.Y, mPixels, mSize.X, mSize.Y, 0, 0 );
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
      var x = (int)Math.Floor( mInvGeoTransform[0] + geo.X * mInvGeoTransform[1] + geo.Y * mInvGeoTransform[2] );
      var y = (int)Math.Floor( mInvGeoTransform[3] + geo.X * mInvGeoTransform[4] + geo.Y * mInvGeoTransform[5] );
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
