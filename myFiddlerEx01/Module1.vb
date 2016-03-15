'
'  Conversion of FiddlerCore demonstration program from C# to Visual Basic.Net by Super169
'  - SAZ support is not converted
'
'/*
'* This demo program shows how to use the FiddlerCore library.
'*
'* Before compiling, ensure that the project's REFERENCES list points to the 
'* copy of FiddlerCore.dll included in this package.
'*
'* SESSION ARCHIVE (SAZ) SUPPORT
'*===========
'* By default, the project Is compiled without support for the SAZ File format.
'* If you want to add SAZ support, define the token SAZ_SUPPORT in the list of
'* Conditional Compilation symbols on the project's BUILD tab. You will also
'* need to add Ionic.Zip.Reduced.dll to your project's references, add the included
' * SAZ-DotNetZip.cs file to your code, And set 
' * 
' *    FiddlerApplication.oSAZProvider = New DNZSAZProvider();
' *    
' * in your startup code, as shown below.
'*/


Imports System
Imports System.Collections.Generic
Imports System.Threading
Imports Fiddler

Module Module1
    Dim oSecureEndpoint As Proxy
    Dim sSecureEndpointHostname As String = "localhost"
    Dim iSecureEndpointPort As Integer = 7777

    Dim oAllSessions As List(Of Fiddler.Session)

    Sub WriteCommandResponse(s As String)
        Dim oldColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.Write(s)
        Console.ForegroundColor = oldColor
    End Sub

    Sub DoQuit()
        WriteCommandResponse("Shutting down...")
        If (oSecureEndpoint IsNot Nothing) Then oSecureEndpoint.Dispose()
        Fiddler.FiddlerApplication.Shutdown()
        Thread.Sleep(500)
    End Sub

    Function Ellipsize(s As String, iLen As Integer) As String
        If (s.Length <= iLen) Then Return s
        Return s.Substring(0, iLen - 3) + "..."
    End Function


    Sub WriteSessionList(oAllSessions As List(Of Fiddler.Session))
        Dim oldColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("Session list contains...")

        Try
            Monitor.Enter(oAllSessions)
            For Each oS As Session In oAllSessions
                Console.Write(String.Format("{0} {1} {2}\n{3} {4}\n\n", oS.id, oS.oRequest.headers.HTTPMethod, Ellipsize(oS.fullUrl, 60), oS.responseCode, oS.oResponse.MIMEType))
            Next
        Catch ex As Exception

        Finally
            Monitor.Exit(oAllSessions)
        End Try

        Console.WriteLine()
        Console.ForegroundColor = oldColor
    End Sub


    Sub Main()

        oAllSessions = New List(Of Fiddler.Session)

        ' <-- Personalize for your Application, 64 chars Or fewer
        Fiddler.FiddlerApplication.SetAppDisplayName("FiddlerCoreDemoApp")

#Region "AttachEventListeners"

        '
        '  It Is important to understand that FiddlerCore calls event handlers on session-handling
        '  background threads.  If you need to properly synchronize to the UI-thread (say, because
        '  you're adding the sessions to a list view) you must call .Invoke on a delegate on the 
        '  window handle.
        '
        '  If you are writing to a non-threadsafe data structure (e.g. List<t>) you must
        '  use a Monitor Or other mechanism to ensure safety.
        '
        '
        '  Simply echo notifications to the console.  Because Fiddler.CONFIG.QuietMode=true 
        '  by default, we must handle notifying the user ourselves.

        'Fiddler.FiddlerApplication.OnNotification += delegate(object sender, NotificationEventArgs oNEA) { Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); }
        'Fiddler.FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); }
        AddHandler Fiddler.FiddlerApplication.OnNotification, AddressOf OnNotificationHandler
        AddHandler Fiddler.FiddlerApplication.Log.OnLogString, AddressOf OnLogStringHandler
        AddHandler FiddlerApplication.BeforeRequest, AddressOf BeforeRequestHandler
        AddHandler Fiddler.FiddlerApplication.AfterSessionComplete, AddressOf AfterSessionCompleteHandler

        '  Tell the system console to handle CTRL+C by calling our method that
        '  gracefully shuts down the FiddlerCore.
        '  
        '  Note, this doesn't handle the case where the user closes the window with the close button.
        '  See http://geekswithblogs.net/mrnat/archive/2004/09/23/11594.aspx for info on that...
        '
        AddHandler Console.CancelKeyPress, AddressOf Console_CancelKeyPress

