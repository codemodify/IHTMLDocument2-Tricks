using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace Ui.Desktop
{
    public partial class BandauStarter : Form
    {
        public String WebAppToStart { get; set; }
        public String WebAppParameters { get; set; }
        public bool   WebAppLoadProfile{ get; set; }
        public bool   WebAppShellExec{ get; set; }

        public Process Bandeau;

        public event EventHandler BandeauClosed = delegate { };
        public event EventHandler BandeauStartSuccess = delegate { };
        public event EventHandler BandeauStartError = delegate { };

        public BandauStarter()
        {
            InitializeComponent();
        }

        private void CloseButton_Click( object sender, EventArgs e )
        {
            Close();
        }

        private void BandauStarter_Load( object sender, EventArgs e )
        {
            ThreadPool.QueueUserWorkItem( StartProcess );
        }

        public bool IsBandeauRunning()
        {
            if( null == Bandeau )
                return false;

            if( Bandeau.HasExited )
                return false;

            return true;
        }

        private String ReplaceEnvVars( String stringWithEnvVarsInIt )
        {
            CaUiDesktop.Logger.LogTrace( "ReplaceEnvVars( " + stringWithEnvVarsInIt  + " )" );

            while( stringWithEnvVarsInIt.Contains("%") )
            {
                int startIndex = stringWithEnvVarsInIt.IndexOf( "%", 0, StringComparison.Ordinal );
                int endIndex = stringWithEnvVarsInIt.IndexOf( "%", startIndex+1, StringComparison.Ordinal );

                String envVarAsInBatch = stringWithEnvVarsInIt.Substring( startIndex, endIndex - startIndex + 1 );
                String envVar = envVarAsInBatch.Substring( 1, envVarAsInBatch.Length - 2 );

                String envVarResolved = Environment.GetEnvironmentVariable( envVar );

                stringWithEnvVarsInIt = stringWithEnvVarsInIt.Replace( envVarAsInBatch, envVarResolved );
            }

            CaUiDesktop.Logger.LogTrace( "Resultat: " + stringWithEnvVarsInIt );

            return stringWithEnvVarsInIt;
        }

        private void StartProcess( object stateInfo )
        {
            try
            {
                Thread.Sleep( 1000 );

                // check if the target is already running
                Process[] processes = Process.GetProcessesByName( Path.GetFileNameWithoutExtension(WebAppToStart) );

                Bandeau = null;
                foreach( Process process in processes )
                {
                    if( process.SessionId == Process.GetCurrentProcess().SessionId ) // check the TerminalSessionId because we're in Citrix
                    {
                        Bandeau = process;
                        break;
                    }
                }

                if( null == Bandeau )
                {
                    Bandeau = new Process();
                    Bandeau.StartInfo.FileName = ReplaceEnvVars( WebAppToStart );
                    Bandeau.StartInfo.Arguments = ReplaceEnvVars( WebAppParameters );
                    Bandeau.StartInfo.LoadUserProfile = WebAppLoadProfile;
                    Bandeau.StartInfo.UseShellExecute = WebAppShellExec;
                    Bandeau.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    Bandeau.Start();
                }

                Bandeau.Exited += new EventHandler( Bandeau_Exited );
                Bandeau.EnableRaisingEvents = true;

                BandeauStartSuccess( null, null );
            }
            catch( Exception e )
            {
                CaUiDesktop.Logger.LogTrace( "StartProcess() Exception: " + e.ToString() );


                Bandeau = null;

                BandeauStartError( null, null );
            }
        }

        void Bandeau_Exited( object sender, EventArgs e )
        {
            Bandeau = null;

            BandeauClosed( sender, e );
        }

        public void MonitorForManualStart()
        {
            ThreadPool.QueueUserWorkItem( StartProcessManual );
        }

        private void StartProcessManual( object stateInfo )
        {
            try
            {
                Thread.Sleep( 1000 );

                // check if the target is already running
                Process[] processes = Process.GetProcessesByName( Path.GetFileNameWithoutExtension( WebAppToStart ) );

                Bandeau = null;
                foreach( Process process in processes )
                {
                    if( process.SessionId == Process.GetCurrentProcess().SessionId ) // check the TerminalSessionId because we're in Citrix
                    {
                        Bandeau = process;
                        break;
                    }
                }

                if( null == Bandeau )
                {
                    ThreadPool.QueueUserWorkItem( StartProcessManual );
                    return;
                }

                Bandeau.Exited += new EventHandler( Bandeau_Exited );
                Bandeau.EnableRaisingEvents = true;

                BandeauStartSuccess( null, null );
            }
            catch( Exception e )
            {
                CaUiDesktop.Logger.LogTrace( "StartProcessManual() Exception: " + e.ToString() );

                Bandeau = null;

                BandeauStartError( null, null );
            }
        }
    }
}
