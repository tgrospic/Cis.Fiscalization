REM VS2012
REM "C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\wsdl.exe" /n:Cis /out:..\FiskalizacijaService.cs wsdl\FiskalizacijaService.wsdl schema\FiskalizacijaSchema.xsd schema\xmldsig-core-schema.xsd

REM VS2015
"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\wsdl.exe" /n:Cis /out:..\FiskalizacijaService.cs wsdl\FiskalizacijaService.wsdl schema\FiskalizacijaSchema.xsd schema\xmldsig-core-schema.xsd
