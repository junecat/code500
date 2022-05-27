FROM mcr.microsoft.com/dotnet/aspnet:6.0
ENV TZ=Europe/Moscow
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
COPY bin/Release/net6.0/publish/ App/
WORKDIR /App
EXPOSE 3967
ENTRYPOINT ["dotnet", "code500.dll"]