#End Region

        Dim sSAZInfo As String = "NoSAZ"


        Console.WriteLine(String.Format("Starting {0} ({1})...", Fiddler.FiddlerApplication.GetVersionString(), sSAZInfo))

        '  For the purposes of this demo, we'll forbid connections to HTTPS 
        '  sites that use invalid certificates. Change this from the default only
        '  if you know EXACTLY what that implies.
        Fiddler.CONFIG.IgnoreServerCertErrors = False

        '  ... but you can allow a specific (even invalid) certificate by implementing And assigning a callback...
        '  FiddlerApplication.OnValidateServerCertificate += New System.EventHandler<ValidateServerCertificateEventArgs>(CheckCert);

        FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", True)

        '  For forward-compatibility with updated FiddlerCore libraries, it Is strongly recommended that you 
        '  start with the DEFAULT options And manually disable specific unwanted options.
        Dim oFCSF As FiddlerCoreStartupFlags = FiddlerCoreStartupFlags.Default








    End Sub


#Region "EventHandler"

    Private Sub OnNotificationHandler(ByVal o As Object, oNEA As NotificationEventArgs)
        Console.WriteLine("** NotifyUser: " & oNEA.NotifyString)
    End Sub

    Private Sub OnLogStringHandler(ByVal o As Object, oLEA As LogEventArgs)
        Console.WriteLine("** LogString: " & oLEA.LogString)
    End Sub

    Private Sub BeforeRequestHandler(ByVal oS As Fiddler.Session)
        '  Console.WriteLine("Before request for:\t" + oS.fullUrl);
        '  In order to enable response tampering, buffering mode MUST
        '  be enabled; this allows FiddlerCore to permit modification of
        '  the response in the BeforeResponse handler rather than streaming
        '  the response to the client as the response comes in.
        oS.bBufferResponse = False
        Monitor.Enter(oAllSessions)
        oAllSessions.Add(oS)
        Monitor.Exit(oAllSessions)

        '  Set this property if you want FiddlerCore to automatically authenticate by
        '  answering Digest/Negotiate/NTLM/Kerberos challenges itself
        '  oS["X-AutoAuth"] = "(default)";

        '  If the Then request Is going To our secure endpoint, we'll echo back the response.

        '  Note: This BeforeRequest Is getting called For both our main proxy tunnel And our secure endpoint, 
        '  so we have to look at which Fiddler port the client connected to (pipeClient.LocalPort) to determine whether this request 
        '  was sent To secure endpoint, Or was merely sent to the main proxy tunnel (e.g. a CONNECT) in order to *reach* the secure endpoint.

        '  As a result of this, if you run the demo And visit https//localhost:7777 in your browser, you'll see

        '  Session List contains...

        '    1 CONNECT http://localhost:7777
        '    200                                         <-- CONNECT tunnel sent to the main proxy tunnel, port 8877

        '    2 GET https://localhost:7777/
        '    200 text/html                               <-- GET request decrypted on the main proxy tunnel, port 8877

        '    3 GET https://localhost:7777/               
        '    200 text/html                               <-- GET request received by the secure endpoint, port 7777


        If ((oS.oRequest.pipeClient.LocalPort = iSecureEndpointPort) And (oS.hostname = sSecureEndpointHostname)) Then
            oS.utilCreateResponseAndBypassServer()
            oS.oResponse.headers.SetStatus(200, "Ok")
            oS.oResponse.headers.Item("Content-Type") = "text/html; charset=UTF-8"
            oS.oResponse.headers.Item("Cache-Control") = "text/html; charset=UTF-8"
            oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString())
        End If

    End Sub

    '/*
    '    // The following event allows you to examine every response buffer read by Fiddler. Note that this isn't useful for the vast majority of
    '    // applications because the raw buffer Is nearly useless; it's not decompressed, it includes both headers and body bytes, etc.
    '    //
    '    // This event Is only useful for a handful of applications which need access to a raw, unprocessed byte-stream
    '    Fiddler.FiddlerApplication.OnReadResponseBuffer += New EventHandler<RawReadEventArgs>(FiddlerApplication_OnReadResponseBuffer);
    '*/

    '/*
    'Fiddler.FiddlerApplication.BeforeResponse += delegate(Fiddler.Session oS) {
    '    // Console.WriteLine("{0}:HTTP {1} for {2}", oS.id, oS.responseCode, oS.fullUrl);

    '    // Uncomment the following two statements to decompress/unchunk the
    '    // HTTP response And subsequently modify any HTTP responses to replace 
    '    // instances of the word "Microsoft" with "Bayden". You MUST also
    '    // set bBufferResponse = true inside the beforeREQUEST method above.
    '    //
    '    //oS.utilDecodeResponse(); oS.utilReplaceInResponse("Microsoft", "Bayden");
    '};*/

    Private Sub AfterSessionCompleteHandler(ByVal oS As Fiddler.Session)

    End Sub

    Private Sub Console_CancelKeyPress(ByVal o As Object, e As ConsoleCancelEventArgs)
        DoQuit()
    End Sub

#End Region

End Module
