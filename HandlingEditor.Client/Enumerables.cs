using System.Collections;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace HandlingEditor.Client
{
    public class KvpEnumerable : IEnumerable<string>
    {
        public string prefix { get; set; } = string.Empty;

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

    public class VehicleEnumerable : IEnumerable<int>
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

    public class PedEnumerable : IEnumerable<int>
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

    public class PickupEnumerable : IEnumerable<int>
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

    public class ObjectEnumerable : IEnumerable<int>
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

    /** THIS SHOULD BE SIMPLER
    public static class Enumerables
    {
        public static IEnumerable<string> Kvp(string prefix)
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

        public static IEnumerable<int> Vehicles()
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

        public static IEnumerable<int> Peds()
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

        public static IEnumerable<int> Pickups()
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

        public static IEnumerable<int> Objects()
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
    }*/
}
