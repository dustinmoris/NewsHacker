FROM microsoft/dotnet:1-core

ADD . /app
WORKDIR /app

ENV MAX_AGE 120.0
ENV SLEEP_TIME 125.0

ENTRYPOINT ["dotnet", "NewsHacker.dll"]