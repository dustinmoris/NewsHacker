FROM microsoft/dotnet:1-core

ADD . /app
WORKDIR /app

ENV MAX_AGE 60.0
ENV SLEEP_TIME 61.0

ENTRYPOINT ["dotnet", "NewsHacker.dll"]