#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Optima.Actors/Optima.Actors.csproj", "Optima.Actors/"]
COPY ["Dapr/src/Dapr.AspNetCore/Dapr.AspNetCore.csproj", "Dapr/src/Dapr.AspNetCore/"]
COPY ["Dapr/src/Dapr.Client/Dapr.Client.csproj", "Dapr/src/Dapr.Client/"]
COPY ["Optima.DatasetLoader/Optima.DatasetLoader.csproj", "Optima.DatasetLoader/"]
COPY ["Optima.Interfaces/Optima.Interfaces.csproj", "Optima.Interfaces/"]
COPY ["Dapr/src/Dapr.Actors/Dapr.Actors.csproj", "Dapr/src/Dapr.Actors/"]
COPY ["Optima.ColumnInferrer/Optima.ColumnInferrer.fsproj", "Optima.ColumnInferrer/"]
COPY ["Optima.ProtoGenerator/Optima.ProtoGenerator.csproj", "Optima.ProtoGenerator/"]
COPY ["Dapr/src/Dapr.Actors.AspNetCore/Dapr.Actors.AspNetCore.csproj", "Dapr/src/Dapr.Actors.AspNetCore/"]
RUN dotnet restore "Optima.Actors/Optima.Actors.csproj"
COPY . .
WORKDIR "/src/Optima.Actors"
RUN dotnet build "Optima.Actors.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Optima.Actors.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Optima.Actors.dll"]