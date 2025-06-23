# 一键启动 GameServer 所有服务
Write-Host '启动 CenterServer...'
Start-Process -FilePath 'dotnet' -ArgumentList 'run --project .\CenterServer\CenterServer.csproj' -NoNewWindow
Write-Host '等待 CenterServer 启动…'
while (-not (Test-NetConnection -ComputerName 'localhost' -Port 7000 -Quiet)) { Start-Sleep -Seconds 1 }
Write-Host 'CenterServer 已就绪'

Write-Host '启动 LogicServer...'
Start-Process -FilePath 'dotnet' -ArgumentList 'run --project .\LogicServer\LogicServer.csproj' -NoNewWindow
Write-Host '等待 LogicServer 启动…'
while (-not (Test-NetConnection -ComputerName 'localhost' -Port 6000 -Quiet)) { Start-Sleep -Seconds 1 }
Write-Host 'LogicServer 已就绪'

Write-Host '启动 GatewayServer...'
Start-Process -FilePath 'dotnet' -ArgumentList 'run --project .\GatewayServer\GatewayServer.csproj' -NoNewWindow
Write-Host '等待 GatewayServer 启动…'
while (-not (Test-NetConnection -ComputerName 'localhost' -Port 5000 -Quiet)) { Start-Sleep -Seconds 1 }
Write-Host 'GatewayServer 已就绪'

Write-Host '所有服务已启动完毕。'
