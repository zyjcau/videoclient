$ErrorActionPreference = 'Stop'

function Test-DotNet471 {
    $key = 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full'
    $release = (Get-ItemProperty -Path $key -Name Release -ErrorAction SilentlyContinue).Release
    return ($release -ge 461308)
}

function Test-VcRuntimeX64 {
    $key = 'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64'
    $runtime = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
    return ($runtime -and $runtime.Installed -eq 1 -and $runtime.Major -ge 14)
}

function Run-Installer($path, $arguments, $name) {
    if (!(Test-Path $path)) {
        throw "$name installer not found: $path"
    }

    Write-Host "Installing $name ..."
    $process = Start-Process -FilePath $path -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -notin @(0, 3010, 1641, 1638)) {
        throw "$name installation failed. Exit code: $($process.ExitCode)"
    }

    return ($process.ExitCode -in @(3010, 1641))
}

$rebootRequired = $false

if (!(Test-DotNet471)) {
    $rebootRequired = (Run-Installer ".\NDP471-KB4033342-x86-x64-AllOS-ENU.exe" "/q /norestart" ".NET Framework 4.7.1") -or $rebootRequired
}

if (!(Test-VcRuntimeX64)) {
    $rebootRequired = (Run-Installer ".\vc_redist.x64.exe" "/install /quiet /norestart" "Microsoft Visual C++ Redistributable x64") -or $rebootRequired
}

if (!(Test-Path ".\VideoClientWindows_1.04.14_26.6.6.core.exe")) {
    throw "Client installer not found: VideoClientWindows_1.04.14_26.6.6.core.exe"
}

Start-Process -FilePath ".\VideoClientWindows_1.04.14_26.6.6.core.exe" -Wait

if ($rebootRequired) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show("Runtime installation completed. Please restart Windows before starting the client.", "VideoClientWindows", "OK", "Information") | Out-Null
}
