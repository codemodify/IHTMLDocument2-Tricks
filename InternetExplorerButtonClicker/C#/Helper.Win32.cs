
using System;
using System.Text;
using mshtml;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace Helper
{
    public class Win32
    {
        public delegate void HtmlDocumentFoundEventHandler( object sender, EventArgs e );
        public static event HtmlDocumentFoundEventHandler HtmlDocumentFoundEvent = null;

        public delegate bool EnumWindowsDelegate     ( int hwnd, int lParam );
        public delegate int  EnumChildWindowsDelegate( int hwnd, int lParam );

        static String s_windowText;
        static String s_processName;

        #region Win32 API Import

            [DllImport( "user32.Dll" )]
            public static extern int EnumWindows( EnumWindowsDelegate x, int y );
            [DllImport( "User32.Dll" )]
            public static extern void GetWindowText( int h, StringBuilder s, int nMaxCount );
            [DllImport( "User32.Dll" )]
            public static extern void GetClassName( int h, StringBuilder s, int nMaxCount );
            [DllImport( "User32.Dll" )]
            public static extern IntPtr PostMessage( IntPtr hWnd, int msg, int wParam, int lParam );

            [DllImport( "User32.Dll" )]
            public static extern int EnumChildWindows( IntPtr hwndParent, EnumChildWindowsDelegate lpEnumFunc, int lParam );

            [DllImport( "user32.dll", EntryPoint = "RegisterWindowMessageA" )]
            public static extern int RegisterWindowMessage( string lpString );

            [DllImport( "user32.dll", EntryPoint = "SendMessageTimeoutA" )]
            public static extern int SendMessageTimeout( IntPtr hwnd, int msg, int wParam, int lParam, int fuFlags, int uTimeout, out int lpdwResult );

            [DllImport( "OLEACC.dll" )]
            public static extern int ObjectFromLresult( int lResult, ref Guid riid, int wParam, ref IHTMLDocument2 ppvObject );
            public const int SMTO_ABORTIFHUNG = 0x2;
            public static Guid IID_IHTMLDocument = new Guid( "626FC520-A41E-11CF-A731-00A0C9082637" );


            [DllImport( "user32.dll" )]
            private static extern uint GetWindowThreadProcessId( IntPtr hWnd, out uint ProcessId );

            const uint PROCESS_ALL_ACCESS = 0x000F0000 | 0x00100000 | 0xFFF;
            const uint PROCESS_VM_READ = (0x0010);
            //[DllImport( "kernel32" )]
            //public static extern IntPtr OpenProcess( uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId );
            [DllImport( "kernel32.dll" )]
            public static extern IntPtr OpenProcess( UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwProcessId );

            [DllImport( "kernel32" )]
            [return: MarshalAs( UnmanagedType.Bool )]
            public static extern bool CloseHandle( IntPtr hObject );

            [DllImport( "psapi.dll" )]
            static extern uint GetModuleBaseName( IntPtr hProcess, IntPtr hModule, out char[] lpBaseName, uint nSize );

	    #endregion

        #region AskHtmlDocumentFromProcess()
        public static void AskHtmlDocumentFromProcess( String processName )
        {
            s_processName = processName;

            EnumWindows( new EnumWindowsDelegate( EnumWindowsCallBackByProcessName ), 0 );
        } 
        #endregion

        #region AskHtmlDocumentFromWindow()
        public static void AskHtmlDocumentFromWindow( String windowText )
        {
            s_windowText = windowText;

            EnumWindows( new EnumWindowsDelegate( EnumWindowsCallBackByWindowText ), 0 );
        } 
        #endregion

        #region EnumWindowsCallBackByProcessName()
        private static bool EnumWindowsCallBackByProcessName( int hwnd, int lParam )
        {
            IntPtr windowHandle = (IntPtr) hwnd;

            uint processId = 0;
            GetWindowThreadProcessId( windowHandle, out processId );

            foreach( Process process in Process.GetProcessesByName( s_processName ) )
            {
                if( process.Id == (int) processId )
                {
                    EnumChildWindows( windowHandle, new EnumChildWindowsDelegate( EnumChildWindowsCallBack ), lParam );

                    //return false;
                }
            }

            return true;
        } 
        #endregion

        #region EnumWindowsCallBackByWindowText()
        private static bool EnumWindowsCallBackByWindowText( int hwnd, int lParam )
        {
            StringBuilder windowText = new StringBuilder( 1024 );

            GetWindowText( hwnd, windowText, windowText.Capacity );

            if( windowText.ToString().Equals( s_windowText ) )
            {
                EnumChildWindows( (IntPtr) hwnd, new EnumChildWindowsDelegate( EnumChildWindowsCallBack ), lParam );

                return false;
            }

            return true;
        } 
        #endregion

        #region EnumChildWindowsCallBack()
        public static int EnumChildWindowsCallBack( int hwnd, int lParam )
        {
            StringBuilder className = new StringBuilder( 256 );

            GetClassName( hwnd, className, className.Capacity );

            if( className.ToString() == "Internet Explorer_Server" )
            {
                int lngMsg = 0;
                int lRes;
                IHTMLDocument2 document = null;

                lngMsg = RegisterWindowMessage( "WM_HTML_GETOBJECT" );
                if( lngMsg != 0 )
                {
                    SendMessageTimeout( (IntPtr) hwnd, lngMsg, 0, 0, SMTO_ABORTIFHUNG, 1000, out lRes );
                    if( !(bool) (lRes == 0) )
                    {
                        int hr = ObjectFromLresult( lRes, ref IID_IHTMLDocument, 0, ref document );
                        if( (bool) (document == null) )
                        {
                            // logInfo( "No IHTMLDocument Found!" );
                        }
                        else
                        {
                            if( null != HtmlDocumentFoundEvent )
                            {
                                HtmlDocumentFoundEvent( document, null );
                            }

                            return 0;
                        }
                    }
                }
            }

            return 1;
        } 
        #endregion
    }
}