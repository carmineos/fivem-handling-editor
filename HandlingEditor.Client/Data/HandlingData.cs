using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;

namespace HandlingEditor.Client
{
    // TODO: Rework class, make dictionaries private, add indexer which invokes PropertyChanged event
    public class HandlingData
    {
        public const float Epsilon = 0.001f;

        public delegate void HandlingDataPropertyChanged(string propertyName, object value);
        public event HandlingDataPropertyChanged PropertyChanged;

        private readonly Dictionary<string, object> _defaultFields;
        private readonly Dictionary<string, object> _fields;

        public Dictionary<string, object> Fields => new Dictionary<string, object>(_fields);

        public void SetFieldValue(string name, object value)
        {
            _fields[name] = value;
            PropertyChanged?.Invoke(name, value);
        }

        public object GetFieldValue(string name) => _fields[name];

        public object GetDefaultFieldValue(string name) => _defaultFields[name];

        public HandlingData(Dictionary<string, object> currentValues, Dictionary<string,object> defaultValues)
        {
            _defaultFields = new Dictionary<string, object>(defaultValues);
            _fields = new Dictionary<string, object>(currentValues);
        }

        public bool IsEdited
        {
            get
            {
                foreach(var item in _fields)
                {
                    var value = item.Value;
                    var defaultValue = _defaultFields[item.Key];

                    Type fieldType = value.GetType();

                    if (fieldType == HandlingFieldTypes.IntType)
                    {
                        if (defaultValue != value)
                            return true;
                    }
                    else if(fieldType == HandlingFieldTypes.FloatType)
                    {
                        if (!MathUtil.WithinEpsilon((float)value, (float)defaultValue, Epsilon))
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
            foreach (var item in _defaultFields)
            {
                string name = item.Key;
                var value = item.Value;
                Type fieldType = value.GetType();

                if (fieldType == HandlingFieldTypes.FloatType || fieldType == HandlingFieldTypes.IntType)
                    _fields[name] = value;

                else if (fieldType == HandlingFieldTypes.Vector3Type)
                {
                    Vector3 vec = (Vector3)value;
                    _fields[name] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            }

            PropertyChanged?.Invoke(nameof(Reset), default);
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("HandlingPreset Fields:");
            foreach (var item in _fields)
            {
                s.AppendLine($"{item.Key}: {item.Value}({_defaultFields[item.Key]})");
            }

            return s.ToString();
        }

        public void CopyFields(HandlingData other, bool onlySharedFields = true)
        {
            foreach (var item in other._fields)
            {
                if (onlySharedFields)
                {
                    if (_fields.ContainsKey(item.Key))
                        _fields[item.Key] = item.Value;
                }
                else
                {
                    _fields[item.Key] = item.Value;
                }
            }
        }

    }

    //public class CHandlingData
    //{
    //    private string handlingName;
    //    private float fMass;
    //    private float fInitialDragCoeff;
    //    private float fPercentSubmerged;
    //    private Vector3 vecCentreOfMassOffset;
    //    private Vector3 vecInertiaMultiplier;
    //    private float fDriveBiasFront;
    //    private int nInitialDriveGears;
    //    private float fInitialDriveForce;
    //    private float fDriveInertia;
    //    private float fClutchChangeRateScaleUpShift;
    //    private float fClutchChangeRateScaleDownShift;
    //    private float fInitialDriveMaxFlatVel;
    //    private float fBrakeForce;
    //    private float fBrakeBiasFront;
    //    private float fHandBrakeForce;
    //    private float fSteeringLock;
    //    private float fTractionCurveMax;
    //    private float fTractionCurveMin;
    //    private float fTractionCurveLateral;
    //    private float fTractionSpringDeltaMax;
    //    private float fLowSpeedTractionLossMult;
    //    private float fCamberStiffnesss;
    //    private float fTractionBiasFront;
    //    private float fTractionLossMult;
    //    private float fSuspensionForce;
    //    private float fSuspensionCompDamp;
    //    private float fSuspensionReboundDamp;
    //    private float fSuspensionUpperLimit;
    //    private float fSuspensionLowerLimit;
    //    private float fSuspensionRaise;
    //    private float fSuspensionBiasFront;
    //    private float fAntiRollBarForce;
    //    private float fAntiRollBarBiasFront;
    //    private float fRollCentreHeightFront;
    //    private float fRollCentreHeightRear;
    //    private float fCollisionDamageMult;
    //    private float fWeaponDamageMult;
    //    private float fDeformationDamageMult;
    //    private float fEngineDamageMult;
    //    private float fPetrolTankVolume;
    //    private float fOilVolume;
    //    private float fSeatOffsetDistX;
    //    private float fSeatOffsetDistY;
    //    private float fSeatOffsetDistZ;
    //    private int nMonetaryValue;
    //    private string strModelFlags;
    //    private string strHandlingFlags;
    //    private string strDamageFlags;
    //    private string AIHandling;
    //    private string SubHandlingData;
    //    private float fWeaponDamageScaledToVehHealthMult;
    //    private float fPopUpLightRotation;
    //    private float fDownforceModifier;
    //    private float fRocketBoostCapacity;
    //    private float fBoostMaxSpeed;
    //}
}
