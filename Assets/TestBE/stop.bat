@echo off
echo Stopping and removing Test Backend...
docker-compose down -v
echo Test Backend stopped.
pause
