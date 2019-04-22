FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY NuGet.Config .
COPY *.sln .
COPY Chronos.Coins ./Chronos.Coins
COPY Chronos.GraphQL.AspNetCore ./Chronos.GraphQL.AspNetCore

# copy everything else and build app
WORKDIR /app/Chronos.GraphQL.AspNetCore
RUN dotnet restore 
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
WORKDIR /app
COPY --from=build /app/Chronos.GraphQL.AspNetCore/out ./
ENTRYPOINT ["dotnet", "Chronos.GraphQL.AspNetCore.dll"]