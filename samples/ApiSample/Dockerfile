FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as SDK
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /out
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 as RUNTIME
WORKDIR /app
COPY --from=SDK /out .
ENTRYPOINT ["dotnet", "ApiSample.dll"]
