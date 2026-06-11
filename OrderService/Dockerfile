FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrderService/OrderService.csproj", "OrderService/"]
RUN dotnet restore "OrderService/OrderService.csproj"
COPY . .
RUN dotnet build "OrderService/OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService/OrderService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt-get install -y curl
ENTRYPOINT ["dotnet", "OrderService.dll"]
EXPOSE 80