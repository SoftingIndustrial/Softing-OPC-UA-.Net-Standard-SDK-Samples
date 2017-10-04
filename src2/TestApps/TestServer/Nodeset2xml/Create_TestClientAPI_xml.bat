set TestClientImportXML=C:\jenkins\workspace\ImportNodeSet2\%%~ni.xml%1
set NodeSet2Dir=C:\jenkins\workspace\ImportNodeSet2\OPCUaNetSdkV1\src\TestApps\TestServer\Nodeset2xml%1
set MCTestResults=C:\jenkins\workspace\ImportNodeSet2\MCTestClient_Results%1

rem Delete old results
cd %TestClientImportXML%
del *.xml 

cd %MCTestResults%
del *.xml

for %%i in (%NodeSet2Dir%\*.xml) do (

rem Starting Test Server
cd C:\jenkins\workspace\ImportNodeSet2\OPCUaNetSdkV1\src\TestApps\TestServer\bin\Net451\Release
start TestServer.exe 

PING 1.1.1.1 -n 1 -w 5000 >NUL 
	
rem  Create session
echo ^<?xml version=^"1.0^"^ encoding=^"utf-8^"^ ^?^>>> %TestClientImportXML%
echo ^<TestList xmlns:xsi=^"http://www.w3.org/2001/XMLSchema-instance^"^ xmlns:xsd=^"http://www.w3.org/2001/XMLSchema^"^ xmlns=^"http://industrial.softing.com/TestClientSimpleAPI.xsd^"^>>> %TestClientImportXML%
echo.>> %TestClientImportXML%
echo ^<TestList Text=^"opc.tcp binary^"^>>> %TestClientImportXML%
echo  ^<SessionCreate^>>> %TestClientImportXML%
echo   ^<Url^>opc.tcp://localhost:62541/TestServer^</Url^>>> %TestClientImportXML%
echo   ^<Timeout^>300^</Timeout^>>> %TestClientImportXML%
echo   ^<Locales^>en^</Locales^>>> %TestClientImportXML%
echo   ^<Active^>false^</Active^>>> %TestClientImportXML%
echo   ^<SecurityMode^>None^</SecurityMode^>>> %TestClientImportXML%
echo   ^<SecurityPolicy^>None^</SecurityPolicy^>>> %TestClientImportXML%
echo   ^<Encoding^>Binary^</Encoding^>>> %TestClientImportXML%
echo   ^<Property^>Sessions.Session1^</Property^>>> %TestClientImportXML%
echo  ^</SessionCreate^>>> %TestClientImportXML%

rem		Import NodeSet2.xml
echo.>> %TestClientImportXML%
echo creating file %TestClientImportXML%
echo  ^<SessionCall^>>> %TestClientImportXML%
echo   ^<Session^>^Sessions.Session^1^</Session^>>> %TestClientImportXML%
echo   ^<ObjectId Text=^"ns=10;s=ImportNodeset^"/^>>> %TestClientImportXML%
echo   ^<MethodId Text=^"ns=10;s=Import^"/^>>> %TestClientImportXML%
echo   ^<InputArguments^>>> %TestClientImportXML%
echo    ^<anyType xsi:type=^"xsd:string^"^>%NodeSet2Dir%\%%~ni.xml^</anyType^>>> %TestClientImportXML%
echo   ^</InputArguments^>>> %TestClientImportXML%
echo  ^</SessionCall^>>> %TestClientImportXML%
echo.>> %TestClientImportXML%
echo   ^<Disconnect^>>> %TestClientImportXML%
echo    ^<Object^>Sessions.Session1^</Object^>>> %TestClientImportXML%
echo   ^</Disconnect^>>> %TestClientImportXML%
echo.>> %TestClientImportXML%

rem  Remove session
echo  ^</TestList^>>> %TestClientImportXML%
echo ^</TestList^>>> %TestClientImportXML%


PING 1.1.1.1 -n 1 -w 5000 >NUL 

rem Start TestClient API, call method that import NodeSet2.xml
cd C:\jenkins\workspace\ImportNodeSet2\OPCUaNetSdkV1\src\TestApps\TestClientGui SimpleAPI\bin\Net451\Release
start TestClientGui2013.exe C:\jenkins\workspace\ImportNodeSet2\%%~ni.xml r c

PING 1.1.1.1 -n 1 -w 5000 >NUL 

rem Start MCTestClient that validates NodeSet2.xml import
cd C:\jenkins\workspace\ImportNodeSet2\MCTestClient
start MCTestClient.exe /nodeSetFile "%NodeSet2Dir%\%%~ni.xml" /endpointUrl opc.tcp://localhost:62541/TestServer /skipValueValidation /JUnitOut "%MCTestResults%\%%~ni.xml"

PING 1.1.1.1 -n 1 -w 5000 >NUL 

taskkill /im MCTestClient.exe
taskkill /im TestServer.exe

PING 1.1.1.1 -n 1 -w 5000 >NUL 
)