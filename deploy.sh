#!/bin/bash

# Stop the script if a command returns a non 0 exit code
set -ev

# Only publish to Docker Hub on tags
if [ -n "$TRAVIS_TAG" ]; then

    # Read the tag name into an array split by the dot (.)
    IFS='.' read -r -a tag <<< "$TRAVIS_TAG"

    # Remove a leading v from the major version number (e.g. if the tag was v1.0.0)
    MAJOR="${${tag[0]}//v}"

    # Set the other parts of the sem ver to respective variables as well
    MINOR=${tag[1]}
    BUILD=${tag[2]}

    # Create a clean sem ver variable
    SEMVER="$MAJOR.$MINOR.$BUILD"

    # Build the Docker image
    docker build -t dustinmoris/newshacker:$SEMVER src/bin/Release/netcoreapp1.0/publish/.

    # Tag the same image with :latest as well
    docker tag dustinmoris/newshacker:$SEMVER dustinmoris/newshacker:latest

    # Login to Docker Hub and upload images
    docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
    docker push dustinmoris/newshacker:$SEMVER
    docker push dustinmoris/newshacker:latest
    
fi