using System;

namespace GeoTiffReaderTest
{
  class Program
  {
    static void Main( string[] args )
    {
      try
      {
        Application app = new Application( args );
        app.run();
      }
      catch (Exception e)
      {
        printException( e );
      }
    }
    static void printException( Exception e )
    {
      var except = e;

      int identSize = 2;
      char identChar = ' ';

      string prefix = $"Runtime error!";
      int indentCount = 0;
      Console.WriteLine( prefix );
      while ( except != null )
      {
        Console.WriteLine( $"{ new string( identChar, indentCount )}{except.Message}" );
        except = except.InnerException;
        indentCount += identSize;
      }
    }
  }
}
