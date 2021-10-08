using System;
using System.Collections.Generic;

namespace GeoTiffReaderTest
{
  class GeoTiffFactory
  {
    public static GeoTiff CreateFromTiles( List<string> tileImagePaths )
    {
      if ( tileImagePaths.Count == 0 )
        throw new ArgumentException( "empty tile image list, cannot create GeoTiff" );
      return new GeoTiff( tileImagePaths [0]);
    }
  }
}
