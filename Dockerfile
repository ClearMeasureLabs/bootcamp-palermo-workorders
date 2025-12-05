FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
EXPOSE 8080 80
WORKDIR /app
COPY /built/ /app

RUN ls -lsa /app && \
    chmod -R 755 /app && \
    chown -R $APP_UID:$APP_UID /app

USER $APP_UID
ENTRYPOINT ["dotnet", "ClearMeasure.Bootcamp.UI.Server.dll"]