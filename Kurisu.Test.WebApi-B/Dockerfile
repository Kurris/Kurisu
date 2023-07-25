FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Kurisu.Test.WebApi-B/Kurisu.Test.WebApi-B.csproj", "Kurisu.Test.WebApi-B/"]
RUN dotnet restore "Kurisu.Test.WebApi-B/Kurisu.Test.WebApi-B.csproj"
COPY . .
WORKDIR "/src/Kurisu.Test.WebApi-B"
RUN dotnet build "Kurisu.Test.WebApi-B.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kurisu.Test.WebApi-B.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kurisu.Test.WebApi-B.dll"]
