#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ON.Authentication/Shared ON.Authentication/Shared
COPY ON.Fragment ON.Fragment
COPY ON.Crypto ON.Crypto
RUN dotnet restore "ON.Content/Video/Rumble/Service/ON.Content.Rumble.Service.csproj"
WORKDIR "/src/ON.Contnet/Video/Rumble/Service"
RUN rm -f appsettings.Development.json
RUN dotnet build "ON.Content.Rumble.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ON.Content.Rumble.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ON.Content.Rumble.Service.dll"]