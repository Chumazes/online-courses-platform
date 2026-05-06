FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["OnlineCourses.API/OnlineCourses.API.csproj", "OnlineCourses.API/"]
COPY ["OnlineCourses.Data/OnlineCourses.Data.csproj", "OnlineCourses.Data/"]
COPY ["OnlineCourses.Models/OnlineCourses.Models.csproj", "OnlineCourses.Models/"]

RUN dotnet restore "OnlineCourses.API/OnlineCourses.API.csproj"

COPY . .

RUN dotnet publish "OnlineCourses.API/OnlineCourses.API.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OnlineCourses.API.dll"]
