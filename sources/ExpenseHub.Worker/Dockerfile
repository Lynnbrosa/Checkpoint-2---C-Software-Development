FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY [".editorconfig", "./"]
COPY ["sources/ExpenseHub.Api/ExpenseHub.Api.csproj", "sources/ExpenseHub.Api/"]
COPY ["sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj", "sources/ExpenseHub.Worker/"]
RUN dotnet restore "sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj"

COPY ["sources/ExpenseHub.Api/", "sources/ExpenseHub.Api/"]
COPY ["sources/ExpenseHub.Worker/", "sources/ExpenseHub.Worker/"]
RUN dotnet publish "sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /data

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExpenseHub.Worker.dll"]

