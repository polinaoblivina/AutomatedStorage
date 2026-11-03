FROM node:22-alpine AS frontend
WORKDIR /src/server/frontend
COPY server/frontend/package.json server/frontend/package-lock.json ./
RUN npm ci
COPY server/frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY server/ServerAutomatedStorage.Domain/ServerAutomatedStorage.Domain.csproj server/ServerAutomatedStorage.Domain/
COPY server/ServerAutomatedStorage.Application/ServerAutomatedStorage.Application.csproj server/ServerAutomatedStorage.Application/
COPY server/ServerAutomatedStorage.Infrastructure/ServerAutomatedStorage.Infrastructure.csproj server/ServerAutomatedStorage.Infrastructure/
COPY server/ServerAutomatedStorage.WebAPI/ServerAutomatedStorage.WebAPI.csproj server/ServerAutomatedStorage.WebAPI/
RUN dotnet restore server/ServerAutomatedStorage.WebAPI/ServerAutomatedStorage.WebAPI.csproj
COPY server/ ./server/
COPY --from=frontend /src/server/ServerAutomatedStorage.WebAPI/wwwroot ./server/ServerAutomatedStorage.WebAPI/wwwroot
RUN dotnet publish server/ServerAutomatedStorage.WebAPI/ServerAutomatedStorage.WebAPI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServerAutomatedStorage.WebAPI.dll"]
