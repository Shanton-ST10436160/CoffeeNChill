FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish CoffeeNChill.Functions.csproj -c Release -o /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
WORKDIR /home/site/wwwroot
COPY --from=build /home/site/wwwroot .
