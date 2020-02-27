fx_version 'adamant'
games { 'gta5' }
--dependency 'MenuAPI'

files {
	--'@MenuAPI/MenuAPI.dll',
	'MenuAPI.dll',	
	'HandlingInfo.xml',
	'HandlingPresets.xml',
	'VehiclesPermissions.xml',
	'Newtonsoft.Json.dll',
	'config.json'
}
client_script {
	'System.Xml.Mono.net.dll',
	'HandlingEditor.Client.net.dll'
}


