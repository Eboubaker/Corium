FROM mcr.microsoft.com/dotnet/sdk:3.1
LABEL version="0.1.0"
LABEL maintainer="Eboubaker Bekkouche"

ENV CORIUM_DOCKERIZED=1

COPY . /source

RUN apt-get update && apt-get install -y libgdiplus

RUN cd /source && dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained false

ENTRYPOINT ["/source/Corium/bin/Debug/netcoreapp3.1/linux-x64/Corium"]