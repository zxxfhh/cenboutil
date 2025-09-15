﻿#!/bin/bash

echo -e "\033[32;40m----------开始发布----------\033[0m"

publish=0

network="cenbogeneral-network"
echo -e "\033[32;40m----------网络名称设置:$network----------\033[0m"

dockercomposefile="docker-compose.yml"
echo -e "\033[32;40m----------docker-compose文件:$dockercomposefile----------\033[0m"

case $1 in
"down")
  echo -e "\033[32;40m----------停止服务----------\033[0m"
  #docker-comppose停止
  docker-compose -f $dockercomposefile down
  
  net=$(docker network ls -f name=$network -q)
  if [ -n "$net" ];then
    echo -e "\033[32;40m----------删除网络----------\033[0m"
    docker network rm $network
  fi
  
;;
"up")

  echo -e "\033[32;40m----------启动服务容器----------\033[0m"
  docker network create $network && docker-compose -f $dockercomposefile up -d
  
;;
"build")

  echo -e "\033[32;40m----------构建镜像----------\033[0m"
  docker-compose build
  
;;
"all") 

  echo -e "\033[32;40m----------判断网络是否存在----------\033[0m"
  net=$(docker network ls -f name=$network -q)
  if [ -z "$net" ];then
    echo -e "\033[32;40m----------创建网络----------\033[0m"
    docker network create $network
  fi
  echo -e "\033[32;40m----------停止服务----------\033[0m"
  docker-compose -f $dockercomposefile down
    
  echo -e "\033[32;40m----------构建镜像----------\033[0m"
  docker-compose build
   
  echo -e "\033[32;40m----------启动服务----------\033[0m"
  docker-compose -f $dockercomposefile up -d
  
;;
*)
  publish=1
  echo -e "\033[31;40m----------参数名称错误;支持：up(启动),down(停止),build(镜像构建),all(全部)----------\033[0m"
;;
esac


if (( $publish == 0 ));then
   echo -e "\033[32;40m----------清理构建缓存----------\033[0m"
   docker system prune -f  
fi

echo -e "\033[32;40m----------结束发布----------\033[0m"

