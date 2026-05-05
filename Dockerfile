FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HospitalManagamentSystem.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish HospitalManagamentSystem.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
COPY --from=build /app/publish ./

CMD ["sh", "-c", "dotnet HospitalManagamentSystem.dll --urls http://0.0.0.0:${PORT:-10000}"]
