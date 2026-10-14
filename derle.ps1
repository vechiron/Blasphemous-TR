# derle.ps1 - Blasphemous TR derleme betigi. Hazirlayan: Vechiron
# Kullanim: powershell -ExecutionPolicy Bypass -File derle.ps1 ["C:\...\Blasphemous"]
#   (yol verilmezse Steam'in varsayilan kurulum yolu kullanilir)
# Cikti (bin\): BlasphemousTR.dll, BlasphemousTR-Kurulum.exe, BlasphemousTR.zip
# Indirilenler (bin\indir\, karmalari sabit): resmi BepInEx 5.4.23.4, TurkishFontFix v1.0.0
# Derleme duzeni: csc v4, -nostdlib, oyunun Managed kutuphaneleri (CLR 2.0 / Unity 2017.4), C# 5.
# Arguman yanit dosyasiyla (.rsp) verilir: bosluklu yollarda tirnak sorunu olmasin.
# Cikis: 0 derlendi - 1 hata
param([string]$Oyun = 'C:\Program Files (x86)\Steam\steamapps\common\Blasphemous')
$ErrorActionPreference = 'Stop'
try {
    $kok = $PSScriptRoot
    $bin = Join-Path $kok 'bin'
    $ind = Join-Path $bin 'indir'
    $yap = Join-Path $bin 'yapim'
    $m = Join-Path $Oyun 'Blasphemous_Data\Managed'
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $bepUrl = 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.4/BepInEx_win_x64_5.4.23.4.zip'
    $bepSha = 'F881201B79DA03E513BF97CDF39607FFA7F9E0D31A519B1AEECA8EB60F8309E7'
    $fontUrl = 'https://github.com/vechiron/Blasphemous-TurkishFontFix/releases/download/v1.0.0/TurkishFontFix.zip'
    $fontSha = '93CAB96F121191994AAAFDCCA0B532EC1577190AC9BEE7CC62D0AB483AA50FFB'   # zip icindeki DLL
    if (-not (Test-Path (Join-Path $m 'Assembly-CSharp-firstpass.dll'))) { Write-Output "Oyun klasoru bulunamadi: $Oyun"; exit 1 }

    if (Test-Path $yap) { Remove-Item -Recurse -Force $yap }
    New-Item -ItemType Directory -Force $ind, $yap | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    function Indir($url, $ad) {
        $p = Join-Path $ind $ad
        if (-not (Test-Path $p)) { Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $p }
        return $p
    }
    function Rsp($argumanlar, $ad) {
        $p = Join-Path $yap $ad
        [IO.File]::WriteAllLines($p, [string[]]$argumanlar, (New-Object Text.UTF8Encoding $false))
        return $p
    }

    # 1) BepInEx (resmi paket): kurulum aracina gomulur, eklenti onun core'una karsi derlenir
    $bepZip = Indir $bepUrl 'BepInEx_win_x64_5.4.23.4.zip'
    if ((Get-FileHash $bepZip -Algorithm SHA256).Hash -ne $bepSha) { Write-Output "BepInEx paketinin karmasi tutmuyor: $bepZip"; exit 1 }
    $bep = Join-Path $yap 'bepinex'
    [IO.Compression.ZipFile]::ExtractToDirectory($bepZip, $bep)
    $core = Join-Path $bep 'BepInEx\core'

    # 2) eklenti
    $dll = Join-Path $bin 'BlasphemousTR.dll'
    $r = @('mscorlib', 'System', 'System.Core', 'UnityEngine', 'UnityEngine.CoreModule', 'Assembly-CSharp-firstpass') | ForEach-Object { '-r:"' + (Join-Path $m ($_ + '.dll')) + '"' }
    $r += '-r:"' + (Join-Path $core 'BepInEx.dll') + '"'
    $r += '-r:"' + (Join-Path $core '0Harmony.dll') + '"'
    $rsp = Rsp (@('-nologo', '-target:library', '-optimize+', '-nostdlib+', '-codepage:65001', ('-out:"' + $dll + '"')) + $r + @('"' + (Join-Path $kok 'src\BlasphemousTR.cs') + '"')) 'eklenti.rsp'
    # -noconfig yanit dosyasinin icinde yok sayiliyor (CS2023) => komut satirinda
    & $csc -noconfig "@$rsp"
    if ($LASTEXITCODE -ne 0) { Write-Output "Eklenti derlenemedi (csc cikis $LASTEXITCODE)"; exit 1 }

    # 3) font eklentisi: TurkishFontFix v1.0.0 surum varligi
    $fontZip = Indir $fontUrl 'TurkishFontFix-1.0.0.zip'
    $font = Join-Path $yap 'font'
    [IO.Compression.ZipFile]::ExtractToDirectory($fontZip, $font)
    $fontDll = Join-Path $font 'BepInEx\plugins\TurkishFontFix\TurkishFontFix.dll'
    if (-not (Test-Path $fontDll) -or (Get-FileHash $fontDll -Algorithm SHA256).Hash -ne $fontSha) { Write-Output "TurkishFontFix.dll bulunamadi ya da karmasi tutmuyor: $fontZip"; exit 1 }

    # 4) gomulecek dosyalar; ad = onek + oyun klasorune gore yol:
    #    "B/" BepInEx (kurulum araci yalniz BepInEx yoksa yazar) - "E/" eklentiler (her kurulumda yazilir)
    $kaynaklar = @()
    foreach ($g in (Get-ChildItem $bep -Recurse -File -Force)) {
        $kaynaklar += [pscustomobject]@{ Ad = 'B/' + $g.FullName.Substring($bep.Length + 1).Replace('\', '/'); Yol = $g.FullName }
    }
    $kaynaklar += [pscustomobject]@{ Ad = 'E/BepInEx/plugins/BlasphemousTR/BlasphemousTR.dll'; Yol = $dll }
    $kaynaklar += [pscustomobject]@{ Ad = 'E/BepInEx/plugins/BlasphemousTR/Turkce.txt'; Yol = (Join-Path $kok 'Turkce.txt') }
    $kaynaklar += [pscustomobject]@{ Ad = 'E/BepInEx/plugins/TurkishFontFix/TurkishFontFix.dll'; Yol = $fontDll }

    # 5) kurulum araci
    $k = Join-Path $kok 'kurulum'
    $exe = Join-Path $bin 'BlasphemousTR-Kurulum.exe'
    $a = @('-nologo', '-target:winexe', '-optimize+', '-codepage:65001',
        ('-win32icon:"' + (Join-Path $k 'simge.ico') + '"'), ('-win32manifest:"' + (Join-Path $k 'app.manifest') + '"'),
        '-r:System.Windows.Forms.dll', '-r:System.Drawing.dll', ('-out:"' + $exe + '"'))
    foreach ($x in $kaynaklar) { $a += ('-resource:"' + $x.Yol + '",' + $x.Ad) }
    $a += ('"' + (Join-Path $k 'Kurulum.cs') + '"')
    $rsp = Rsp $a 'kurulum.rsp'
    & $csc "@$rsp"
    if ($LASTEXITCODE -ne 0) { Write-Output "Kurulum araci derlenemedi (csc cikis $LASTEXITCODE)"; exit 1 }

    # 6) elle kurulum zip'i: icindekiler oyun klasorune kopyalanir; BENIOKU BOM + CRLF
    $oku = [IO.File]::ReadAllText((Join-Path $k 'BENIOKU.txt')).Replace("`r`n", "`n").Replace("`n", "`r`n")
    $okuYol = Join-Path $yap 'BlasphemousTR-BENIOKU.txt'
    [IO.File]::WriteAllText($okuYol, $oku, (New-Object Text.UTF8Encoding $true))
    $zip = Join-Path $bin 'BlasphemousTR.zip'
    if (Test-Path $zip) { Remove-Item $zip }
    $zs = [IO.File]::Open($zip, [IO.FileMode]::Create)
    $za = New-Object IO.Compression.ZipArchive($zs, [IO.Compression.ZipArchiveMode]::Create)
    foreach ($x in $kaynaklar) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, $x.Yol, $x.Ad.Substring(2)) | Out-Null }
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, $okuYol, 'BlasphemousTR-BENIOKU.txt') | Out-Null
    $za.Dispose(); $zs.Dispose()

    foreach ($g in @($dll, $exe, $zip)) { Write-Output "Derlendi: $g" }
    exit 0
} catch {
    Write-Output "Hata: $_"
    exit 1
}
