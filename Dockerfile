# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Useless Inventions/Useless Inventions.csproj", "Useless Inventions/"]
RUN dotnet restore "Useless Inventions/Useless Inventions.csproj"
COPY . .
WORKDIR "/src/Useless Inventions"
RUN dotnet build "Useless Inventions.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Useless Inventions.csproj" -c Release --no-restore -o /app/publish

# Final stage (runtime image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Setting up environment variable for local dev and Azure deployment compatibility
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Useless Inventions.dll"]

# Health check
HEALTHCHECK CMD curl --fail http://localhost:8080/ || exit 1
