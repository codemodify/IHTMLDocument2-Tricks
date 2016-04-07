using System;
using System.Windows.Forms;

using System.Reflection;
using System.IO;
using mshtml;
using System.Configuration;
using System.Threading;
using System.Diagnostics;

namespace Ui.Desktop
{
    public partial class CaUiDesktop : Form
    {
        #region variables

            String _webAppToStart       = String.Empty;
            String _webAppParameters    = String.Empty;
            bool   _webAppLoadProfile   = false;
            bool   _webAppShellExec     = false;

            String _webAppPhoneField    = String.Empty;
            String _webAppCallButton    = String.Empty;
            String _siteToLoad          = String.Empty;

            String _bandeauTop          = String.Empty;
            String _bandeauLeft         = String.Empty;
            String _bandeauWidth        = String.Empty;
            String _bandeauHeight       = String.Empty;

            String _top                 = String.Empty;
            String _left                = String.Empty;
            String _width               = String.Empty;
            String _height              = String.Empty;

            ClipboardHelper         _clipboardHelper    = null;
            static BandauStarter    _bandauStarter      = null;
            ManualResetEvent        _canContinue        = null;
            bool                    _errorOccured       = false;

            public static Logging.ILogger Logger        = Logging.LogManager.GetLogger();

            const String            _messagePrefix      = "cassiopeeRequest";

        #endregion

        public CaUiDesktop()
        {
            InitializeComponent();

            #region Read Settings

            try
            {
                String[] args = Environment.GetCommandLineArgs();
                
                String securityParams = String.Empty;

                if( args.Length > 1 )
                {
                    for( int index=1; index < args.Length; index++ )
                        securityParams += args[ index ] + " ";

                    if( securityParams.Length > 0 )
                        securityParams = securityParams.Remove( securityParams.Length - 1, 1 );
                }

                _webAppToStart = ConfigurationSettings.AppSettings.Get( "webAppToStart" );
                _webAppParameters   = ConfigurationSettings.AppSettings.Get( "webAppParameters" );
                _webAppParameters   = _webAppParameters.Replace( '\'', '\"' );
                _webAppLoadProfile  = Convert.ToBoolean( ConfigurationSettings.AppSettings.Get( "webAppLoadProfile" ) );
                _webAppShellExec    = Convert.ToBoolean( ConfigurationSettings.AppSettings.Get( "webAppShellExec" ) );

                _webAppPhoneField   = ConfigurationSettings.AppSettings.Get( "webAppPhoneField" );
                _webAppCallButton   = ConfigurationSettings.AppSettings.Get( "webAppCallButton" );
                _siteToLoad         = ConfigurationSettings.AppSettings.Get( "siteToLoad" );
                _siteToLoad        += String.Format( "?{0}", securityParams );

                _bandeauTop         = ConfigurationSettings.AppSettings.Get( "bandeauTop" );
                _bandeauLeft        = ConfigurationSettings.AppSettings.Get( "bandeauLeft" );
                _bandeauWidth       = ConfigurationSettings.AppSettings.Get( "bandeauWidth" );
                _bandeauHeight      = ConfigurationSettings.AppSettings.Get( "bandeauHeight" );

                _top                = ConfigurationSettings.AppSettings.Get( "top" );
                _left               = ConfigurationSettings.AppSettings.Get( "left" );
                _width              = ConfigurationSettings.AppSettings.Get( "width" );
                _height             = ConfigurationSettings.AppSettings.Get( "height" );

                _canContinue = new ManualResetEvent( false );

                _bandauStarter = new BandauStarter();
                _bandauStarter.WebAppToStart = _webAppToStart;
                _bandauStarter.WebAppParameters = _webAppParameters;
                _bandauStarter.WebAppLoadProfile = _webAppLoadProfile;
                _bandauStarter.WebAppShellExec = _webAppShellExec;
                _bandauStarter.BandeauClosed += BandeauClosed;
                _bandauStarter.BandeauStartError += BandeauStartError;
                _bandauStarter.BandeauStartSuccess += new EventHandler( BandeauStartSuccess );
            }

            #region exception
            catch( Exception e )
            {
                String errorMessage = String.Format
                (
                    "Configuration file invalid. Details: \n{0}",
                    e.ToString()
                );

                MessageBox.Show( errorMessage );
            }
            #endregion

            #endregion
        }

        void ThisThreadBandeauStartSuccess( object sender, EventArgs e )
        {
            _bandauStarter.Close();

            _canContinue.Set();
        }

