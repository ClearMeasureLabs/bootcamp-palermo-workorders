#!/usr/bin/env bash
export ASPNETCORE_ENVIRONMENT=Development
export APPLICATIONINSIGHTS_CONNECTION_STRING='InstrumentationKey=586d68ed-85bc-4092-ac8a-fabb7a583e93;IngestionEndpoint=https://centralus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/;ApplicationId=5328e763-3c56-4eae-ad66-aa528a92e984'
export ConnectionStrings__SqlConnectionString='server=127.0.0.1;database=ChurchBulletin_7158_serve;User ID=sa;Password=IbmBob-mssql#Dev1;TrustServerCertificate=true;'
export AI_OpenAI_ApiKey=''
export AI_OpenAI_Url=''
export AI_OpenAI_Model=''
exec dotnet run --project '/workspace/src/UI/Server' --configuration Release --no-launch-profile --urls http://0.0.0.0:8080
