#!/bin/bash
set -ev

# ---------------------------------
# BUILD
# ---------------------------------

dotnet restore
dotnet build -c Release src