Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
projectDir = fso.GetParentFolderName(WScript.ScriptFullName)
WshShell.CurrentDirectory = projectDir

' Direct binary launch without cmd.exe or npm overhead (~3x faster startup)
electronExe = projectDir & "\node_modules\electron\dist\electron.exe"
If fso.FileExists(electronExe) Then
    WshShell.Run """" & electronExe & """ """ & projectDir & """", 0, False
Else
    WshShell.Run "cmd.exe /c npm start", 0, False
End If