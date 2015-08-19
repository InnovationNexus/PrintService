Imports System.IO
Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Text.RegularExpressions
Imports System.Net.Mail

Partial Public Class PDFPrintService
    Inherits System.ServiceProcess.ServiceBase
    Private interval As Integer = Int32.Parse(ConfigurationManager.AppSettings("intervalSeconds")) * 1000
    Private WithEvents tmr As New System.Timers.Timer(interval)
    Private watchDir = ConfigurationManager.AppSettings("watchdir")
    Private processedDir = ConfigurationManager.AppSettings("processeddir")
    Private appPath As String = ConfigurationManager.AppSettings("foxitexe")
    Private printer As String = ConfigurationManager.AppSettings("printer")
    Private eLog As New System.Diagnostics.EventLog("Application", Environment.MachineName, "PDFPrintService")
    Private testMode As Boolean = Boolean.Parse(ConfigurationManager.AppSettings("testmode"))
    '--- TEST MODE
    'Private websvc As New TTWebSvc.TicketTrackerService()
    '--- Need to eliminate this and build into current service

    Protected Sub New()
#If DEBUG Then

#Else
        InitializeComponent()
#End If

    End Sub

    Private Sub RunPrint(ByVal Files() As FileInfo)
        Dim movefiles As New List(Of String)(Files.Count())
        If Files.Count() > 0 Then
            Dim filename As String
            For Each fi As FileInfo In Files
                filename = fi.FullName
                Try
                    PrintFile(filename)
                    movefiles.Add(fi.FullName)
                Catch printMoveException As Exception
                    sendEmailException("PDF Print Service Exception: <br />Exception Message: " & printMoveException.Message & "<br />" & "Location: RunPrint() function")
                    eLog.WriteEntry("PDF Print Service Exception Message: " & printMoveException.Message & ".  Location: RunPrint() function")
                End Try
            Next
            MoveProcessedFiles(movefiles)
            PurgeOldFiles()
        End If

    End Sub

    Private Sub PrintFile(ByVal filename As String)
        If (testMode) Then
            eLog.WriteEntry("Test mode: simulated printing " + filename + " to " + printer)
            Return
        End If

        Dim proc As New Process

        Try
            proc.StartInfo.CreateNoWindow = True
            proc.StartInfo.FileName = appPath
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            Dim Arguments = String.Format("/t ""{0}"" ""{1}""", filename, printer)
            proc.StartInfo.Arguments = Arguments
            eLog.WriteEntry("Starting: " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments)
            proc.Start()
            eLog.WriteEntry("Process Start")
            proc.WaitForExit(60 * 1000) ' wait 60s.  better to WaitForExit(), but need to test more.
            eLog.WriteEntry("After wait for exit")
        Catch processStart As Exception
            sendEmailException("PDF Print Service Exception: <br />Exception Message: " & processStart.Message & "<br />" & "Location: PrintFile() function")
            eLog.WriteEntry("PDF Print Service Exception Message: " & processStart.Message & ".  Location: PrintFile() function")
        End Try

        Try
            If (Not proc.HasExited) Then
                eLog.WriteEntry("Foxit has not exited after 60s, and will be killed.")
                proc.Kill()
                proc.Close()
            End If
        Catch procNoExit As Exception
            sendEmailException("PDF Print Service Exception: <br />Exception Message: " & procNoExit.Message & "<br />" & "Location: PrintFile() function")
            eLog.WriteEntry("Foxit has not exited after 90s, and there was a problem in the try catch to kill the process. Error Message: " & procNoExit.Message)
        End Try
    End Sub

    Private Sub sendEmailException(ByVal emailBody As String)

        Dim msgMail As MailMessage = New MailMessage()
        Dim smtpClient As SmtpClient = New SmtpClient()
        smtpClient.Port = 52525
        msgMail.To.Clear()
        msgMail.To.Add(New MailAddress("JPHenry@EmployReward.com"))
        msgMail.From = New MailAddress("Print_Service_Error@EmployReward.com")
        msgMail.Priority = Net.Mail.MailPriority.High
        msgMail.IsBodyHtml = True
        msgMail.Subject = "PDF Print Service Error"
        msgMail.Body = emailBody
        msgMail.CC.Clear()
        msgMail.CC.Add("LPerkins@EmployReward.com")
        msgMail.CC.Add("JJordan@EmployReward.com")

        Try
            smtpClient.Send(msgMail)
        Catch smtpExc As SmtpException
            eLog.WriteEntry("Error sending email notification.  Error Message: " & smtpExc.Message)
            Exit Sub
        End Try
        msgMail.Dispose()
        smtpClient.Dispose()
    End Sub

    Private Sub MoveProcessedFiles(ByVal movefiles As List(Of String))
        For Each filename In movefiles
            Dim filenameonly = System.IO.Path.GetFileName(filename)
            Dim getPath = watchDir & filenameonly
            Dim movePath = processedDir & filenameonly

            If File.Exists(getPath) Then
                Try
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(getPath, movePath, True)
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(getPath)
                Catch noDelete As Exception
                    sendEmailException("PDF Print Service Exception: <br />Exception Message: " & noDelete.Message & "<br />" & "Location: MoveProcessedFiles() function")
                    eLog.WriteEntry("Problem deleting and moving files :" & noDelete.Message)
                End Try
            End If
        Next
        eLog.WriteEntry("MoveProcessedFiles ran successfully.")
    End Sub

    Private Function GetDestinationFilename(ByVal destFilename As String) As String
        Dim dir As String = Path.GetDirectoryName(destFilename)
        Dim fname As String = Path.GetFileName(destFilename)
        fname = String.Format("{0} {1}", DateTime.Now.ToString("yyyyMMddHHmmss"), fname)
        destFilename = Path.Combine(dir, fname)
        Return destFilename
        eLog.WriteEntry("GetDestinationFilename ran successfully. Filename is " & destFilename)
    End Function

    Private Sub PurgeOldFiles()
        Dim days As Int32 = Int32.Parse(ConfigurationManager.AppSettings("purgeAfterDays"))
        Dim cutoff = DateTime.Now.AddDays(-days)
        Dim di As New DirectoryInfo(processedDir)
        For Each f In di.GetFiles("*.pdf")
            Dim dt = ParseDateFromFilename(f.Name)
            If (dt = DateTime.MinValue) Then dt = f.LastWriteTime
            If (dt < cutoff) Then
                f.Delete()
            End If
        Next
        eLog.WriteEntry("PurgeOldFiles ran successfully")
    End Sub

    Private Function ParseDateFromFilename(ByVal filename As String) As DateTime
        Dim pattern = "^((?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<hour>\d{2})(?<min>\d{2})(?<sec>\d{2})\s+(?<filename>.*))$"
        Dim rex As New Regex(pattern)
        Try
            Dim match = rex.Match(filename)
            Dim year As Integer = match.Groups("year").Value
            Dim month As Integer = match.Groups("month").Value
            Dim day As Integer = match.Groups("day").Value
            Dim hour As Integer = match.Groups("hour").Value
            Dim min As Integer = match.Groups("min").Value
            Dim sec As Integer = match.Groups("sec").Value
            Dim dt As New DateTime(year, month, day, hour, min, sec)
            'Dim dt As New DateTime(match.Groups("year").Value, match.Groups("month").Value, match.Groups("day").Value, match.Groups("hour").Value, match.Groups("min").Value, match.Groups("sec").Value)
            Return dt
        Catch
            Return DateTime.MinValue
        End Try
        eLog.WriteEntry("ParseDateFromFilename() ran successfully")
    End Function

    Private Sub Tmr_Tick(ByVal sender As Object, ByVal e As Timers.ElapsedEventArgs) Handles tmr.Elapsed
        tmr.Stop()
        Dim di As New DirectoryInfo(watchDir)
        eLog.WriteEntry("Getting Files")
        Dim files = di.GetFiles("*.pdf")
        If (files.Count() > 0) Then
            eLog.WriteEntry("File Count = " & files.Count())
            RunPrint(files)
            eLog.WriteEntry("Finished RunPrint Function")
        End If
        tmr.Start()
        eLog.WriteEntry("Timer Start")
    End Sub

    Protected Overrides Sub OnStart(ByVal args() As String)
        eLog.WriteEntry("PDF Print Service Start")
        tmr.Enabled = True
        tmr.Start()
        tmr.AutoReset = True
    End Sub

    Protected Overrides Sub OnStop()
        eLog.WriteEntry("PDF Print Service Stop")
        tmr.Stop()
    End Sub

End Class
