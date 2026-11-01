#!/bin/bash
dotnet test tests/MusicJournal.Tests/MusicJournal.Tests.csproj --nologo /p:WarningLevel=0
~/.dotnet/tools/reportgenerator \
  -reports:"tests/MusicJournal.Tests/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:TextSummary \
  -classfilters:"-AspNetCoreGeneratedDocument*;-MusicJournal.Views*;-MusicJournal.Migrations*;-MusicJournal.Services*;-Program"
cat coverage-report/Summary.txt
