using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace Ui.Desktop
{
    static class main
    {
        [STAThread]
        static void Main()
        {
            #region make sure there is one instance

            int count = 0;

            String processName = AppDomain.CurrentDomain.FriendlyName;

            Process[] processes = Process.GetProcessesByName( Path.GetFileNameWithoutExtension( processName ) );

            foreach( Process process in processes )
            {
                if( process.SessionId == Process.GetCurrentProcess().SessionId ) // check the TerminalSessionId because we're in Citrix
                {
                    count++;
                }
            }

            if( count > 1 )
                return;

            #endregion

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            Application.Run( new CaUiDesktop() );
        }
    }
}
