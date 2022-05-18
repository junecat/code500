FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY bin/Release/net5.0/publish/ App/
WORKDIR /App
EXPOSE 3967
ENTRYPOINT ["dotnet", "code500.dll"]