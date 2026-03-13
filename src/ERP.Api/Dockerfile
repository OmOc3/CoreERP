FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ERP.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/ERP.Domain/ERP.Domain.csproj", "src/ERP.Domain/"]
COPY ["src/ERP.Application/ERP.Application.csproj", "src/ERP.Application/"]
COPY ["src/ERP.Infrastructure/ERP.Infrastructure.csproj", "src/ERP.Infrastructure/"]
COPY ["src/ERP.Api/ERP.Api.csproj", "src/ERP.Api/"]

RUN dotnet restore "src/ERP.Api/ERP.Api.csproj"

COPY . .
RUN dotnet publish "src/ERP.Api/ERP.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ERP.Api.dll"]
