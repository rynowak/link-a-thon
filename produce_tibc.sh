#!/usr/bin/env bash

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

referencescommandline=

for dll in $scriptroot/src/ApiTemplate/obj/Release/netcoreapp3.0/linux-x64/multifile-publish/*.dll
do
  referencescommandline="$referencescommandline -r:$dll"
done

rm -r -f $scriptroot/tibcdata
mkdir -p $scriptroot/tibcdata

for dll in $scriptroot/src/ApiTemplate/obj/Release/netcoreapp3.0/linux-x64/multifile-publish/*.dll
do
  assemfullname=$(basename $dll)
  assemname="${assemfullname%.*}"
  ibcfile=$scriptroot/rawibcdata/$assemname.ibc
  tibcfile=$scriptroot/tibcdata/$assemname.tibc
  if [ -f $ibcfile ]
  then
    tibc_command="$scriptroot/src/coreclrbin/tibcmgr/tibcmgr convert $referencescommandline $ibcfile $dll $tibcfile"
    echo $tibc_command
    $tibc_command
  fi
done

