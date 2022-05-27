# code500
This is demo repo for service for return various HTTP codes: 500, 404, 301, etc

Этот "микропроект" возник как ответ на вопрос на стековерфлоу: "Есть сервис где можно получить по get запросу 500-511 ошибки?" ( https://ru.stackoverflow.com/questions/1410394/ )

Сама идея такого сервиа крайне проста: нужно анализировать имя, по кторому происходит обращение, и в зависиимости от этого имени - выдавать тот или иной HTTP-код.

Именно это и просиходит в строчках 


HostString hostString = context.Request.HttpContext.Request.Host;

string host = hostString.Host;

string[] domains = host.Split('.');

... и далее анализируем начало doamins[], пытаясь обнаружить что то типа code404.junecat.ru или www.code404.junecat.ru


Но это еще не совсем всё.

Мы будем запускать контейнер с проектом, и этот контейнер выглядит просто:


FROM mcr.microsoft.com/dotnet/aspnet:6.0

ENV TZ=Europe/Moscow

RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

COPY bin/Release/net5.0/publish/ App/

WORKDIR /App

EXPOSE 3967

ENTRYPOINT ["dotnet", "code500.dll"]


Особенными выглядяд только строчки установки нужнйо таймзоны - ну, хочется в логах читать время в обычном формате. Порт, который экспозится - выбран случайно, всё равно на него будет перенаправлен запрос посредством директивы proxy-pass nginx'а.

Но для этого нужно это приложение собрать.

Собирать тоже будем в контейнере.

Вот Dockerfile для конткйнера, в котором происходит сборка:

