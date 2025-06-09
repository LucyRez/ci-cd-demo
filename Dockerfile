# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ImageConverter.WebApi/ImageConverter.WebApi.csproj", "ImageConverter.WebApi/"]
RUN dotnet restore "ImageConverter.WebApi/ImageConverter.WebApi.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "ImageConverter.WebApi/ImageConverter.WebApi.csproj" -c Release -o /app/build
RUN dotnet publish "ImageConverter.WebApi/ImageConverter.WebApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "ImageConverter.WebApi.dll"]