# code500
This is demo repo for service for return various HTTP codes: 500, 404, 301, etc

Этот "микропроект" возник как ответ на вопрос на стековерфлоу: "Есть сервис где можно получить по get запросу 500-511 ошибки?" ( https://ru.stackoverflow.com/questions/1410394/ )

Сама идея такого сервиа крайне проста: нужно анализировать имя, по кторому происходит обращение, и в зависиимости от этого имени - выдавать тот или иной HTTP-код.

Именно это и просиходит в строчках 

```
HostString hostString = context.Request.HttpContext.Request.Host;
string host = hostString.Host;
string[] domains = host.Split('.');
... и далее анализируем начало doamins[], пытаясь обнаружить что то типа code404.junecat.ru или www.code404.junecat.ru
```

Но это еще не совсем всё.

Мы будем запускать контейнер с проектом, и этот контейнер выглядит просто:

```
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ENV TZ=Europe/Moscow
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
COPY bin/Release/net5.0/publish/ App/
WORKDIR /App
EXPOSE 3967
ENTRYPOINT ["dotnet", "code500.dll"]
```

Особенными выглядят только строчки установки нужной таймзоны - ну, хочется в логах читать время в обычном формате. Порт, который экспозится - выбран случайно, всё равно на него будет перенаправлен запрос посредством директивы proxy-pass nginx'а.

Но для этого нужно это приложение собрать.

**Собирать тоже будем в контейнере.**

Вот Dockerfile для контейнера, в котором происходит сборка:

```
FROM ubuntu:latest
RUN apt-get update
RUN apt-get install -y wget
RUN wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y apt-transport-https
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN apt-get install -y dotnet-sdk-6.0
# dotnet sdk is installed!

RUN apt-get install -y git
ARG ssh_prv_key
ARG ssh_pub_key

# Authorize SSH Host
RUN mkdir -p /root/.ssh
RUN chmod 0700 /root/.ssh
RUN ssh-keyscan github.com > /root/.ssh/known_hosts

# Add the keys and set permissions
RUN echo "$ssh_prv_key" > /root/.ssh/id_rsa
RUN echo "$ssh_pub_key" > /root/.ssh/id_rsa.pub
RUN chmod 600 /root/.ssh/id_rsa
RUN chmod 600 /root/.ssh/id_rsa.pub

WORKDIR /App
RUN git clone git@github.com:junecat/code500.git
WORKDIR /App/code500
RUN dotnet publish -c release
WORKDIR /App/publish-output
CMD cp -r /App/code500/bin/Release/net6.0/* /App/publish-output
```

Теперь "своими словами" расскажу, что здесь творится.

Берется стнадартная убунта, в неё ставится dotnet sdk 6.0

Затем устанавливается git (ну, гит-клиент нужен, чтобы взять из репозитория код программы)

Параллельно из *аргументов команды* берутся ключи для *read-only* доступа к репозиторию с кодом. 

То есть, чтобы выгружать код  я сделал специальную пару ключей, поместил их в гитхаб и на хост, с которого запускаю команду на билд кода.

ключи для ридонли доступа называются ro_key и ro_key.pub

Сама команда на билд выглядит так:

```
docker build -t code500build-image1 --build-arg ssh_prv_key="$(cat ~/.ssh/ro_key)" --build-arg ssh_pub_key="$(cat ~/.ssh/ro_key.pub)" -f Dockerfile .
```

Ну, а потом в докерфайле всё просто: забираем код, строим приложение с ключиком release, потом результаты копируем в папку на хосте. 

Можно нам - хлопать в ладоши, а контейнеру - умирать.

В результате в папке publish-output (папка лежит рядом с Dockerfile) лежат бинарники, которые нужно поместить в папочку bin/Release/net5.0/publish/, чтобы команда из докерфайла запуска приложения

```
COPY bin/Release/net5.0/publish/ App/
```

их нашла и скопировала внутрь уже *рабочего* контейнера