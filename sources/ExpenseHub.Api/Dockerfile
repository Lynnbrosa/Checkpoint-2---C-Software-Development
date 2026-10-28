FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY [".editorconfig", "./"]
COPY ["sources/ExpenseHub.Api/ExpenseHub.Api.csproj", "sources/ExpenseHub.Api/"]
RUN dotnet restore "sources/ExpenseHub.Api/ExpenseHub.Api.csproj"

COPY ["sources/ExpenseHub.Api/", "sources/ExpenseHub.Api/"]
RUN dotnet publish "sources/ExpenseHub.Api/ExpenseHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
LABEL org.opencontainers.image.source="https://github.com/Lynnbrosa/Checkpoint-2---C-Software-Development"
WORKDIR /app
RUN mkdir -p /data/keys && chown -R $APP_UID:$APP_UID /data
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExpenseHub.Api.dll"]

