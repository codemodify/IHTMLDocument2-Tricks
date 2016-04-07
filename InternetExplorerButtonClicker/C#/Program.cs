using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;

namespace InternetExplorerButtonClicker
{
    static class Program
    {
        public struct Params
        {
            public String _webAppToLookFor     ;
            public String _webAppPhoneField    ;
            public String _webAppCallButton    ;
            public String _siteToLoad          ;
        };

        public static Params _params;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            _params._webAppToLookFor    = ConfigurationSettings.AppSettings.Get( "webAppToLookFor" );
            _params._webAppPhoneField   = ConfigurationSettings.AppSettings.Get( "webAppPhoneField" );
            _params._webAppCallButton   = ConfigurationSettings.AppSettings.Get( "webAppCallButton" );
            _params._siteToLoad         = ConfigurationSettings.AppSettings.Get( "siteToLoad" );
            
            Application.Run( new Form1() );
        }
    }
}
