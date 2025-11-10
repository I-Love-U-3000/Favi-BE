# FaviBE.Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT=Favi-BE.API/Favi-BE.API.csproj    # ← đổi mặc định cho khớp
ARG CONFIG=Release

WORKDIR /src
COPY . .

RUN echo "Project = $PROJECT" \
 && dotnet restore "$PROJECT" \
 && dotnet publish "$PROJECT" -c $CONFIG -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Favi-BE.API.dll"]