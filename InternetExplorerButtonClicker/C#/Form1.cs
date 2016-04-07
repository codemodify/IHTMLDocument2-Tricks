using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using mshtml;
using System.Timers;

namespace InternetExplorerButtonClicker
{
    public partial class Form1 : Form
    {
        static String   _clipboardPreviousValue = String.Empty;
        ClipboardHelper _clipboardHelper = new ClipboardHelper();

        public Form1()
        {
            InitializeComponent();

            _clipboardHelper.ClipboardChanged = new EventHandler<ClipboardChangedEventArgs>( ClipChanged );
            _webBrowser.Url = new Uri( Program._params._siteToLoad );
            Helper.Win32.HtmlDocumentFoundEvent += new Helper.Win32.HtmlDocumentFoundEventHandler( HtmlDocumentFound );
        }

        private void ClipChanged( object sender, EventArgs dataObject )
        {
            String clipboardValue = Clipboard.GetText();
            if(
                String.Empty.Equals( clipboardValue )
                ||
                "".Equals( clipboardValue )
            )
                return;

            Helper.Win32.AskHtmlDocumentFromProcess( Program._params._webAppToLookFor );
            //Helper.Win32.AskHtmlDocumentFromWindow( "Contact Agent Simulation - Windows Internet Explorer" );
        }

        private void HtmlDocumentFound( object sender, EventArgs dataObject )
        {
            IHTMLDocument2 iHTMLDocument = sender as IHTMLDocument2;
            if( null == iHTMLDocument )
                return;

            sendMessageToCallCenter( ref iHTMLDocument, Clipboard.GetText() );
        }

        private void sendMessageToCallCenter( ref IHTMLDocument2 targetHtmlDocument, String phoneNumber )
        {
            MethodInfo getElementById = targetHtmlDocument.GetType().GetMethod( "getElementById" );

            object[] param1 = new object[] { Program._params._webAppPhoneField };
            IHTMLElement textInput = getElementById.Invoke( targetHtmlDocument, param1 ) as IHTMLElement;

            if( null == textInput ) // wrong window ?
                return;

            textInput.innerText = phoneNumber.Replace( " ", "" );

            object[] param2 = new object[] { Program._params._webAppCallButton };
            IHTMLElement buttonInput = getElementById.Invoke( targetHtmlDocument, param2 ) as IHTMLElement;

            MethodInfo  mi = buttonInput.GetType().GetMethod( "click" );
            mi.Invoke( buttonInput, new object[ 0 ] );
        }
    }
}
