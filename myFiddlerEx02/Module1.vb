Imports System.Threading
Imports Fiddler


Module Module1
    Dim oSecureEndpoint As Proxy
    Dim sSecureEndpointHostname As String = "localhost"
    Dim iSecureEndpointPort As Integer = 7777

    Dim oAllSessions As List(Of Fiddler.Session)

    Sub WriteConsole(s As String, c As ConsoleColor)
        Dim oldColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = c
        Console.Write(s)
        Console.ForegroundColor = oldColor
    End Sub

    Sub WriteCommandResponse(s As String)
        WriteConsole(s, ConsoleColor.Yellow)
    End Sub

    Sub DoQuit()
        WriteCommandResponse("Shutting down...")
        If (oSecureEndpoint IsNot Nothing) Then oSecureEndpoint.Dispose()
        Fiddler.FiddlerApplication.Shutdown()
        Thread.Sleep(500)
    End Sub


    Sub Main()
        oAllSessions = New List(Of Fiddler.Session)

        Fiddler.FiddlerApplication.SetAppDisplayName("FiddlerCoreDemoApp")

        ' Attach interrupt here (removed)

        Dim sSAZInfo As String = "NoSAZ"
        Console.WriteLine(String.Format("Starting {0} ({1})...", Fiddler.FiddlerApplication.GetVersionString(), sSAZInfo))

        Console.WriteLine("Press <ENTER> to quit")
        Console.ReadLine()
        DoQuit()
    End Sub

End Module
