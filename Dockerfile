# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Useless Inventions/Useless Inventions.csproj", "Useless Inventions/"]

# Add the Npgsql package reference
RUN dotnet add "Useless Inventions/Useless Inventions.csproj" package Npgsql.EntityFrameworkCore.PostgreSQL
RUN dotnet restore "Useless Inventions/Useless Inventions.csproj"
COPY . .
WORKDIR "/src/Useless Inventions"
RUN dotnet build "Useless Inventions.csproj" -c Release -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
RUN dotnet publish "Useless Inventions.csproj" -c Release -o /app/publish

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Useless Inventions.dll"]
