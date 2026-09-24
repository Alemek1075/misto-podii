FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MistoPodii/MistoPodii.csproj src/MistoPodii/
RUN dotnet restore src/MistoPodii/MistoPodii.csproj
COPY src/MistoPodii/ src/MistoPodii/
RUN dotnet publish src/MistoPodii/MistoPodii.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN mkdir -p /app/data && chown app:app /app/data
COPY --from=build --chown=app:app /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MistoPodii.dll"]
