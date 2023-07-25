FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Kurisu.Test.WebApi-A/Kurisu.Test.WebApi-A.csproj", "Kurisu.Test.WebApi-A/"]
RUN dotnet restore "Kurisu.Test.WebApi-A/Kurisu.Test.WebApi-A.csproj"
COPY . .
WORKDIR "/src/Kurisu.Test.WebApi-A"
RUN dotnet build "Kurisu.Test.WebApi-A.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kurisu.Test.WebApi-A.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kurisu.Test.WebApi-A.dll"]
