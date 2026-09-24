# workorders/worker: the NServiceBus endpoint WorkOrderProcessing (app:src/Worker, F5),
# deployed as Deployment `worker` (contract §7.4, §7.5). It has no HTTP endpoint and
# no probes until WI-04; it requires RemotableBus__ApiUrl and the SQL connection
# string from the environment (appsettings files are not published).
#
# Build context: a directory holding publish/ (the `dotnet publish` output of
# src/Worker), staged by .codefresh/scripts/stage-built.sh; built, pushed and signed
# by Codefresh workorders/release (and workorders/preview for previews).
#
# Base: ASP.NET Core runtime, because ChurchBulletin.ServiceDefaults references the
# Microsoft.AspNetCore.App shared framework.
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:<dotnet-aspnet-10.0-digest>

ARG VERSION=0.0.0-local
ARG REVISION=unknown

LABEL org.opencontainers.image.source="https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders" \
      org.opencontainers.image.revision="${REVISION}" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.title="workorders/worker" \
      org.opencontainers.image.description="Work Orders NServiceBus endpoint WorkOrderProcessing"

WORKDIR /app
# Files stay root-owned and read-only for the runtime user.
COPY publish/ /app/

USER $APP_UID
ENTRYPOINT ["dotnet", "Worker.dll"]
