FROM node:24-alpine AS frontend-build
WORKDIR /src/frontend

COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci

COPY src/frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

COPY src/RePlay.Server/RePlay.Server.csproj src/RePlay.Server/
RUN dotnet restore src/RePlay.Server/RePlay.Server.csproj

COPY src/RePlay.Server/ src/RePlay.Server/
RUN dotnet publish src/RePlay.Server/RePlay.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

COPY --from=frontend-build /src/frontend/dist/ /app/publish/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=backend-build /app/publish/ ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RePlay.Server.dll"]
