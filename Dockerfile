FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY src/ ./
RUN dotnet publish DeliveryHub.csproj -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
ENV DATA_DIR=/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeliveryHub.dll"]
