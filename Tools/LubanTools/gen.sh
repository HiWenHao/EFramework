#!/bin/bash
WORKSPACE=../..
CONF_ROOT=.
UNITY_ASSETS_PATH=$WORKSPACE/EF_Unity/Assets/Luban
LUBAN_DLL=$CONF_ROOT/Luban/Luban.dll
LUBAN_CONFIG=$CONF_ROOT/DataTables/luban.conf

dotnet $LUBAN_DLL \
    -t all \
    -c cs-simple-json \
    -d json \
    --conf $LUBAN_CONFIG \
    -x outputCodeDir=$UNITY_ASSETS_PATH/Code \
    -x outputDataDir=$UNITY_ASSETS_PATH/Json 

read -p "Press any key to continue..."