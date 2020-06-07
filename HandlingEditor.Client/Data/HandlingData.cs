using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;

namespace HandlingEditor.Client
{
    public class HandlingData : IEquatable<HandlingData>
    {
        public const float Epsilon = 0.001f;

        public event EventHandler<string> HandlingFieldEdited;

        public Dictionary<string, dynamic> DefaultFields { get; private set; }
        public Dictionary<string, dynamic> Fields { get; set; }

        public HandlingData()
        {
            DefaultFields = new Dictionary<string, dynamic>();
            Fields = new Dictionary<string, dynamic>();
        }

        public bool IsEdited
        {
            get
            {
                foreach(var item in Fields)
                {
                    var value = item.Value;
                    var defaultValue = DefaultFields[item.Key];

                    Type fieldType = value.GetType();

                    if (fieldType == HandlingFieldTypes.IntType)
                    {
                        if (defaultValue != value)
                            return true;
                    }
                    else if(fieldType == HandlingFieldTypes.FloatType)
                    {
                        if (!MathUtil.WithinEpsilon(value, defaultValue, Epsilon))
                            return true;
                    }
                    else if (fieldType == HandlingFieldTypes.Vector3Type)
                    {
                        if (!((Vector3)value).Equals((Vector3)defaultValue))
                            return true;
                    }
                }
                return false;
            }
        }

        public void Reset()
        {
            foreach (var item in DefaultFields)
            {
                string name = item.Key;
                var value = item.Value;
                Type fieldType = value.GetType();

                if (fieldType == HandlingFieldTypes.FloatType || fieldType == HandlingFieldTypes.IntType)
                    Fields[name] = value;

                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    Vector3 vec = (Vector3)value;
                    Fields[name] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            }
        }

        public bool Equals(HandlingData other)
        {
            if (Fields.Count != other.Fields.Count)
                return false;

            foreach (var item in Fields)
            {
                string key = item.Key;

                if (!other.Fields.TryGetValue(key, out dynamic otherValue))
                    return false;

                var value = item.Value;

                Type fieldType = value.GetType();

                if (fieldType == HandlingFieldTypes.IntType)
                {
                    if (value != otherValue)
                        return false;
                }
                else if(fieldType == HandlingFieldTypes.FloatType)
                {
                    if (!MathUtil.WithinEpsilon(value, otherValue, Epsilon))
                        return false;
                }
                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    if (!((Vector3)value).Equals((Vector3)otherValue))
                        return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("HandlingPreset Fields:");
            foreach (var item in Fields)
            {
                s.AppendLine($"{item.Key}: {item.Value}({DefaultFields[item.Key]})");
            }

            return s.ToString();
        }

        public void CopyFields(HandlingData other, bool onlySharedFields = true)
        {
            foreach (var item in other.Fields)
            {
                if (onlySharedFields)
                {
                    if (Fields.ContainsKey(item.Key))
                        Fields[item.Key] = item.Value;
                }
                else
                {
                    Fields[item.Key] = item.Value;
                }
            }
        }

    }

    public class CHandlingData
    {
        private string handlingName;
        private float fMass;
        private float fInitialDragCoeff;
        private float fPercentSubmerged;
        private Vector3 vecCentreOfMassOffset;
        private Vector3 vecInertiaMultiplier;
        private float fDriveBiasFront;
        private int nInitialDriveGears;
        private float fInitialDriveForce;
        private float fDriveInertia;
        private float fClutchChangeRateScaleUpShift;
        private float fClutchChangeRateScaleDownShift;
        private float fInitialDriveMaxFlatVel;
        private float fBrakeForce;
        private float fBrakeBiasFront;
        private float fHandBrakeForce;
        private float fSteeringLock;
        private float fTractionCurveMax;
        private float fTractionCurveMin;
        private float fTractionCurveLateral;
        private float fTractionSpringDeltaMax;
        private float fLowSpeedTractionLossMult;
        private float fCamberStiffnesss;
        private float fTractionBiasFront;
        private float fTractionLossMult;
        private float fSuspensionForce;
        private float fSuspensionCompDamp;
        private float fSuspensionReboundDamp;
        private float fSuspensionUpperLimit;
        private float fSuspensionLowerLimit;
        private float fSuspensionRaise;
        private float fSuspensionBiasFront;
        private float fAntiRollBarForce;
        private float fAntiRollBarBiasFront;
        private float fRollCentreHeightFront;
        private float fRollCentreHeightRear;
        private float fCollisionDamageMult;
        private float fWeaponDamageMult;
        private float fDeformationDamageMult;
        private float fEngineDamageMult;
        private float fPetrolTankVolume;
        private float fOilVolume;
        private float fSeatOffsetDistX;
        private float fSeatOffsetDistY;
        private float fSeatOffsetDistZ;
        private int nMonetaryValue;
        private string strModelFlags;
        private string strHandlingFlags;
        private string strDamageFlags;
        private string AIHandling;
        private string SubHandlingData;
        private float fWeaponDamageScaledToVehHealthMult;
        private float fPopUpLightRotation;
        private float fDownforceModifier;
        private float fRocketBoostCapacity;
        private float fBoostMaxSpeed;
    }
}
