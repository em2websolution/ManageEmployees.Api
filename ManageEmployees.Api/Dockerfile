#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ManageEmployees.Api/ManageEmployees.Api.csproj", "ManageEmployees.Api/"]
COPY ["ManageEmployees.Domain/ManageEmployees.Domain.csproj", "ManageEmployees.Domain/"]
COPY ["ManageEmployees.Infra.CrossCutting.IoC/ManageEmployees.Infra.CrossCutting.IoC.csproj", "ManageEmployees.Infra.CrossCutting.IoC/"]
COPY ["ManageEmployees.Infra.Data/ManageEmployees.Infra.Data.csproj", "ManageEmployees.Infra.Data/"]
COPY ["ManageEmployees.Services/ManageEmployees.Services.csproj", "ManageEmployees.Services/"]
COPY ["ManageEmployees.UnitTests/ManageEmployees.UnitTests.csproj", "ManageEmployees.UnitTests/"]
RUN dotnet restore "./ManageEmployees.Api/ManageEmployees.Api.csproj"
COPY . .
WORKDIR "/src/ManageEmployees.Api"
RUN dotnet build "./ManageEmployees.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ManageEmployees.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ManageEmployees.Api.dll"]