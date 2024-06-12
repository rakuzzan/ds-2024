start start "" /D "D:\nats" nats-server.exe

cd ../RankCalculator
start "" dotnet run

cd ..\Valuator\
start dotnet run --urls "http://localhost:5001"
start dotnet run --urls "http://localhost:5002"

cd ..\..\nginx-1.25.5
start nginx -c ..\ds-2024\nginx\nginx.conf
