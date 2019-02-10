using CitizenFX.Core;
using System;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Server
{
    public class RequestsHandler : BaseScript
    {
        public RequestsHandler()
        {
            EventHandlers.Add("HandlingEditor:RequestEditVehiclePermissions", new Action<Player>(SendEditVehiclePermissions));
            EventHandlers.Add("HandlingEditor:RequestSaveServerPresetPermissions", new Action<Player>(SendSaveServerPresetPermissions));
            EventHandlers.Add("HandlingEditor:RequestSavePersonalPresetPermissions", new Action<Player>(SendSavePersonalPresetPermissions));
        }

        private void SendSavePersonalPresetPermissions([FromSource]Player source)
        {
            if (IsPlayerAceAllowed(source.Handle, "HandlingEditor.SavePersonalPreset"))
                TriggerClientEvent(source, "HandlingEditor.SavePersonalPreset");
        }

        private void SendSaveServerPresetPermissions([FromSource]Player source)
        {
            if (IsPlayerAceAllowed(source.Handle, "HandlingEditor.SaveServerPreset"))
                TriggerClientEvent(source, "HandlingEditor.SaveServerPreset");
        }

        private void SendEditVehiclePermissions([FromSource]Player source)
        {
            if (IsPlayerAceAllowed(source.Handle, "HandlingEditor.EditVehicle"))
                TriggerClientEvent(source, "HandlingEditor.EditVehicle");
        }
    }
}
