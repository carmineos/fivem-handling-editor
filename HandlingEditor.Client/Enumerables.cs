using System.Collections;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    internal class KvpEnumerable : IEnumerable<string>
    {
        public string prefix;

        public KvpEnumerable(string kvpprefix)
        {
            this.prefix = kvpprefix;
        }

        public IEnumerator<string> GetEnumerator()
        {
            int handle = StartFindKvp(prefix);

            if (handle != -1)
            {
                string kvp;
                do
                {
                    kvp = FindKvp(handle);

                    if (kvp != null)
                        yield return kvp;
                }
                while (kvp != null);
                EndFindKvp(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class VehicleEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                do yield return entity;
                while (FindNextVehicle(handle, ref entity));

                EndFindVehicle(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class PedEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstPed(ref entity);

            if (handle != -1)
            {
                do yield return entity;
                while (FindNextPed(handle, ref entity));

                EndFindPed(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class PickupEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstPickup(ref entity);

            if (handle != -1)
            {
                do yield return entity;
                while (FindNextPickup(handle, ref entity));

                EndFindPickup(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class ObjectEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstObject(ref entity);

            if (handle != -1)
            {
                do yield return entity;
                while (FindNextObject(handle, ref entity));

                EndFindObject(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
