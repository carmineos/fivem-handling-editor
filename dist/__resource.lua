resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

--dependency 'MenuAPI'

files {
	--'@MenuAPI/MenuAPI.dll',
	'MenuAPI.dll',	
	'HandlingInfo.xml',
	'HandlingPresets.xml',
	'VehiclesPermissions.xml',
	'config.ini'
}
client_script {
	'System.Xml.Mono.net.dll',
	'HandlingEditor.Client.net.dll'
}