@echo off
netsh advfirewall firewall add rule name="Machine3 User Report Service 8083" dir=in action=allow protocol=TCP localport=8083
echo Firewall rule added for TCP port 8083.
pause
