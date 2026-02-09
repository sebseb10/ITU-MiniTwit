FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Chirp.sln

RUN dotnet publish src/Chirp.Web/Chirp.Web.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .
#Mabey remake this, creates a foler for Database since original db path was ../ 
#which takes the program out of the docker container
RUN mkdir -p Chirp.Infrastructure

ENV ASPNETCORE_URLS=http://+:8080

# sets ASP enviroment to Development so it does not trigger the .isproduction 
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "Chirp.Web.dll"]