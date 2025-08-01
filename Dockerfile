FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
EXPOSE 8080 80
WORKDIR /app
COPY /built/ /app

RUN ls -lsa /app


USER $APP_UID
ENTRYPOINT ["dotnet", "ClearMeasure.Bootcamp.UI.Server.dll"]