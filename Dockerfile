FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HospitalManagamentSystem.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish HospitalManagamentSystem.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "HospitalManagamentSystem.dll"]
