FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MinecraftServerMicroservice.csproj", "./"]
RUN dotnet restore "MinecraftServerMicroservice.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "MinecraftServerMicroservice.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MinecraftServerMicroservice.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir ../root/.kube
RUN touch config
RUN --mount=type=secret,id=KUBE_CONFIG \
	cat /run/secrets/KUBE_CONFIG > config
RUN cp config ../root/.kube/

ENTRYPOINT ["dotnet", "MinecraftServerMicroservice.dll"]
