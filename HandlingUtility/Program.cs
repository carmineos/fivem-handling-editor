using HandlingEditor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace HandlingReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            /*
             if (args.Length <= 0)
                return;
            */

            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            string path = Path.Combine(dir.FullName, "meta");
            dir = new DirectoryInfo(path);

            IEnumerable<string> files = dir.GetFiles("*.meta").Select(f => f.FullName);

            //MergeHandlingFiles(files.ToArray());

            //GetCHandlingDataFields(files);
            //GetSubHandlingDataFields(files);
            CHandlingDataPreset.SaveXML("HandlingInfo.xml", "handling_merged.meta");
            //string text = File.ReadAllText("HandlingInfo_wip.xml");
            //string text = File.ReadAllText("HandlingInfo.json");
            //CHandlingDataInfo dataInfo = new CHandlingDataInfo();
            //dataInfo.ParseXML(text);

            Console.ReadKey();
        }

        public static void MergeHandlingFiles(string[] filePaths)
        {
            XDocument doc = XDocument.Load(filePaths[0]);

            for (int i = 1; i < filePaths.Length; i++)
            {
                //doc.Root.Add(XDocument.Load(files[i].FullName).Root.Elements());
                doc.Element("CHandlingDataMgr").Element("HandlingData").Add(XDocument.Load(filePaths[i]).Element("CHandlingDataMgr").Element("HandlingData").Elements());
            } 

            doc.Save("handling_merged.meta");
        }

        public static string GetFieldType(string fieldName)
        {
            if (fieldName.StartsWith("f"))
                return $"public float {fieldName};";
            else if (fieldName.StartsWith("n"))
                return $"public int {fieldName};";
            else if (fieldName.StartsWith("vec"))
                return $"public Vector3 {fieldName};";
            else if (fieldName.StartsWith("str"))
                return $"public string {fieldName};";
            else
                return $"public string {fieldName};";
        }

        public static void GetCHandlingDataFieldsFromMeta(HashSet<string> list, string filename)
        {
            XDocument doc = XDocument.Load(filename);

            var data = doc.Element("CHandlingDataMgr").Elements("HandlingData");
            var elements = data.Elements("Item").Where(a => a.Attribute("type").Value == "CHandlingData");

            foreach (var item in elements)
            {
                var fields = item.Elements();

                foreach (var field in fields)
                {
                    list.Add(field.Name.ToString());
                }
            }

        }

        public static void GetCHandlingDataFields(IEnumerable<string> filePaths)
        {
            HashSet<string> list = new HashSet<string>();

            foreach (string path in filePaths)
                GetCHandlingDataFieldsFromMeta(list, path);

            using (StreamWriter writer = new StreamWriter("CHandlingData.txt"))
            {
                foreach (var item in list)
                    writer.WriteLine(item);
                
                writer.WriteLine();

                writer.WriteLine("public class CHandlingData\r\n{");
                foreach (var item in list)
                    writer.WriteLine("\t"+GetFieldType(item));
                writer.WriteLine("}");
            }
        }

        public static void GetSubHandlingDataFieldsFromMeta(Dictionary<string,HashSet<string>> list, string filename)
        {
            XDocument doc = XDocument.Load(filename);

            var data = doc.Element("CHandlingDataMgr").Elements("HandlingData");
            var elements = data.Elements("Item").Where(a => a.Attribute("type").Value == "CHandlingData");

            foreach (var item in elements)
            {
                var subhandlingField = item.Element("SubHandlingData");

                if (subhandlingField != null)
                {
                    foreach (var subhandlingItem in subhandlingField.Elements())
                    {
                        var subhandlingType = subhandlingItem.Attribute("type").Value;

                        if (subhandlingType != "NULL" && subhandlingType != "")
                        {
                            if (!list.ContainsKey(subhandlingType))
                                list[subhandlingType] = new HashSet<string>();

                            var fields = subhandlingItem.Elements();
                            foreach (var a in fields)
                                list[subhandlingType].Add(a.Name.ToString());
                        }
                    }
                }
            }

        }

        public static void GetSubHandlingDataFields(IEnumerable<string> filePaths)
        {
            Dictionary<string, HashSet<string>> list = new Dictionary<string, HashSet<string>>();

            foreach (string path in filePaths)
                GetSubHandlingDataFieldsFromMeta(list, path);

            foreach (var item in list)
            {
                using (StreamWriter writer = new StreamWriter(item.Key + ".txt"))
                    foreach (string s in item.Value)
                        writer.WriteLine(s);
            }
        }


        static void CollectValues(string[] filenames)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (filenames.Length == 0)
            {
                Console.WriteLine("args[0]: handling.meta\nname: property{min,max,avg}");
            }
            else
            {
                XDocument doc = XDocument.Load(filenames[0]);
                List<string> result = new List<string>();

                IEnumerable<string> properties = doc.Element("CHandlingDataMgr").Element("HandlingData").Elements().Descendants().Select(a => a.Name.ToString()).Distinct();

                foreach (string property in properties)
                {
                    string line = property + ":";
                    IEnumerable<string> attributes = doc.Descendants(property).Attributes().Select(a => a.Name.ToString()).Distinct();
                    IEnumerable<XElement> sameprop = doc.Descendants(property);

                    foreach (string attribute in attributes)
                    {
                        try
                        {
                            float min = sameprop.Select(a => float.Parse(a.Attribute(attribute).Value)).Min();
                            float max = sameprop.Select(a => float.Parse(a.Attribute(attribute).Value)).Max();
                            float avg = sameprop.Select(a => float.Parse(a.Attribute(attribute).Value)).Average();
                            line = line + " " + attribute.ToString() + "{" + min.ToString() + "," + max.ToString() + "," + avg.ToString() + "}";
                        }
                        catch (Exception e) { }
                    }
                    result.Add(line);
                }

                using (StreamWriter writer = new StreamWriter("handling_stats.txt"))
                    foreach (string s in result)
                        writer.WriteLine(s);
            }
        }
    }

    public class CHandlingDataPreset
    {
        public static List<string> FieldsNames = new List<string>()
        {
            "handlingName",
            "fMass",
            "fInitialDragCoeff",
            "fPercentSubmerged",
            "vecCentreOfMassOffset",
            "vecInertiaMultiplier",
            "fDriveBiasFront",
            "nInitialDriveGears",
            "fInitialDriveForce",
            "fDriveInertia",
            "fClutchChangeRateScaleUpShift",
            "fClutchChangeRateScaleDownShift",
            "fInitialDriveMaxFlatVel",
            "fBrakeForce",
            "fBrakeBiasFront",
            "fHandBrakeForce",
            "fSteeringLock",
            "fTractionCurveMax",
            "fTractionCurveMin",
            "fTractionCurveLateral",
            "fTractionSpringDeltaMax",
            "fLowSpeedTractionLossMult",
            "fCamberStiffnesss",
            "fTractionBiasFront",
            "fTractionLossMult",
            "fSuspensionForce",
            "fSuspensionCompDamp",
            "fSuspensionReboundDamp",
            "fSuspensionUpperLimit",
            "fSuspensionLowerLimit",
            "fSuspensionRaise",
            "fSuspensionBiasFront",
            "fAntiRollBarForce",
            "fAntiRollBarBiasFront",
            "fRollCentreHeightFront",
            "fRollCentreHeightRear",
            "fCollisionDamageMult",
            "fWeaponDamageMult",
            "fDeformationDamageMult",
            "fEngineDamageMult",
            "fPetrolTankVolume",
            "fOilVolume",
            "fSeatOffsetDistX",
            "fSeatOffsetDistY",
            "fSeatOffsetDistZ",
            "nMonetaryValue",
            "strModelFlags",
            "strHandlingFlags",
            "strDamageFlags",
            "AIHandling",
            "SubHandlingData",
            "fWeaponDamageScaledToVehHealthMult",
            "fPopUpLightRotation",
            "fDownforceModifier",
            "fRocketBoostCapacity",
            "fBoostMaxSpeed"
        };
        public static Dictionary<string, string> FieldsDescriptions = new Dictionary<string, string>()
        {
            {"handlingName", "This is used by the vehicles.meta file to identify the handling line of the vehicle."},
            {"fMass", "This is the weight of the vehicle in kilograms. Only used when the vehicle collides with another vehicle or non-static object."},
            {"fInitialDragCoeff", "Sets the drag coefficient on the rage physics archetype of the vehicle (proportional to velocity squared). Increase to simulate aerodynamic drag."},
            {"fPercentSubmerged", "A percentage of vehicle height in the water before vehicle 'floats'. So as the vehicle falls into the water, at 85% vehicle height (seemingly the default for road vehicles) it will stop sinking to float for a moment before it sinks (boats excluded)."},
            {"vecCentreOfMassOffset", "This value shifts the center of gravity in meters from side to side (when in vehicle looking forward)."},
            {"fDriveBiasFront", "This value is used to determine whether a vehicle is front, rear, or four wheel drive."},
            {"nInitialDriveGears", "How many forward speeds a transmission contains."},
            {"fInitialDriveForce", "This multiplier modifies the game's calculation of drive force (from the output of the transmission)."},
            {"fDriveInertia", "Describes how fast an engine will rev. For example an engine with a long stroke crank and heavy flywheel will take longer to redline than an engine with a short stroke and light flywheel."},
            {"fClutchChangeRateScaleUpShift", "Clutch speed multiplier on up shifts, bigger number = faster shifts."},
            {"fClutchChangeRateScaleDownShift", "Clutch speed multiplier on down shifts, bigger number = faster shifts."},
            {"fInitialDriveMaxFlatVel", "Determines the speed at redline in top gear. Setting this value does not guarantee the vehicle will reach this speed. Multiply the number in the file by 0-82 to get the speed in mph or multiply by 1.32 to get kph."},
            {"fBrakeForce", "Multiplies the game's calculation of deceleration. Bigger number = harder braking."},
            {"fBrakeBiasFront", "This controls the distribution of braking force between the front and rear axles."},
            {"fHandBrakeForce", "Braking power for handbrake. Bigger number = harder braking."},
            {"fSteeringLock", "This value is a multiplier of the game's calculation of the angle a steer wheel will turn while at full turn. Steering lock is directly related to over or understeer / turning radius."},
            {"fTractionCurveMax", "Cornering grip of the vehicle as a multiplier of the tire surface friction."},
            {"fTractionCurveMin", "Accelerating/braking grip of the vehicle as a multiplier of the tire surface friction."},
            {"fTractionCurveLateral", "Shape of lateral traction curve (peak traction position in degrees)."},
            {"fTractionSpringDeltaMax", "This value denotes at what distance above the ground the car will lose traction."},
            {"fLowSpeedTractionLossMult", "How much traction is reduced at low speed, 0.0 means normal traction. It affects mainly car burnout (spinning wheels when car doesn't move) when pressing gas. Decreasing value will cause less burnout, less sliding at start. However, the higher value, the more burnout car gets. Optimal is 1.0."},
            {"fCamberStiffnesss", "This value modify the grip of the car when you're drifting and you release the gas. In general, it makes your car slide more on sideways movement. More than 0 make the car sliding on the same angle you're drifting and less than 0 make your car oversteer (not recommend to use more than 0.1 / -0.1 if you don't know what you're doing). This value depend of the others, and is better to modify when your handling is finished (or quasi finished). Not recommended to modify it for grip."},
            {"fTractionBiasFront", "Determines the distribution of traction from front to rear."},
            {"fTractionLossMult", "How much is traction affected by material grip differences from 1.0. Basically it affects how much grip is changed when driving on asphalt and mud (the higher, the more grip you loose, making car less responsive and prone to sliding)."},
            {"fSuspensionForce", "1 / (Force * NumWheels) = Lower limit for zero force at full extension. Affects how strong suspension is. Can help if car is easily flipped over when turning."},
            {"fSuspensionCompDamp", "Damping during strut compression. Bigger = stiffer."},
            {"fSuspensionReboundDamp", "Damping during strut rebound. Bigger = stiffer."},
            {"fSuspensionUpperLimit", "Visual limit... how far can wheels move up / down from original position."},
            {"fSuspensionLowerLimit", "Visual limit... how far can wheels move up / down from original position."},
            {"fSuspensionRaise", "The amount that the suspension raises the body off the wheels. Recommend adjusting at second decimal unless vehicle has room to move. ie -0.02 is plenty of drop on an already low car. Too much will show the wheels clipping through or if positive, no suspension joining the body to wheels."},
            {"fSuspensionBiasFront", "Force damping scale front/back. If more wheels at back (e.g. trucks) need front suspension to be stronger. This value determines which suspension is stronger, front or rear. If value is above 0.50 then front is stiffer, when below, rear."},
            {"fAntiRollBarForce", "The spring constant that is transmitted to the opposite wheel when under compression larger numbers are a larger force. Larger Numbers = less body roll."},
            {"fAntiRollBarBiasFront", "The bias between front and rear for the antiroll bar(0 front, 1 rear)."},
            {"fRollCentreHeightFront", "This value modify the weight transmission during an acceleration between the left and right."},
            {"fRollCentreHeightRear", "This value modify the weight transmission during an acceleration between the front and rear (and can affect the acceleration speed)."},
            {"fCollisionDamageMult", "Multiplies the game's calculation of damage to the vehicle through collision."},
            {"fWeaponDamageMult", "Multiplies the game's calculation of damage to the vehicle through weapon damage."},
            {"fDeformationDamageMult", "Multiplies the game's calculation of deformation-causing damage."},
            {"fEngineDamageMult", "Multiplies the game's calculation of damage to the engine, causing explosion or engine failure."},
            {"fPetrolTankVolume", "Amount of petrol that will leak after shooting the vehicle's petrol tank."},
            {"fOilVolume", "Black smoke time before engine dies?"},
            {"fSeatOffsetDistX", "The distance from the door to the car seat."},
            {"fSeatOffsetDistY", "The distance from the door to the car seat."},
            {"fSeatOffsetDistZ", "The distance from the door to the car seat."},
            {"nMonetaryValue", "Vehicle worth."},
            {"strModelFlags", "Model flags. Written in HEX. Rightmost digit is the first one."},
            {"strHandlingFlags", "Handling flags. Written in HEX. Rightmost digit is the first one."},
            {"strDamageFlags", "Indicates the doors that are nonbreakable. Written in HEX. Rightmost digit is the first one."},
            {"AIHandling", "Tells the AI which driving profile it should use when driving the vehicle. Use AVERAGE for boats, bikes, aircraft, etc."},
        };

        public static Type GetFieldType(string name)
        {
            if (name.StartsWith("f")) return typeof(float);
            else if (name.StartsWith("n")) return typeof(int);
            else if (name.StartsWith("str")) return typeof(string);
            else if (name.StartsWith("vec")) return typeof(Vector3);
            else return null;
        }

        public static void SaveXML(string filename, string handlingFile)
        {
            IEnumerable<XElement> handlings = XDocument.Load(handlingFile).Element("CHandlingDataMgr").Element("HandlingData").Elements();

            XDocument doc = new XDocument();
            doc.Add(new XComment(" https://www.gtamodding.com/wiki/Handling.meta "));
            XElement chandlingfields = new XElement("CHandlingData");
            foreach (var item in FieldsNames)
            {
                var values = handlings.Descendants(item);
                XElement e = new XElement(item);
                bool editable;
                dynamic min = 0, max = 0;

                Type fieldType = GetFieldType(item);
                if (fieldType == typeof(float))
                {
                    editable = true;
                    min = values.Select(a => float.Parse(a.Attribute("value").Value)).Min();
                    max = values.Select(a => float.Parse(a.Attribute("value").Value)).Max();
                    e.Add(CreateProperty("Min", min));
                    e.Add(CreateProperty("Max", max));
                }
                else if(fieldType == typeof(int))
                {
                    editable = false;
                    min = values.Select(a => int.Parse(a.Attribute("value").Value)).Min();
                    max = values.Select(a => int.Parse(a.Attribute("value").Value)).Max();
                    e.Add(CreateProperty("Min", min));
                    e.Add(CreateProperty("Max", max));
                }
                else if(fieldType == typeof(Vector3))
                {
                    editable = false;
                    var minX = values.Select(a => float.Parse(a.Attribute("x").Value)).Min();
                    var minY = values.Select(a => float.Parse(a.Attribute("y").Value)).Min();
                    var minZ = values.Select(a => float.Parse(a.Attribute("z").Value)).Min();
                    var maxX = values.Select(a => float.Parse(a.Attribute("x").Value)).Max();
                    var maxY = values.Select(a => float.Parse(a.Attribute("y").Value)).Max();
                    var maxZ = values.Select(a => float.Parse(a.Attribute("z").Value)).Max();

                    min = new Vector3(minX,minY,minZ);
                    max = new Vector3(maxX, maxY, maxZ);
                    e.Add(CreateProperty("Min", min));
                    e.Add(CreateProperty("Max", max));
                }
                else
                {
                    editable = false;
                }

                e.Add(new XAttribute("Editable", editable));

                XElement desc = new XElement("Description");
                if (FieldsDescriptions.ContainsKey(item))
                    desc.Value = FieldsDescriptions[item];
                e.Add(desc);

                chandlingfields.Add(e);

            }
            doc.Add(chandlingfields);
            doc.Save(filename);
        }

        private static XElement CreateProperty(string name, bool value)
        {
            XElement e = new XElement(name);
            e.Add(new XAttribute("value", value));
            return e;
        }

        private static XElement CreateProperty(string name, float value)
        {
            XElement e = new XElement(name);
            e.Add(new XAttribute("value", value));
            return e;
        }

        private static XElement CreateProperty(string name, int value)
        {
            XElement e = new XElement(name);
            e.Add(new XAttribute("value", value));
            return e;
        }

        private static XElement CreateProperty(string name, Vector3 value)
        {
            XElement e = new XElement(name);
            e.Add(new XAttribute("x", value.X));
            e.Add(new XAttribute("y", value.Y));
            e.Add(new XAttribute("z", value.Z));
            return e;
        }
    }

    public class Vector3
    {
        public float X;
        public float Y;
        public float Z;
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
