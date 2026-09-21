FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/PropertyRental.Web/PropertyRental.Web.csproj src/PropertyRental.Web/
RUN dotnet restore src/PropertyRental.Web/PropertyRental.Web.csproj
COPY src/PropertyRental.Web/ src/PropertyRental.Web/
RUN dotnet publish src/PropertyRental.Web/PropertyRental.Web.csproj \
    -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PropertyRental.Web.dll"]
