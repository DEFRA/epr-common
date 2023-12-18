FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["EPR.Common.Sample.API.csproj", "./"]
RUN dotnet restore "EPR.Common.Sample.API.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "EPR.Common.Sample.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EPR.Common.Sample.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EPR.Common.Sample.API.dll"]
