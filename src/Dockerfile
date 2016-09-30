FROM microsoft/dotnet:1-core

ADD . /app
WORKDIR /app

ENV MAX_AGE 180.0
ENV SLEEP_TIME 185.0

ENTRYPOINT ["dotnet", "NewsHacker.dll"]