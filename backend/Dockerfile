FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/TaskManager.Api/TaskManager.Api.csproj src/TaskManager.Api/
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj
COPY src/TaskManager.Api/ src/TaskManager.Api/
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
