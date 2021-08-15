
$source = New-Object System.Text.StringBuilder

Get-ChildItem |
    Where-Object { $_.Name -match '\.cs$' } |
    ForEach-Object { Get-Content -Path $_ -Raw } |
    ForEach-Object { $source.AppendLine($_) > $null }

$addTypeParams = @{
    TypeDefinition = $source.ToString()
    Language = 'CSharpVersion3'
    ReferencedAssemblies = 'System.Windows.Forms', 'System.Drawing'
}

Add-Type @addTypeParams

try {
    $server = New-Object PipeListening.CheapPipeServer("Test1")

    $recievedEvent = Register-ObjectEvent -InputObject $server -EventName Recieved -Action {
        $Event | Out-Host
    }

    $priorityChangedEvent = Register-ObjectEvent -InputObject $server -EventName PriorityChanged -Action {
        $Event | Out-Host
    }

    $text = @'
{0} {1}
'@ -f $server.GetType(), $server.Name
    $server.Start()
    [System.Windows.Forms.MessageBox]::Show($text)
} finally {
    if ($null -ne $server){
        $server.Close()
        $server = $null
    }

    Unregister-Event -SourceIdentifier $recievedEvent.Name
    Unregister-Event -SourceIdentifier $priorityChangedEvent.Name
}
