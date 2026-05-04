@echo off
echo Starting Test Backend...
docker-compose up -d --build
echo Test Backend started. Open http://localhost:8000 in your browser.
pause
