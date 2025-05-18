param(
    [ValidateSet('Debug','Release')][string]$config = 'Release'
)

dotnet build ./finstar-test.sln -c $config

$projects = @(
    "TaskManagement.API/TaskManagement.API.csproj",
    "TaskManagement.Consumer/TaskManagement.Consumer.csproj",
    "TaskManagement.Listener/TaskManagement.Listener.csproj"
)


Start-Process dotnet "watch run --project `"TaskManagement.API/TaskManagement.API.csproj`""
Start-Process dotnet "watch run --project `"TaskManagement.Consumer/TaskManagement.Consumer.csproj`""
Start-Process dotnet "watch run --project `"TaskManagement.Listener/TaskManagement.Listener.csproj`"  -- --urls http://localhost:5050"
