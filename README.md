# NewsHacker
A small bot which regularly checks popular blogs and news feeds for new articles and publishes them on Hacker News.

[![Build Status](https://travis-ci.org/dustinmoris/NewsHacker.svg)](https://travis-ci.org/dustinmoris/NewsHacker)

[![Build history](https://buildstats.info/travisci/chart/dustinmoris/NewsHacker)](https://travis-ci.org/dustinmoris/NewsHacker?branch=master)

## Usage

```
docker run -e USER_COOKIE="<your user cookie value>" dustinmoris/newshacker:latest
```

You must speicify the `USER_COOKIE` environment variable when launching the container. This variable expects the value from the Hacker News `user` cookie.

### Optional parameters

You can set the `MAX_AGE` and `SLEEP_TIME` environment variables to specify the polling interval.

#### MAX_AGE

This environment variabls specifies the **maximum age in seconds** of an article which the bot will try to submit to Hacker News. It makes sure that the bot will not try to publish old articles and in connection with `SLEEP_TIME` it makes sure that the bot will not try to post an article multiple times.

The variable expects a flaoting point value. e.g.: `MAX_AGE=180.0`

#### SLEEP_TIME

This environment variable specifies the **duration in seconds** which the bot will go to sleep before checking for new articles again.

The variable expects a flaoting point value. e.g.: `SLEEP_TIME=181.0`