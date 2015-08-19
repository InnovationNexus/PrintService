Imports System.ComponentModel
Imports System.Configuration.Install
Imports System.ServiceProcess

Public Class ProjectInstaller

    Public Sub New()
        MyBase.New()

        'This call is required by the Component Designer.
        InitializeComponent()

        'Add initialization code after the call to InitializeComponent

    End Sub


    Private Sub ServiceInstaller1_AfterInstall(sender As System.Object, e As System.Configuration.Install.InstallEventArgs) Handles ServiceInstaller1.AfterInstall

    End Sub

    'Private Sub serviceInstaller1_BeforeInstall(sender As Object, e As InstallEventArgs)
    '    Try
    '        Dim controller As ServiceController = ServiceController.GetServices().Where(Function(s) s.ServiceName = ServiceInstaller1.ServiceName).FirstOrDefault()
    '        If controller IsNot Nothing Then
    '            If (controller.Status <> ServiceControllerStatus.Stopped) AndAlso (controller.Status <> ServiceControllerStatus.StopPending) Then
    '                controller.[Stop]()
    '            End If
    '        End If
    '    Catch ex As Exception
    '        Throw New System.Configuration.Install.InstallException(ex.Message.ToString())
    '    End Try
    'End Sub

    'Private Sub serviceInstaller1_BeforeUninstall(sender As Object, e As InstallEventArgs)
    '    Try
    '        Dim controller As ServiceController = ServiceController.GetServices().Where(Function(s) s.ServiceName = ServiceInstaller1.ServiceName).FirstOrDefault()
    '        If controller IsNot Nothing Then
    '            If (controller.Status <> ServiceControllerStatus.Stopped) AndAlso (controller.Status <> ServiceControllerStatus.StopPending) Then
    '                controller.[Stop]()
    '            End If
    '        End If
    '    Catch ex As Exception
    '        Throw New System.Configuration.Install.InstallException(ex.Message.ToString())
    '    End Try
    'End Sub

    'Private Sub serviceInstaller1_AfterInstall(sender As Object, e As InstallEventArgs)
    '    Dim SC1 As New ServiceController(ServiceInstaller1.ServiceName)
    '    SC1.Start()
    'End Sub


End Class
