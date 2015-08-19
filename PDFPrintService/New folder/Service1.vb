Imports System.IO
Imports System.Configuration
Imports System.Text.RegularExpressions

Partial Public Class PDFPrintService
	Inherits System.ServiceProcess.ServiceBase

	Private interval As Integer = Int32.Parse(ConfigurationManager.AppSettings("intervalSeconds")) * 1000
	Private WithEvents tmr As New System.Timers.Timer(interval)
	Private watchDir = ConfigurationManager.AppSettings("watchdir")
	Private processedDir = ConfigurationManager.AppSettings("processeddir")
	Private appPath As String = ConfigurationManager.AppSettings("foxitexe")
	Private printer As String = ConfigurationManager.AppSettings("printer")
    Private eLog As New System.Diagnostics.EventLog("Application", Environment.MachineName, "PdfPrintService")
	Private testMode As Boolean = Boolean.Parse(ConfigurationManager.AppSettings("testmode"))
	Private websvc As New TTWebSvc.TicketTrackerService()

	Protected Sub New()
		InitializeComponent()
	End Sub

	Private Sub RunPrint(ByVal Files() As FileInfo)
		Dim movefiles As New List(Of String)(Files.Count())

		Dim filename As String
		For Each fi As FileInfo In Files
			filename = fi.FullName
			Try
				PrintFile(filename)
				movefiles.Add(fi.FullName)
			Catch ex As Exception
                eLog.WriteEntry(ex.Message)
			End Try
		Next

		MoveProcessedFiles(movefiles)
		PurgeOldFiles()
	End Sub

	Private Sub PrintFile(ByVal filename As String)
		If (testMode) Then
            eLog.WriteEntry("Test mode: simulated printing " + filename + " to " + printer)
			Return
		End If

		Dim proc As New Process
		proc.StartInfo.CreateNoWindow = True
		proc.StartInfo.FileName = appPath
		proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
		Dim Arguments = String.Format("/t ""{0}"" ""{1}""", filename, printer)
		proc.StartInfo.Arguments = Arguments
        eLog.WriteEntry("Starting: " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments)
		proc.Start()

        proc.WaitForExit(90 * 1000) ' wait 90s.  better to WaitForExit(), but need to test more.
		'proc.WaitForExit()
		'proc.Close()

        If (Not proc.HasExited) Then
            eLog.WriteEntry("Foxit has not exited after 90s, and will be killed.")
			proc.Kill()
			proc.Close()
		End If
    End Sub

    Private Sub MoveProcessedFiles(ByVal movefiles As List(Of String))
        For Each filename In movefiles
            Dim filenameonly = System.IO.Path.GetFileName(filename)
            Dim getPath = Path.Combine(watchDir, filenameonly)
            Dim movePath = Path.Combine(processedDir, filenameonly)

            If File.Exists(getPath) Then
                Try
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(getPath, movePath, True)
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(getPath)
                Catch noDelete As Exception
                    'sendEmailException("PDF Print Service Exception: <br />Exception Message: " & noDelete.Message & "<br />" & "Location: MoveProcessedFiles() function")
                    'eLog.WriteEntry("Problem deleting and moving files :" & ex.Message)
                End Try
            End If
        Next
    End Sub
    'Private Sub MoveProcessedFiles(ByVal movefiles As List(Of String))
    '	For Each filename In movefiles
    '		Dim filenameonly = System.IO.Path.GetFileName(filename)

    '		Try
    '			Dim moved = websvc.MovePrintedFile(filenameonly)
    '			If(Not moved)
    '				eLog.WriteEntry(filename + " could not be moved (not found in source directory)")
    '			End If
    '		Catch ex As Exception
    '			eLog.WriteEntry("Unable to move file " + filename + " via WebService: " + ex.ToString())
    '		End Try

    '		'Dim dest = GetDestinationFilename(Path.Combine(processedDir, filenameonly))
    '		'Try
    '		'	System.IO.File.Move(filename, dest)
    '		'Catch ex As Exception
    '		'	eLog.WriteEntry("Unable to move file " + filename + " to " + dest + ": " + ex.ToString())
    '		'End Try
    '	Next
    'End Sub

	Private Function GetDestinationFilename(ByVal destFilename As String) As String
		Dim dir as string = Path.GetDirectoryName(destFilename)
		Dim fname As String = Path.GetFileName(destFilename)
		fname = String.Format("{0} {1}", DateTime.Now.ToString("yyyyMMddHHmmss"), fname)
		destFilename = Path.Combine(dir, fname)
		Return destFilename
	End Function

	Private Sub PurgeOldFiles()
		Dim days As Int32 = Int32.Parse(ConfigurationManager.AppSettings("purgeAfterDays"))
		Dim cutoff = DateTime.Now.AddDays(-days)
		Dim di As New DirectoryInfo(processedDir)
		For Each f In di.GetFiles("*.pdf")
			Dim dt = ParseDateFromFilename(f.Name)
			If(dt = DateTime.MinValue) Then dt = f.LastWriteTime
			If(dt < cutoff) Then
				f.Delete()
			End If
		Next
	End Sub

	Private Function ParseDateFromFilename(ByVal filename As String) As DateTime
		Dim pattern = "^((?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<hour>\d{2})(?<min>\d{2})(?<sec>\d{2})\s+(?<filename>.*))$"
		Dim rex As New Regex(pattern)
		Try
			Dim match = rex.Match(filename)
			Dim dt As New DateTime(match.Groups("year").Value, match.Groups("month").Value, match.Groups("day").Value, match.Groups("hour").Value, match.Groups("min").Value, match.Groups("sec").Value)
			Return dt
		Catch
			Return DateTime.MinValue
		End Try
	End Function

	Private Sub Tmr_Tick(ByVal sender As Object, ByVal e As Timers.ElapsedEventArgs) Handles tmr.Elapsed
		tmr.Stop()
		Dim di As New DirectoryInfo(watchDir)
		Dim files = di.GetFiles("*.pdf")
		If (files.Count() > 0) Then RunPrint(files)
		tmr.Start()
	End Sub

	Protected Overrides Sub OnStart(ByVal args() As String)
		tmr.Enabled = True
		tmr.Start()
		tmr.AutoReset = True
	End Sub

    Protected Overrides Sub OnStop()
        '      Dim procKill As Process
        tmr.Stop()
    End Sub

End Class
