FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# Render requires the app to listen on port 10000
ENV ASPNETCORE_URLS=http://+:10000
USER $APP_UID
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TodoView/TodoView.csproj", "TodoView/"]
RUN dotnet restore "TodoView/TodoView.csproj"
COPY . .
WORKDIR "/src/TodoView"
RUN dotnet build "./TodoView.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TodoView.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoView.dll"]