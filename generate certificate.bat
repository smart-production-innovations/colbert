@echo off

SET openssl_path="C:\Program Files\Git\usr\bin\openssl.exe"
SET openssl_path_alt="%APPDATA%\..\Local\Programs\Git\usr\bin\openssl.exe"

if not exist %openssl_path% (
  SET openssl_path=%openssl_path_alt%
)

if not exist %openssl_path% (
  echo openssl not found - change path in batch file to openssl location
  pause
  exit
)

%openssl_path% req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -sha256 -days 365 -nodes
%openssl_path% pkcs12 -inkey key.pem -in cert.pem -export -out cert.pfx -passout pass: -macalg SHA1 -certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES

echo:

copy cert.pfx cert.bytes /Y

echo:
echo generated certificate successfully!
echo:

pause
