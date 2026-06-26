@echo off
netsh advfirewall firewall add rule name="Machine1 Product Inventory 5001" dir=in action=allow protocol=TCP localport=5001
pause
