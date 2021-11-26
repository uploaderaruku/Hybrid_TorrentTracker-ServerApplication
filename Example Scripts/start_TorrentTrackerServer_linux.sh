#!/bin/sh

#SERVICE_NAME=서비스명
#DotNetRumtimeName=실행할 .Net Core Runtime 파일명
#PATH_TO_DotNetRunTime=실행할 DotNetRumtime 파일이 위치한 경로명
#PID_PATH_NAME=./서비스명.pid

#실행할때 입력하는 명령어 예시.
#파일 실행권한 부여 항목이 있으므로 스크립트 실행시 반드시 sudo 명령어를 함께 사용해야 합니다.
#sudo sh StartTorrentTrackerServer_linux.sh start,
#sudo sh StartTorrentTrackerServer_linux.sh stop,
#sudo sh StartTorrentTrackerServer_linux.sh restart,

#본 파일은 .Net Core 3.1 Build Menu 에서 Publish 할때, 
#다음과 같은 옵션설정으로 빌드된 DotNetRumtime 실행파일을 기준으로 작성되었습니다.
#게시(Publish)
#배포모드 : 자체포함(SelfContained), 대상 런타임 : linux-x64, 단일 파일 생성(PublishSingleFile) : true

#require custom variables.
SERVICE_NAME=TorrentTrackerServer
PATH_TO_DotNetRunTime=/home/username
DotNetRumtimeName="Torrent.Tracker.Server_linux_x64"

#example
#SERVICE_NAME="BitTorrent Tracker Server"
#PATH_TO_DotNetRunTime=/root/trackerServer/own_dir_path
#DotNetRumtimeName="Torrent.Tracker.Server_linux_x64"

PID_PATH_NAME=$PATH_TO_DotNetRunTime/$SERVICE_NAME.pid

case $1 in
    start)
        echo "Starting $SERVICE_NAME ..."
        if [ ! -f $PID_PATH_NAME ]; then
            cd $PATH_TO_DotNetRunTime
			chmod +x "$DotNetRumtimeName"
            nohup ./"$DotNetRumtimeName"  >> /dev/null & 
            echo $! > $PID_PATH_NAME
            echo "$SERVICE_NAME started ..."
        else
            echo "$SERVICE_NAME is already running ..."
        fi
    ;;
    stop)
        if [ -f $PID_PATH_NAME ]; then
            PID=$(cat $PID_PATH_NAME);
            echo "$SERVICE_NAME stoping ..."
            kill -9 $PID;
            echo "$SERVICE_NAME stopped ..."
            rm $PID_PATH_NAME
        else
            echo "$SERVICE_NAME is not running ..."
        fi
    ;;
    restart)
        if [ -f $PID_PATH_NAME ]; then
            PID=$(cat $PID_PATH_NAME);
            echo "$SERVICE_NAME stopping ...";
            kill -9 $PID;
            echo "$SERVICE_NAME stopped ...";
            rm $PID_PATH_NAME
            echo "$SERVICE_NAME starting ..."
            cd $PATH_TO_DotNetRunTime
			chmod +x "$DotNetRumtimeName"
            nohup ./"$DotNetRumtimeName"  >> /dev/null & 
            echo $! > $PID_PATH_NAME
            echo "$SERVICE_NAME started ..."
        else
            echo "$SERVICE_NAME is not running ..."
        fi
    ;;
esac
