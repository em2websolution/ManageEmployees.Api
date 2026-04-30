FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ManageEmployees.Api/ManageEmployees.Api.csproj", "ManageEmployees.Api/"]
COPY ["ManageEmployees.Domain/ManageEmployees.Domain.csproj", "ManageEmployees.Domain/"]
COPY ["ManageEmployees.Infra.CrossCutting.IoC/ManageEmployees.Infra.CrossCutting.IoC.csproj", "ManageEmployees.Infra.CrossCutting.IoC/"]
COPY ["ManageEmployees.Infra.Data/ManageEmployees.Infra.Data.csproj", "ManageEmployees.Infra.Data/"]
COPY ["ManageEmployees.Services/ManageEmployees.Services.csproj", "ManageEmployees.Services/"]
RUN dotnet restore "ManageEmployees.Api/ManageEmployees.Api.csproj"
COPY . .
WORKDIR "/src/ManageEmployees.Api"
RUN dotnet build "ManageEmployees.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ManageEmployees.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ManageEmployees.Api.dll"]