        void ThisThreadShowStartError( object sender, EventArgs e )
        {
            _bandauStarter.Close();

            MessageBox.Show( this, "Problème de Démarrage du Bandeau", "", MessageBoxButtons.OK, MessageBoxIcon.Error );

            _canContinue.Set();
        }

        void ThisThreadBandeauClosed( object sender, EventArgs e )
        {
            DialogResult result = MessageBox.Show
            (
                this,
                "Le Bandeau a été fermé , Voulez vous le redémarrer ?",
                "Problème de communication avec le Bandeau",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if( result == DialogResult.Yes )
            {
                _bandauStarter = new BandauStarter();
                _bandauStarter.WebAppToStart = _webAppToStart;
                _bandauStarter.WebAppParameters = _webAppParameters;
                _bandauStarter.WebAppLoadProfile = _webAppLoadProfile;
                _bandauStarter.WebAppShellExec = _webAppShellExec;
                _bandauStarter.BandeauClosed += BandeauClosed;
                _bandauStarter.BandeauStartError += BandeauStartError;
                _bandauStarter.BandeauStartSuccess += new EventHandler( BandeauStartSuccess );
                _bandauStarter.ShowDialog();
            }
            else
                _bandauStarter.MonitorForManualStart();
        }

        void BandeauStartSuccess( object sender, EventArgs e )
        {
            Invoke( new EventHandler( ThisThreadBandeauStartSuccess ), new object[] { sender, e } );
        }

        void BandeauStartError( object sender, EventArgs e )
        {
            Invoke( new EventHandler( ThisThreadShowStartError ), new object[] { sender, e } );
        }

        void BandeauClosed( object sender, EventArgs e )
        {
            Invoke( new EventHandler( ThisThreadBandeauClosed ), new object[] { sender, e } );
        }

        #region clipboardChangedDelegate()

        private void clipboardChangedDelegate( object sender, EventArgs dataObject )
        {
            String clipboardValue = Clipboard.GetText();
            if(
                String.Empty.Equals( clipboardValue )
                ||
                "".Equals( clipboardValue )
                ||
                clipboardValue.StartsWith( _messagePrefix )
            )
                return;

            this.ControlBox = false;

            // handle the command
            const String c_resize   = "resize";
            const String c_center   = "center";
            const String c_call     = "call";
            const String c_quit     = "quit";
            const String c_error    = "error";
            const String c_applySizeSettingsForSecondScreen = "applySizeSettingsForSecondScreen";

            String[] message = clipboardValue.Split( ':' );

            switch( message[ 0 ] )
            {
                #region resize
                case c_resize:
                {
                    String widthAsString    = message[ 1 ];
                    String heightAsString   = message[ 2 ];

                    int width   = System.Convert.ToInt32( widthAsString );
                    int height  = System.Convert.ToInt32( heightAsString );

                    Width  = width;
                    Height = height;
                }
                break; 
                #endregion

                #region center
                case c_center:
                {
                    String widthAsString    = message[ 1 ];
                    String heightAsString   = message[ 2 ];

                    int width   = System.Convert.ToInt32( widthAsString );
                    int height  = System.Convert.ToInt32( heightAsString );

                    Width = width;
                    Height = height;

                    Left = (Screen.PrimaryScreen.Bounds.Width - width) / 2;
                    Top = (Screen.PrimaryScreen.Bounds.Height - height) / 2;
                }
                break;
                #endregion

                case c_call:
                    Helper.Win32.AskHtmlDocumentFromProcess( _bandauStarter.Bandeau );
                    break;

                case c_quit:
                    _errorOccured = true;
                    Close();
                    break;

                case c_error:
                    _errorOccured = true;
                    this.ControlBox = true; // once there is an error then allow the close button
                    break;

                #region applySizeSettingsForSecondScreen
                case c_applySizeSettingsForSecondScreen:
                {
                    int top             = System.Convert.ToInt32( _top );
                    int left            = System.Convert.ToInt32( _left );
                    int width           = System.Convert.ToInt32( _width );
                    int height          = System.Convert.ToInt32( _height );

                    Top = top;
                    Left = left;
                    Width = width;
                    Height = height;

                    // query the Bandeau Window and resize it
                    //Helper.Win32.AskHwndFromProcess( _webAppToStart );
                    Helper.Win32.AskHwndFromProcess( _bandauStarter.Bandeau );
                }
                break; 
                #endregion

                default:
                    break;
            }
        }

        class StateInfo
        {
            public IHTMLDocument2   iHTMLDocument;
            public String           phoneNumber;
        }

        private void HtmlDocumentFound( object sender, EventArgs dataObject )
        {
            IHTMLDocument2 iHTMLDocument = sender as IHTMLDocument2;
            if( null == iHTMLDocument )
                return;

            String  phoneNumber = Clipboard.GetText();
                    phoneNumber = phoneNumber.Split(':')[1];

            //sendMessageToCallCenter( iHTMLDocument, phoneNumber );

            StateInfo   stateInfo = new StateInfo();
                        stateInfo.iHTMLDocument = iHTMLDocument;
                        stateInfo.phoneNumber = phoneNumber;

            ThreadPool.QueueUserWorkItem( ThreadProc, stateInfo );

            Clipboard.Clear();
        }

        #endregion

        #region sendMessageToCallCenter()

        void ThreadProc( Object objectAsStateInfo )
        {
            StateInfo stateInfo = objectAsStateInfo as StateInfo;

            if( null == stateInfo )
                return;

            sendMessageToCallCenter( stateInfo.iHTMLDocument, stateInfo.phoneNumber );
        }

        private void sendMessageToCallCenter( IHTMLDocument2 targetHtmlDocument, String phoneNumber )
        {
            MethodInfo  getElementById = targetHtmlDocument.GetType().GetMethod( "getElementById" );

            object[] param1 = new object[] { _webAppPhoneField };
            IHTMLElement textInput = getElementById.Invoke( targetHtmlDocument, param1 ) as IHTMLElement;

            if( null == textInput ) // wrong window
            {
                Logger.LogError( "sendMessageToCallCenter( " + phoneNumber + " ) - wrong window" );

                return;
            }

            textInput.innerText = phoneNumber.Replace( " ", ""  );

            object[] param2 = new object[] { _webAppCallButton };
            IHTMLElement buttonInput = getElementById.Invoke( targetHtmlDocument, param2 ) as IHTMLElement;

            MethodInfo  mi = buttonInput.GetType().GetMethod( "click" );
                        mi.Invoke( buttonInput, new object[ 0 ] );
        }

        #endregion

        #region HwndFound()
        
        private void HwndFound( int hwnd )
        {
            int bandeauTop       = System.Convert.ToInt32( _bandeauTop );
            int bandeauLeft      = System.Convert.ToInt32( _bandeauLeft );
            int bandeauWidth     = System.Convert.ToInt32( _bandeauWidth );
            int bandeauHeight    = System.Convert.ToInt32( _bandeauHeight );

            Helper.Win32.MoveWindow( hwnd, bandeauLeft, bandeauTop, bandeauWidth, bandeauHeight, 1 );

            //Helper.Win32.HwndFoundEvent -= new Helper.Win32.HwndFoundEventHandler( HwndFound );
        }

        #endregion

        #region CaUiDesktop_Load()

        private void CaUiDesktop_Load( object sender, EventArgs e )
        {
            _bandauStarter.ShowDialog();

            _canContinue.WaitOne();

            if( false == _bandauStarter.IsBandeauRunning() )
            {
                Close();
                return;
            }

            _clipboardHelper = new ClipboardHelper();
            _clipboardHelper.ClipboardChanged = new EventHandler<ClipboardChangedEventArgs>( clipboardChangedDelegate );

            _webBrowser.Url = new Uri( _siteToLoad );
            _webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler( WebBrowserDocumentCompleted );

            Helper.Win32.HtmlDocumentFoundEvent += new Helper.Win32.HtmlDocumentFoundEventHandler( HtmlDocumentFound );
            Helper.Win32.HwndFoundEvent         += new Helper.Win32.HwndFoundEventHandler( HwndFound );
        }

        #endregion

        #region WebBrowserDocumentCompleted()

        void WebBrowserDocumentCompleted( object sender, WebBrowserDocumentCompletedEventArgs e )
        {
            this.Text = _webBrowser.Document.Title;
        }

        #endregion

        protected override void OnClosing( System.ComponentModel.CancelEventArgs e )
        {
            // If there:
            //  - is an error when connecting to the website
            //  - is an error when authenticating
            //  - is an error during the "campagne d'appels process" a webservice might fail
            //  - user clicks "Quit"
            //  - user clicks "FinTraitement"
            // then we allow closing
            if( _errorOccured )
            {
                base.OnClosing( e );
            }

            // If Alt + F4 --> send message to close()
            else
            {
                e.Cancel = true;
                Clipboard.SetText( _messagePrefix + "Close" );
            }
        }
    }
}
