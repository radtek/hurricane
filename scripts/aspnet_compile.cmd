:: This script precompiles ASP.NET using 2.0 profile.

set path=%windir%\Microsoft.NET\Framework\v2.0.50727

aspnet_compiler -errorstack -f -p %USERPROFILE%\gshare\ -v / %USERPROFILE%\gshare.compiled\