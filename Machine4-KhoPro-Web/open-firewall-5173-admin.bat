@echo off
netsh advfirewall firewall add rule name="KhoPro Web 5173" dir=in action=allow protocol=TCP localport=5173
pause
